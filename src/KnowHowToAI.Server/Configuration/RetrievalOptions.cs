namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Retrieval- und Transportgrenzen für Listen, Suche und Snippets.
/// Ungültige Werte verhindern den Serverstart. Änderungen erfordern einen Prozessneustart.
/// </summary>
internal sealed record RetrievalOptions
{
    /// <summary>Standardseitengröße für Listen (list_children etc.).</summary>
    public int DefaultPageSize { get; init; } = 20;

    /// <summary>Größte akzeptierte Listenseite.</summary>
    public int MaximumPageSize { get; init; } = 100;

    /// <summary>Standardseitengröße für Suchergebnisse.</summary>
    public int SearchPageSize { get; init; } = 10;

    /// <summary>Größte akzeptierte Such-Seite.</summary>
    public int SearchMaximumPageSize { get; init; } = 50;

    /// <summary>Maximale Länge eines Suchsnippets in Zeichen.</summary>
    public int SnippetMaximumCharacters { get; init; } = 300;
}
