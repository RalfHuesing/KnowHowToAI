namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Zentrale Prozess-Exitcodes des Servers.
/// </summary>
internal static class ServerExitCodes
{
    /// <summary>Reguläres Ende, Shutdown oder Abbruch.</summary>
    public const int Success = 0;

    /// <summary>Start- oder Betriebsfehler, der das Ende des Servers erzwingt.</summary>
    public const int StartupFailure = 1;
}
