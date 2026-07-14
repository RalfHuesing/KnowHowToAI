namespace KnowHowToAI.Core.Configuration;

// Bindung an den "KnowHowToAi"-Abschnitt in appsettings.json.
// Siehe docs/03-Projektstruktur-und-Konfiguration.md, Abschnitt 2.
public sealed record KnowHowToAiOptions
{
    public required string DocsRootPath { get; init; }
    public required string ConnectionString { get; init; }
    public string ExportMarkerFileName { get; init; } = ".knowhowtoai-export-marker.json";
}
