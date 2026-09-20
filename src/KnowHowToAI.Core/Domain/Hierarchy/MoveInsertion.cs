using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Hierarchy;

internal readonly record struct MoveInsertion(
    SnapshotId SnapshotId,
    NodeId NodeId,
    NodeId? TargetParentNodeId,
    int TargetIndex);
