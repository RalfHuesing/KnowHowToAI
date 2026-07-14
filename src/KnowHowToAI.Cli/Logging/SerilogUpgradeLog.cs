using DbUp.Engine.Output;
using Serilog;

namespace KnowHowToAI.Cli.Logging;

// Bindet DbUps IUpgradeLog an Serilog, damit die Migration nie auf Console.Out schreibt.
// Siehe docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 1 ("Logging-Abstraktion").
public sealed class SerilogUpgradeLog(ILogger logger) : IUpgradeLog
{
    public void LogDebug(string format, params object[] args) => logger.Debug(format, args);
    public void LogInformation(string format, params object[] args) => logger.Information(format, args);
    public void LogTrace(string format, params object[] args) => logger.Verbose(format, args);
    public void LogWarning(string format, params object[] args) => logger.Warning(format, args);
    public void LogError(string format, params object[] args) => logger.Error(format, args);
    public void LogError(Exception ex, string format, params object[] args) => logger.Error(ex, format, args);
}
