using KnowHowToAI.Server.Configuration;
using Serilog;
using Serilog.Events;

namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Baut die Protokollierung auf: alle Ausgaben nach stderr, optional ergänzt um eine
/// rotierende Datei. Roh-Payloads schreibt der MCP-Transport ausschließlich auf
/// Trace-Ebene; der konfigurierbare Default-Level liegt darüber.
/// </summary>
internal static class LoggingSetup
{
    /// <summary>
    /// Wendet die validierte Protokollierungs-Konfiguration auf eine Serilog-Konfiguration an.
    /// </summary>
    public static void ApplyTo(LoggerConfiguration configuration, LoggingOptions options)
    {
        configuration
            .MinimumLevel.Is(Enum.Parse<LogEventLevel>(options.MinimumLevel, ignoreCase: true))
            .Enrich.With(new SecretRedactionEnricher())
            .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose);

        if (!string.IsNullOrWhiteSpace(options.FilePath))
        {
            var logPath = ResolveLogFilePath(options.FilePath);
            configuration.WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day);
        }
    }

    internal static string ResolveLogFilePath(string filePath) =>
        Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(AppContext.BaseDirectory, filePath);
}
