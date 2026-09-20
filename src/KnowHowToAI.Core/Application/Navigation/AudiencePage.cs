using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>Paginiertes Ergebnis für list_audiences.</summary>
public sealed record AudiencePage(
    IReadOnlyList<Audience> Items,
    string? NextCursor,
    long? ChangeVersion = null);
