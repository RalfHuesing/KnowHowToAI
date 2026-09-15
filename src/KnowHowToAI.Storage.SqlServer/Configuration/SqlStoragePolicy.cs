namespace KnowHowToAI.Storage.SqlServer.Configuration;

/// <summary>
/// Immutable Policy-Record für SQL-Storage-Parameter.
/// Wird vom Composition Root aus der App-Konfiguration übergeben.
/// Storage kennt weder IConfiguration noch IOptions.
/// </summary>
public sealed record SqlStoragePolicy
{
    /// <summary>SQL-Command-Timeout in Sekunden.</summary>
    public int CommandTimeoutSeconds { get; init; }
}

