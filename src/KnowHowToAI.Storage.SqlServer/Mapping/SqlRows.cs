namespace KnowHowToAI.Storage.SqlServer.Mapping;

// Dapper-Zeilenmodelle bleiben strikt innerhalb des Storage-Adapters.
internal sealed record SnapshotRow(
    long SnapshotId,
    long? BaseSnapshotId,
    string State,
    DateTime CreatedAtUtc,
    DateTime? CommittedAtUtc,
    Guid? TransactionId = null,
    string? Actor = null,
    string? Client = null,
    string? Purpose = null,
    string? CommitMessage = null);

internal sealed record TransactionRow(
    Guid TransactionId,
    long BaseSnapshotId,
    long WorkingSnapshotId,
    string State,
    long ChangeVersion,
    DateTime CreatedAtUtc,
    DateTime? CommittedAtUtc,
    string? Purpose,
    string? Actor,
    string? Client,
    string? CommitMessage);

internal sealed record NodeRow(
    long SnapshotId,
    Guid NodeId,
    Guid? ParentNodeId,
    string Title,
    string? Description,
    int SortOrder,
    bool IsDeleted);

internal sealed record AudienceRow(
    long SnapshotId,
    string AudienceId,
    string Name,
    string? Description,
    bool IsDeleted);

internal sealed record AudienceResolutionRow(
    long SnapshotId,
    string RequestedAudienceId,
    string CandidateAudienceId,
    int Priority);

internal sealed record NodeContentRow(
    long SnapshotId,
    Guid NodeId,
    string AudienceId,
    Guid ContentRevisionId,
    string ContentMode,
    string ContentMd,
    bool IsDeleted);

internal sealed record ContentDependencyRow(
    long SnapshotId,
    Guid TargetNodeId,
    string TargetAudienceId,
    Guid SourceNodeId,
    string SourceAudienceId,
    Guid SourceContentRevisionId);

internal sealed record ReleaseRow(
    long ReleaseId,
    long SnapshotId,
    string Name,
    string? Description,
    DateTime ReleasedAtUtc);
