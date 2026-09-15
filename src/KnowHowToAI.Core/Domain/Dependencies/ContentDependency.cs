using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Dependencies;

public sealed record ContentDependency(
    SnapshotId SnapshotId,
    NodeId TargetNodeId,
    RoleId TargetRoleId,
    NodeId SourceNodeId,
    RoleId SourceRoleId,
    ContentRevisionId SourceContentRevisionId);
