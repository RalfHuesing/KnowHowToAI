namespace KnowHowToAI.Server.Web.Components.Shared.Diffs;

/// <summary>Unveränderliches UI-Datenmodell für eine Seite eines strukturierten Snapshot-Diffs.</summary>
public sealed record SnapshotDiffViewModel(
    long BaseSnapshotId,
    long TargetSnapshotId,
    int TotalCount,
    IReadOnlyList<SnapshotDiffEntryViewModel> Entries,
    string? NextCursor);
