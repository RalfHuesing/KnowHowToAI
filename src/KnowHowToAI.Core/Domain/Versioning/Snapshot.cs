using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Versioning;

public sealed record Snapshot(
    SnapshotId SnapshotId,
    SnapshotId? BaseSnapshotId,
    SnapshotState State,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CommittedAtUtc,
    SnapshotCommitMetadata? CommitMetadata = null);
