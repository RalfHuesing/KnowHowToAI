namespace KnowHowToAI.Core.Application.Policies;

/// <summary>
/// Immutable Policy-Record für Retrieval- und Paging-Grenzen.
/// Wird vom Composition Root aus der App-Konfiguration übergeben.
/// Domain und Application kennen weder IConfiguration noch IOptions.
/// </summary>
public sealed record RetrievalPolicy
{
    /// <summary>Standardseitengröße für Listen (list_children etc.).</summary>
    public int DefaultPageSize { get; init; }

    /// <summary>Größte akzeptierte Listenseite.</summary>
    public int MaximumPageSize { get; init; }

    /// <summary>Standardseitengröße für Suchergebnisse.</summary>
    public int SearchPageSize { get; init; }

    /// <summary>Größte akzeptierte Such-Seite.</summary>
    public int SearchMaximumPageSize { get; init; }

    /// <summary>Maximale Länge eines Suchsnippets in Zeichen.</summary>
    public int SnippetMaximumCharacters { get; init; }
}

