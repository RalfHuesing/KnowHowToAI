using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>Paginierte, absteigend nach Snapshot-ID sortierte Historienseite.</summary>
public sealed record SnapshotPage(
    IReadOnlyList<Snapshot> Items,
    string? NextCursor);
