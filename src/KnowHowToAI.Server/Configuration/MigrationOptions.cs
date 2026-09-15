namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Migrations-Parameter.
/// Ungültige Werte verhindern den Serverstart. Änderungen erfordern einen Prozessneustart.
/// </summary>
internal sealed record MigrationOptions
{
    /// <summary>Warten auf den Migration-Lock in Sekunden.</summary>
    public int LockTimeoutSeconds { get; init; } = 60;

    /// <summary>Ausstehende Migrationen beim Serverstart automatisch anwenden.</summary>
    public bool ApplyOnStartup { get; init; } = true;
}
