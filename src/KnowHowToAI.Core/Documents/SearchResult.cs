namespace KnowHowToAI.Core.Documents;

public sealed record SearchResult(
    IReadOnlyList<DocumentSummary> Results,
    bool Truncated);
