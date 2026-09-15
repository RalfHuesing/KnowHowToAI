namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// SQL-Storage-Parameter.
/// Ungültige Werte verhindern den Serverstart. Änderungen erfordern einen Prozessneustart.
/// </summary>
internal sealed record StorageOptions
{
    /// <summary>SQL-Command-Timeout in Sekunden.</summary>
    public int CommandTimeoutSeconds { get; init; } = 30;
}
