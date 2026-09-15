namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Wurzel-Options für KnowHowTo AI. Wird beim Serverstart einmalig aus "KnowHowToAI"
/// gebunden und vollständig validiert. Ungültige Werte verhindern den Start ohne Secrets.
/// </summary>
internal sealed record KnowHowToAIOptions
{
    /// <summary>Konfigurationsschlüssel unter dem die Options gebunden werden.</summary>
    public const string SectionName = "KnowHowToAI";

    public ValidationOptions Validation { get; init; } = new();
    public RetrievalOptions Retrieval { get; init; } = new();
    public StorageOptions Storage { get; init; } = new();
    public MigrationOptions Migrations { get; init; } = new();
}
