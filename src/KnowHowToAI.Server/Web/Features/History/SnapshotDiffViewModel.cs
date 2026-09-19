namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>
/// Unveränderliches UI-Datenmodell für das Vergleichsergebnis zweier Snapshots.
/// </summary>
/// <param name="BaseSnapshotId">ID des Ausgangssnapshots.</param>
/// <param name="TargetSnapshotId">ID des Zielsnapshots.</param>
/// <param name="TotalCount">Gesamtanzahl der Änderungen.</param>
/// <param name="Entries">Liste der Diff-Einträge der aktuellen Seite.</param>
/// <param name="NextCursor">Opaker Cursor für die nächste Diff-Seite (falls vorhanden).</param>
public sealed record SnapshotDiffViewModel(
    long BaseSnapshotId,
    long TargetSnapshotId,
    int TotalCount,
    IReadOnlyList<SnapshotDiffEntryViewModel> Entries,
    string? NextCursor);
