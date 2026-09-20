using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Dependencies;

public sealed record ContentDependency(
    SnapshotId SnapshotId,
    NodeId TargetNodeId,
    AudienceId TargetAudienceId,
    NodeId SourceNodeId,
    AudienceId SourceAudienceId,
    ContentRevisionId SourceContentRevisionId);
