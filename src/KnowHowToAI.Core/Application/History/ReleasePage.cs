using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>Paginiertes Ergebnis für list_releases.</summary>
public sealed record ReleasePage(
    IReadOnlyList<Release> Items,
    string? NextCursor);
