using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Hierarchy;

public sealed record Node(
    SnapshotId SnapshotId,
    NodeId NodeId,
    NodeId? ParentNodeId,
    string Title,
    string? Description,
    int SortOrder,
    bool IsDeleted) : ITombstoned;
