using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

/// <summary>
/// Liest eine offene Working-Transaction samt gesamter Wissensansicht kurz und konsistent unter derselben
/// Zeilensperre wie Working-Mutationen. Hält weder Verbindung noch SQL-Transaktion über den Aufruf hinaus offen.
/// </summary>
internal sealed class SqlWorkingSnapshotReadRepository : SqlRepository, IWorkingSnapshotReadRepository
{
    private readonly Func<CancellationToken, Task>? _afterGuardReadForTestAsync;

    private const string LockAndReadTransactionSql = """
        SELECT transactionRow.TransactionId,
               transactionRow.BaseSnapshotId,
               transactionRow.WorkingSnapshotId,
               transactionRow.State,
               transactionRow.ChangeVersion,
               transactionRow.CreatedAtUtc,
               transactionRow.CommittedAtUtc,
               transactionRow.Purpose,
               transactionRow.Actor,
               transactionRow.Client,
               transactionRow.CommitMessage,
               snapshotRow.State AS SnapshotState
        FROM dbo.KnowHowToAI_Transaction AS transactionRow WITH (UPDLOCK, HOLDLOCK)
        LEFT JOIN dbo.KnowHowToAI_Snapshot AS snapshotRow WITH (UPDLOCK, HOLDLOCK)
            ON snapshotRow.SnapshotId = transactionRow.WorkingSnapshotId
        WHERE transactionRow.TransactionId = @transactionId;
        """;

    private const string ListNodesSql = """
        SELECT SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
        FROM dbo.KnowHowToAI_Node
        WHERE SnapshotId = @snapshotId
        ORDER BY SortOrder, NodeId;
        """;

    private const string ListRolesSql = """
        SELECT SnapshotId, RoleId, Name, Description, IsDeleted
        FROM dbo.KnowHowToAI_Role
        WHERE SnapshotId = @snapshotId
        ORDER BY RoleId;
        """;

    private const string ListRoleResolutionsSql = """
        SELECT SnapshotId, RequestedRoleId, CandidateRoleId, Priority
        FROM dbo.KnowHowToAI_RoleResolution
        WHERE SnapshotId = @snapshotId
        ORDER BY RequestedRoleId, Priority;
        """;

    private const string ListContentsSql = """
        SELECT SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
        FROM dbo.KnowHowToAI_NodeContent
        WHERE SnapshotId = @snapshotId
        ORDER BY NodeId, RoleId;
        """;

    private const string ListDependenciesSql = """
        SELECT SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId
        FROM dbo.KnowHowToAI_ContentDependency
        WHERE SnapshotId = @snapshotId
        ORDER BY TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId;
        """;

    public SqlWorkingSnapshotReadRepository(
        SqlConnectionFactory connectionFactory,
        SqlStoragePolicy storagePolicy,
        Func<CancellationToken, Task>? afterGuardReadForTestAsync = null)
        : base(connectionFactory, storagePolicy)
    {
        _afterGuardReadForTestAsync = afterGuardReadForTestAsync;
    }

    public async Task<Result<WorkingSnapshotReadData>> ReadOpenWorkingAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseTransaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var guardRow = await connection.QuerySingleOrDefaultAsync<WorkingSnapshotReadGuardRow>(
                CreateCommand(
                    LockAndReadTransactionSql,
                    new { transactionId = transactionId.Value },
                    cancellationToken,
                    databaseTransaction)).ConfigureAwait(false);

            var error = CreateGuardError(guardRow, transactionId);
            if (error is not null)
                return await RollbackAndReturnAsync(databaseTransaction, Result<WorkingSnapshotReadData>.Failure(error))
                    .ConfigureAwait(false);

            if (_afterGuardReadForTestAsync is not null)
                await _afterGuardReadForTestAsync(cancellationToken).ConfigureAwait(false);

            var entities = await LoadWorkingSnapshotEntitiesAsync(connection, databaseTransaction, guardRow!.WorkingSnapshotId, cancellationToken)
                .ConfigureAwait(false);

            var transaction = SqlRowMapper.ToTransaction(new TransactionRow(
                guardRow.TransactionId,
                guardRow.BaseSnapshotId,
                guardRow.WorkingSnapshotId,
                guardRow.State,
                guardRow.ChangeVersion,
                guardRow.CreatedAtUtc,
                guardRow.CommittedAtUtc,
                guardRow.Purpose,
                guardRow.Actor,
                guardRow.Client,
                guardRow.CommitMessage));

            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<WorkingSnapshotReadData>.Success(new WorkingSnapshotReadData(
                transaction,
                guardRow.ChangeVersion,
                entities.Nodes,
                entities.Audiences,
                entities.Resolutions,
                entities.Contents,
                entities.Dependencies));
        }
        catch
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<(
        IReadOnlyList<Node> Nodes,
        IReadOnlyList<Audience> Audiences,
        IReadOnlyList<AudienceResolution> Resolutions,
        IReadOnlyList<NodeContent> Contents,
        IReadOnlyList<ContentDependency> Dependencies)> LoadWorkingSnapshotEntitiesAsync(
        SqlConnection connection,
        SqlTransaction databaseTransaction,
        long snapshotId,
        CancellationToken cancellationToken)
    {
        var nodeRows = await connection.QueryAsync<NodeRow>(
            CreateCommand(ListNodesSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var roleRows = await connection.QueryAsync<RoleRow>(
            CreateCommand(ListRolesSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var resRows = await connection.QueryAsync<RoleResolutionRow>(
            CreateCommand(ListRoleResolutionsSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var contentRows = await connection.QueryAsync<NodeContentRow>(
            CreateCommand(ListContentsSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var depRows = await connection.QueryAsync<ContentDependencyRow>(
            CreateCommand(ListDependenciesSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);

        return (
            nodeRows.Select(SqlRowMapper.ToNode).ToArray(),
            roleRows.Select(SqlRowMapper.ToRole).ToArray(),
            resRows.Select(SqlRowMapper.ToRoleResolution).ToArray(),
            contentRows.Select(SqlRowMapper.ToNodeContent).ToArray(),
            depRows.Select(SqlRowMapper.ToContentDependency).ToArray());
    }

    private static DomainError? CreateGuardError(WorkingSnapshotReadGuardRow? guard, TransactionId transactionId)
    {
        if (guard is null)
            return CreateTransactionError(
                ReadContextErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                transactionId);
        if (!string.Equals(guard.State, SqlPersistedValues.TransactionOpen, StringComparison.Ordinal))
            return CreateTransactionError(
                ReadContextErrorCodes.TransactionClosed,
                "Die angefragte Transaction ist nicht offen.",
                transactionId);
        return !string.Equals(guard.SnapshotState, SqlPersistedValues.SnapshotWorking, StringComparison.Ordinal)
            ? CreateTransactionError(
                TransactionValidationErrorCodes.WorkingSnapshotNotOpen,
                "Der Working Snapshot der Transaction ist nicht bearbeitbar.",
                transactionId)
            : null;
    }

    private static DomainError CreateTransactionError(string code, string message, TransactionId transactionId) =>
        new(code, message, new Dictionary<string, string>
        {
            [NavigationErrorCodes.TransactionIdDetail] = transactionId.ToString()
        });

    private static async Task<T> RollbackAndReturnAsync<T>(SqlTransaction databaseTransaction, T result)
    {
        await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        return result;
    }

    private sealed class WorkingSnapshotReadGuardRow
    {
        public Guid TransactionId { get; init; }
        public long BaseSnapshotId { get; init; }
        public long WorkingSnapshotId { get; init; }
        public string State { get; init; } = string.Empty;
        public long ChangeVersion { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime? CommittedAtUtc { get; init; }
        public string? Purpose { get; init; }
        public string? Actor { get; init; }
        public string? Client { get; init; }
        public string? CommitMessage { get; init; }
        public string? SnapshotState { get; init; }
    }
}
