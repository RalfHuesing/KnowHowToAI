namespace KnowHowToAI.Storage.SqlServer.Configuration;

/// <summary>
/// Kapselt den Connection String für den SQL-Server-Storage.
/// Wird vom Composition Root übergeben; Storage loggt, serialisiert oder exponiert
/// den Wert niemals.
/// </summary>
public sealed record SqlStorageConnectionString
{
    /// <summary>Der tatsächliche Connection String.</summary>
    public required string Value { get; init; }
}
