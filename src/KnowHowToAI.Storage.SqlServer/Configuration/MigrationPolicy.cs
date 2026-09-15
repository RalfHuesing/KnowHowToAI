namespace KnowHowToAI.Storage.SqlServer.Configuration;

/// <summary>
/// Immutable Policy-Record für Migrations-Parameter.
/// Wird vom Composition Root aus der App-Konfiguration übergeben.
/// Storage kennt weder IConfiguration noch IOptions.
/// </summary>
public sealed record MigrationPolicy
{
    /// <summary>Warten auf den Migration-Lock in Sekunden.</summary>
    public int LockTimeoutSeconds { get; init; }

    /// <summary>Ausstehende Migrationen beim Serverstart automatisch anwenden.</summary>
    public bool ApplyOnStartup { get; init; }
}

