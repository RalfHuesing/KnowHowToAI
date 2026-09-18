using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Startet den gemeinsamen Host nach synchronen Fail-fast-Prüfungen. Die Prüfung
/// läuft vor dem Start der Hosted Services und damit vor einer Kestrel-Bindung.
/// Kontrolliertes Herunterfahren und Abbruch ergeben <see cref="ServerExitCodes.Success"/>;
/// jeder andere Fehler wird ohne sensitive Details protokolliert und ergibt
/// <see cref="ServerExitCodes.StartupFailure"/>.
/// </summary>
internal static class HostRunner
{
    public static async Task<int> RunAsync(IHost host, ILogger logger)
        => await RunAsync(host, logger, beforeStart: null).ConfigureAwait(false);

    public static async Task<int> RunAsync(IHost host, Action beforeStart)
        => await RunAsync(host, logger: null, beforeStart).ConfigureAwait(false);

    private static async Task<int> RunAsync(IHost host, ILogger? logger, Action? beforeStart)
    {
        try
        {
            beforeStart?.Invoke();
            logger ??= host.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(HostRunner));
            await host.RunAsync().ConfigureAwait(false);
            return ServerExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Server wurde abgebrochen und beendet sich kontrolliert.");
            return ServerExitCodes.Success;
        }
        catch (Exception exception)
        {
            logger?.LogError(
                "Der Server wurde wegen eines unbehandelten Fehlers beendet ({ExceptionType}).",
                exception.GetType().Name);
            return ServerExitCodes.StartupFailure;
        }
    }
}
