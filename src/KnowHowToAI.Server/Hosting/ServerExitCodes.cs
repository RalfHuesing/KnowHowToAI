namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Zentrale Prozess-Exitcodes des STDIO-Servers.
/// </summary>
internal static class ServerExitCodes
{
    /// <summary>Reguläres Ende, Shutdown, Abbruch oder getrennte Client-Pipe.</summary>
    public const int Success = 0;

    /// <summary>Start- oder Betriebsfehler, der das Ende des Servers erzwingt.</summary>
    public const int StartupFailure = 1;
}
