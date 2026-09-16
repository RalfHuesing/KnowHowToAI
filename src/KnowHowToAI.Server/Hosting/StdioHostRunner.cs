using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Fährt den Host kontrolliert herunter: SIGTERM/Ctrl+C, Ende des Client-Eingabestroms
/// und defekte Client-Pipe werden abgefangen und in stabile Prozess-Exitcodes überführt,
/// statt den Prozess mit unbehandelter Exception zu beenden.
/// </summary>
internal static class StdioHostRunner
{
    public static async Task<int> RunAsync(IHost host, ILogger logger)
    {
        try
        {
            await host.RunAsync().ConfigureAwait(false);
            return ServerExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Server wurde abgebrochen und beendet sich kontrolliert.");
            return ServerExitCodes.Success;
        }
        catch (IOException exception)
        {
            logger.LogInformation(
                "Client-Pipe getrennt oder senden fehlgeschlagen ({ExceptionType}); der Server beendet sich kontrolliert.",
                exception.GetType().Name);
            return ServerExitCodes.Success;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Der Server wurde wegen eines unbehandelten Fehlers beendet.");
            return ServerExitCodes.StartupFailure;
        }
    }
}
