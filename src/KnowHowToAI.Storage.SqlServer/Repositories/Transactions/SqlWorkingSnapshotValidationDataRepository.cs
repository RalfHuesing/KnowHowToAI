using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

/// <summary>Liest die gesamte Validierungsansicht einer offenen Working-Transaction atomar.</summary>
internal sealed class SqlWorkingSnapshotValidationDataRepository : SqlRepository, IWorkingSnapshotValidationDataRepository
{
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

    public SqlWorkingSnapshotValidationDataRepository(
        SqlConnectionFactory connectionFactory,
        SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseTransaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var guard = await ReadWorkingSnapshotGuardAsync(connection, databaseTransaction, transactionId, cancellationToken)
                .ConfigureAwait(false);
            var error = CreateGuardError(guard, transactionId);
            if (error is not null)
                return await RollbackAndReturnAsync(databaseTransaction, Result<WorkingSnapshotValidationData>.Failure(error))
                    .ConfigureAwait(false);

            var parameters = new { snapshotId = guard!.WorkingSnapshotId };
            var nodes = (await connection.QueryAsync<NodeRow>(CreateCommand(ListNodesSql, parameters, cancellationToken, databaseTransaction))
                .ConfigureAwait(false)).Select(SqlRowMapper.ToNode).ToArray();
            var roles = (await connection.QueryAsync<RoleRow>(CreateCommand(ListRolesSql, parameters, cancellationToken, databaseTransaction))
                .ConfigureAwait(false)).Select(SqlRowMapper.ToRole).ToArray();
            var resolutions = (await connection.QueryAsync<RoleResolutionRow>(
                CreateCommand(ListRoleResolutionsSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false))
                .Select(SqlRowMapper.ToRoleResolution).ToArray();
            var contents = (await connection.QueryAsync<NodeContentRow>(CreateCommand(ListContentsSql, parameters, cancellationToken, databaseTransaction))
                .ConfigureAwait(false)).Select(SqlRowMapper.ToNodeContent).ToArray();
            var dependencies = (await connection.QueryAsync<ContentDependencyRow>(
                CreateCommand(ListDependenciesSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false))
                .Select(SqlRowMapper.ToContentDependency).ToArray();

            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result<WorkingSnapshotValidationData>.Success(new WorkingSnapshotValidationData(
                nodes,
                roles,
                resolutions,
                contents,
                dependencies));
        }
        catch
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static DomainError? CreateGuardError(WorkingSnapshotGuard? guard, TransactionId transactionId)
    {
        if (guard is null)
            return CreateTransactionError(
                TransactionValidationErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                transactionId);
        if (!string.Equals(guard.TransactionState, SqlPersistedValues.TransactionOpen, StringComparison.Ordinal))
            return CreateTransactionError(
                TransactionValidationErrorCodes.TransactionClosed,
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
            [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString()
        });

    private static async Task<T> RollbackAndReturnAsync<T>(SqlTransaction databaseTransaction, T result)
    {
        await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        return result;
    }
}
