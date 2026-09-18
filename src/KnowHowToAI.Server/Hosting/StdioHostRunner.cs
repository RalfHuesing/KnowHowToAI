using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Fährt den Host kontrolliert herunter: SIGTERM/Ctrl+C, Ende des Client-Eingabestroms
/// und defekte Client-Pipe werden abgefangen und in stabile Prozess-Exitcodes überführt,
/// statt den Prozess mit unbehandelter Exception zu beenden.
/// </summary>
internal static class StdioHostRunner
{
    public static async Task<int> RunAsync(IHost host, ILogger logger)
        => await RunAsync(host, logger, beforeStart: null).ConfigureAwait(false);

    /// <summary>
    /// Startet den gemeinsamen Host nach synchronen Fail-fast-Prüfungen. Die Prüfung
    /// läuft vor dem Start der Hosted Services und damit vor einer Kestrel-Bindung.
    /// </summary>
    public static async Task<int> RunAsync(IHost host, Action beforeStart)
        => await RunAsync(host, logger: null, beforeStart).ConfigureAwait(false);

    private static async Task<int> RunAsync(IHost host, ILogger? logger, Action? beforeStart)
    {
        try
        {
            beforeStart?.Invoke();
            logger ??= host.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(StdioHostRunner));
            await host.RunAsync().ConfigureAwait(false);
            return ServerExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Server wurde abgebrochen und beendet sich kontrolliert.");
            return ServerExitCodes.Success;
        }
        catch (IOException exception) when (!IsAddressAlreadyInUse(exception))
        {
            logger?.LogInformation(
                "Client-Pipe getrennt oder senden fehlgeschlagen ({ExceptionType}); der Server beendet sich kontrolliert.",
                exception.GetType().Name);
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

    private static bool IsAddressAlreadyInUse(IOException exception) =>
        exception.InnerException is SocketException
        {
            SocketErrorCode: SocketError.AddressAlreadyInUse
        };
}
