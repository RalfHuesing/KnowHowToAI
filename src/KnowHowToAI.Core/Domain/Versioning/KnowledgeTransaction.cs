using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Versioning;

public sealed record KnowledgeTransaction(
    TransactionId TransactionId,
    SnapshotId BaseSnapshotId,
    SnapshotId WorkingSnapshotId,
    TransactionState State,
    long ChangeVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CommittedAtUtc,
    string? Purpose,
    string? Actor,
    string? Client,
    string? CommitMessage);
