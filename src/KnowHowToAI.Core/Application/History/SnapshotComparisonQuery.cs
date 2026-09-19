using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.History;

/// <summary>Parameter eines paginierten Vergleichs zweier committed Snapshots.</summary>
public sealed record SnapshotComparisonQuery(
    SnapshotId BaseSnapshotId,
    SnapshotId TargetSnapshotId,
    int? Limit = null,
    string? Cursor = null,
    NodeId? FilterNodeId = null);
