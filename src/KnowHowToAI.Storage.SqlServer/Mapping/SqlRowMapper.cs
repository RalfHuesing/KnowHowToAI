using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Storage.SqlServer.Mapping;

internal static class SqlRowMapper
{
    public static Snapshot ToSnapshot(SnapshotRow row) => new(
        new SnapshotId(row.SnapshotId),
        row.BaseSnapshotId is long baseSnapshotId ? new SnapshotId(baseSnapshotId) : null,
        ToSnapshotState(row.State),
        ToUtc(row.CreatedAtUtc),
        row.CommittedAtUtc is DateTime committedAtUtc ? ToUtc(committedAtUtc) : null);

    public static KnowledgeTransaction ToTransaction(TransactionRow row) => new(
        new TransactionId(row.TransactionId),
        new SnapshotId(row.BaseSnapshotId),
        new SnapshotId(row.WorkingSnapshotId),
        ToTransactionState(row.State),
        row.ChangeVersion,
        ToUtc(row.CreatedAtUtc),
        row.CommittedAtUtc is DateTime committedAtUtc ? ToUtc(committedAtUtc) : null,
        row.Purpose,
        row.Actor,
        row.Client,
        row.CommitMessage);

    public static Node ToNode(NodeRow row) => new(
        new SnapshotId(row.SnapshotId),
        new NodeId(row.NodeId),
        row.ParentNodeId is Guid parentNodeId ? new NodeId(parentNodeId) : null,
        row.Title,
        row.Description,
        row.SortOrder,
        row.IsDeleted);

    public static Role ToRole(RoleRow row) => new(
        new SnapshotId(row.SnapshotId),
        new RoleId(row.RoleId),
        row.Name,
        row.Description,
        row.IsDeleted);

    public static RoleResolution ToRoleResolution(RoleResolutionRow row) => new(
        new SnapshotId(row.SnapshotId),
        new RoleId(row.RequestedRoleId),
        new RoleId(row.CandidateRoleId),
        row.Priority);

    public static NodeContent ToNodeContent(NodeContentRow row) => new(
        new SnapshotId(row.SnapshotId),
        new NodeId(row.NodeId),
        new RoleId(row.RoleId),
        new ContentRevisionId(row.ContentRevisionId),
        ToContentMode(row.ContentMode),
        row.ContentMd,
        row.IsDeleted);

    public static ContentDependency ToContentDependency(ContentDependencyRow row) => new(
        new SnapshotId(row.SnapshotId),
        new NodeId(row.TargetNodeId),
        new RoleId(row.TargetRoleId),
        new NodeId(row.SourceNodeId),
        new RoleId(row.SourceRoleId),
        new ContentRevisionId(row.SourceContentRevisionId));

    public static Release ToRelease(ReleaseRow row) => new(
        new ReleaseId(row.ReleaseId),
        new SnapshotId(row.SnapshotId),
        row.Name,
        row.Description,
        ToUtc(row.ReleasedAtUtc));

    private static SnapshotState ToSnapshotState(string value) => value switch
    {
        SqlPersistedValues.SnapshotWorking => SnapshotState.Working,
        SqlPersistedValues.SnapshotCommitted => SnapshotState.Committed,
        SqlPersistedValues.SnapshotDiscarded => SnapshotState.Discarded,
        _ => throw InvalidValue("Snapshot.State", value)
    };

    private static TransactionState ToTransactionState(string value) => value switch
    {
        SqlPersistedValues.TransactionOpen => TransactionState.Open,
        SqlPersistedValues.TransactionCommitted => TransactionState.Committed,
        SqlPersistedValues.TransactionDiscarded => TransactionState.Discarded,
        _ => throw InvalidValue("Transaction.State", value)
    };

    private static ContentMode ToContentMode(string value) => value switch
    {
        SqlPersistedValues.ContentIndependent => ContentMode.Independent,
        SqlPersistedValues.ContentDerived => ContentMode.Derived,
        _ => throw InvalidValue("NodeContent.ContentMode", value)
    };

    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static InvalidOperationException InvalidValue(string column, string value) =>
        new($"Die Datenbank enthält einen ungültigen Wert '{value}' in {column}.");
}
