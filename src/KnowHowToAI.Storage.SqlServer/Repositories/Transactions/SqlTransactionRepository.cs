using Dapper;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

internal sealed class SqlTransactionRepository : SqlRepository, ITransactionRepository
{
    private const string BeginSql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        BEGIN TRY
            DECLARE @baseSnapshotId BIGINT;
            DECLARE @workingSnapshotId BIGINT;
            DECLARE @workingSnapshot TABLE (SnapshotId BIGINT NOT NULL);

            SELECT @baseSnapshotId = CurrentSnapshotId
            FROM dbo.KnowHowToAI_SystemState WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = 1;

            IF @baseSnapshotId IS NULL
                THROW 50000, 'KnowHowToAI_SystemState enthält keinen aktuellen Snapshot.', 1;

            INSERT INTO dbo.KnowHowToAI_Snapshot (BaseSnapshotId, State)
            OUTPUT inserted.SnapshotId INTO @workingSnapshot (SnapshotId)
            VALUES (@baseSnapshotId, 'Working');

            SELECT @workingSnapshotId = SnapshotId
            FROM @workingSnapshot;

            INSERT INTO dbo.KnowHowToAI_Role (SnapshotId, RoleId, Name, Description, IsDeleted)
            SELECT @workingSnapshotId, RoleId, Name, Description, IsDeleted
            FROM dbo.KnowHowToAI_Role
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_RoleResolution (SnapshotId, RequestedRoleId, CandidateRoleId, Priority)
            SELECT @workingSnapshotId, RequestedRoleId, CandidateRoleId, Priority
            FROM dbo.KnowHowToAI_RoleResolution
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            SELECT @workingSnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
            FROM dbo.KnowHowToAI_Node
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_NodeContent (
                SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            SELECT @workingSnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
            FROM dbo.KnowHowToAI_NodeContent
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_ContentDependency (
                SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId)
            SELECT
                @workingSnapshotId,
                TargetNodeId,
                TargetRoleId,
                SourceNodeId,
                SourceRoleId,
                SourceContentRevisionId
            FROM dbo.KnowHowToAI_ContentDependency
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_Transaction (
                TransactionId,
                BaseSnapshotId,
                WorkingSnapshotId,
                State,
                Purpose,
                Actor,
                Client)
            VALUES (
                @transactionId,
                @baseSnapshotId,
                @workingSnapshotId,
                'Open',
                @purpose,
                @actor,
                @client);

            COMMIT TRANSACTION;

            SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
                   CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
            FROM dbo.KnowHowToAI_Transaction
            WHERE TransactionId = @transactionId;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0
                ROLLBACK TRANSACTION;

            THROW;
        END CATCH;
        """;

    private const string FindSql = """
        SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
               CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
        FROM dbo.KnowHowToAI_Transaction
        WHERE TransactionId = @transactionId;
        """;

    public SqlTransactionRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<KnowledgeTransaction> BeginAsync(
        BeginTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleAsync<TransactionRow>(
            CreateCommand(
                BeginSql,
                new
                {
                    transactionId = request.TransactionId.Value,
                    request.Purpose,
                    request.Actor,
                    request.Client
                },
                cancellationToken)).ConfigureAwait(false);
        return SqlRowMapper.ToTransaction(row);
    }

    public async Task<KnowledgeTransaction?> FindAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<TransactionRow>(
            CreateCommand(FindSql, new { transactionId = transactionId.Value }, cancellationToken)).ConfigureAwait(false);
        return row is null ? null : SqlRowMapper.ToTransaction(row);
    }
}
