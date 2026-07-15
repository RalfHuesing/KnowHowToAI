using System.CommandLine;
using KnowHowToAI.Cli.McpTools;
using KnowHowToAI.Core.Configuration;
using KnowHowToAI.Core.Migrations;
using KnowHowToAI.Core.Sync;
using KnowHowToAI.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// UTF8Encoding ohne BOM: Encoding.UTF8 würde beim ersten Schreibzugriff eine BOM-Präambel
// ausgeben und damit im "server"-Modus die ersten Bytes des JSON-RPC-Streams korrumpieren.
Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

// Bootstrap-Logger mit Options-Defaults, bevor appsettings.json gelesen werden konnte (z.B. wenn
// LoadOptions selbst fehlschlägt). Wird direkt nach LoadOptions durch die konfigurierten
// Rotation-Einstellungen ersetzt, siehe ConfigureLogger. Kein Konsolen-Sink: Console.Out ist für
// den MCP-stdio-Server reserviert, und Console.Error wäre bei einem von Cursor/Claude Desktop
// gestarteten Hintergrundprozess ohnehin nicht einsehbar. Siehe
// docs/02-Architektur-und-Techstack.md, kritischer Implementierungs-Hinweis.
Log.Logger = ConfigureLogger(new KnowHowToAiLoggingOptions());

var configOption = new Option<string?>("--config")
{
    Description = "Pfad zur appsettings.json. Wenn ausgelassen, wird appsettings.json neben der .exe verwendet."
};

var targetOption = new Option<string?>("--target")
{
    Description = "Zielverzeichnis für den Export."
};

var validateCommand = new Command("validate", "Prüft das Docs-Root-Verzeichnis (YAML, Slugs, Hierarchie).") { configOption };
validateCommand.SetAction(RunValidate);

var importCommand = new Command("import", "Bringt das Schema per SQL-Skripten auf den aktuellen Stand und synchronisiert die Docs per Wipe-and-Dump nach SQL Server.") { configOption };
importCommand.SetAction(RunImport);

var exportCommand = new Command("export", "Exportiert den DB-Inhalt als .md-Dateien (Marker-Datei-geschützt).") { configOption, targetOption };
exportCommand.SetAction(RunExport);

var serverCommand = new Command("server", "Startet den MCP-stdio-Server.") { configOption };
serverCommand.SetAction(RunServer);

var rootCommand = new RootCommand("KnowHowToAI — hierarchische Markdown-Wissensdatenbank mit MCP-Zugriff.")
{
    validateCommand,
    importCommand,
    exportCommand,
    serverCommand
};

return await rootCommand.Parse(args).InvokeAsync();

int RunValidate(ParseResult parseResult)
{
    try
    {
        var options = LoadOptions(parseResult.GetValue(configOption));
        Log.Logger = ConfigureLogger(options.Logging);
        var result = new DocsValidator(options.Validation.MaxContentLengthWarning).Validate(options.DocsRootPath);
        return PrintValidationResult(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
}

async Task<int> RunImport(ParseResult parseResult, CancellationToken cancellationToken)
{
    try
    {
        var options = LoadOptions(parseResult.GetValue(configOption));
        Log.Logger = ConfigureLogger(options.Logging);

        await SchemaMigrator.MigrateAsync(
            options.ConnectionString, options.DocumentsTableName, message => Log.Logger.Information(message), cancellationToken);

        var store = new SqlDocumentsStore(options.ConnectionString, options.DocumentsTableName);
        var importService = new ImportService(store.ReplaceAllAsync, options.Validation.MaxContentLengthWarning);
        var result = await importService.ImportAsync(options.DocsRootPath, cancellationToken);
        return PrintValidationResult(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
}

async Task<int> RunExport(ParseResult parseResult, CancellationToken cancellationToken)
{
    try
    {
        var options = LoadOptions(parseResult.GetValue(configOption));
        Log.Logger = ConfigureLogger(options.Logging);
        var target = parseResult.GetValue(targetOption)
            ?? throw new InvalidOperationException("--target ist erforderlich.");

        var store = new SqlDocumentsStore(options.ConnectionString, options.DocumentsTableName);
        var exportService = new ExportService(store.GetAllAsync);
        await exportService.ExportAsync(target, options.ExportMarkerFileName, cancellationToken);

        Console.WriteLine($"Export abgeschlossen nach '{target}'.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
}

async Task<int> RunServer(ParseResult parseResult, CancellationToken cancellationToken)
{
    try
    {
        var options = LoadOptions(parseResult.GetValue(configOption));
        Log.Logger = ConfigureLogger(options.Logging);

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(Log.Logger);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new SqlDocumentsStore(options.ConnectionString, options.DocumentsTableName));
        builder.Services.AddMcpServer(o => o.ServerInstructions = DocsMcpResources.ServerInstructions)
            .WithStdioServerTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly();

        using var host = builder.Build();
        await host.RunAsync(cancellationToken);
        return 0;
    }
    catch (Exception ex)
    {
        Log.Logger.Error(ex, "MCP-Server konnte nicht gestartet werden.");
        return 2;
    }
}

static KnowHowToAiOptions LoadOptions(string? configPath)
{
    var path = configPath ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (!File.Exists(path))
    {
        throw new InvalidOperationException($"Konfigurationsdatei nicht gefunden: '{path}'.");
    }

    var configuration = new ConfigurationBuilder()
        .AddJsonFile(path, optional: false)
        .AddEnvironmentVariables()
        .Build();

    var options = configuration.GetSection("KnowHowToAi").Get<KnowHowToAiOptions>()
        ?? throw new InvalidOperationException($"Abschnitt 'KnowHowToAi' fehlt in '{path}'.");

    // Literal ersetzen statt Environment.ExpandEnvironmentVariables: robust auch dann, wenn der
    // MCP-Prozess (von Cursor/Claude Desktop gestartet) COMPUTERNAME nicht im Environment geerbt hat.
    return options with
    {
        ConnectionString = options.ConnectionString.Replace(
            "%COMPUTERNAME%", Environment.MachineName, StringComparison.OrdinalIgnoreCase)
    };
}

static Serilog.ILogger ConfigureLogger(KnowHowToAiLoggingOptions loggingOptions) =>
    new LoggerConfiguration()
        .MinimumLevel.Is(Enum.Parse<LogEventLevel>(loggingOptions.MinimumLevel))
        .WriteTo.File(
            Path.Combine(AppContext.BaseDirectory, "Logs", "knowhowtoai-.log"),
            rollingInterval: Enum.Parse<RollingInterval>(loggingOptions.RollingInterval),
            retainedFileCountLimit: loggingOptions.RetainedFileCountLimit,
            shared: true)
        .CreateLogger();

static int PrintValidationResult(ValidationResult result)
{
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"WARNING {warning.FilePath}: {warning.Reason}");
    }

    if (result.IsValid)
    {
        Console.WriteLine($"Validation successful. 0 errors found, {result.Warnings.Count} warning(s) found.");
        return 0;
    }

    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.FilePath}: {error.Reason}");
    }

    Console.WriteLine($"Validation failed. {result.Errors.Count} error(s) found.");
    return 1;
}
