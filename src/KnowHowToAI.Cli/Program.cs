using System.CommandLine;

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

var importCommand = new Command("import", "Führt DbUp-Migrationen aus und synchronisiert die Docs per Wipe-and-Dump nach SQL Server.") { configOption };
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

// Implementierung je Kommando: docs/05-Roadmap.md, Schritt 5/6.
static int RunValidate(ParseResult parseResult) => throw new NotImplementedException();
static int RunImport(ParseResult parseResult) => throw new NotImplementedException();
static int RunExport(ParseResult parseResult) => throw new NotImplementedException();
static int RunServer(ParseResult parseResult) => throw new NotImplementedException();
