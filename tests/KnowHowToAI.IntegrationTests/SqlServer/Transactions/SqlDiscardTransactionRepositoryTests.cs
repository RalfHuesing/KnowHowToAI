using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Transactions;

/// <summary>Belegt die atomare Discard-Grenze von M3.6 gegen eine manuell bereitgestellte SQL-Datenbank.</summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlDiscardTransactionRepositoryTests
{
    private static readonly QualityWarningThresholds WarningThresholds = new(4096, 25, 8);

    [Fact]
    public async Task DiscardAsync_DiscardsWorkingStateKeepsCurrentAndPreservesTombstones()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database, "20000000-0000-0000-0000-000000000001");
        await InsertTombstonesAsync(database, transaction.WorkingSnapshotId);
        var before = await ReadStateAsync(database, transaction);

        var result = await CreateRepository(database).DiscardAsync(transaction.TransactionId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionState.Discarded, result.Value!.State);
        Assert.Null(result.Value.CommittedAtUtc);
        var after = await ReadStateAsync(database, transaction);
        Assert.Equal("Discarded", after.SnapshotState);
        Assert.Equal("Discarded", after.TransactionState);
        Assert.Equal(before.CurrentSnapshotId, after.CurrentSnapshotId);
        Assert.Equal(before.SnapshotCreatedAtUtc, after.SnapshotCreatedAtUtc);
        Assert.Equal(before.TransactionCreatedAtUtc, after.TransactionCreatedAtUtc);
        Assert.Null(after.SnapshotCommittedAtUtc);
        Assert.Null(after.TransactionCommittedAtUtc);
        Assert.Equal(1, await CountTombstonesAsync(database, transaction.WorkingSnapshotId));
    }

    [Fact]
    public async Task DiscardAsync_IsStableAfterDiscardAndAfterCommit()
    {
        await using var database = await CreateDatabaseAsync();
        var discarded = await BeginAsync(database, "20000000-0000-0000-0000-000000000002");

        Assert.True((await CreateRepository(database).DiscardAsync(discarded.TransactionId)).IsSuccess);
        var repeated = await CreateRepository(database).DiscardAsync(discarded.TransactionId);
        Assert.False(repeated.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, repeated.Code);

        var committed = await BeginAsync(database, "20000000-0000-0000-0000-000000000003");
        Assert.True((await CreateRepository(database).CommitAsync(CreateCommitRequest(committed.TransactionId))).IsCommitted);
        var afterCommit = await CreateRepository(database).DiscardAsync(committed.TransactionId);
        Assert.False(afterCommit.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, afterCommit.Code);
        var state = await ReadStateAsync(database, committed);
        Assert.Equal("Committed", state.SnapshotState);
        Assert.Equal("Committed", state.TransactionState);
        Assert.Equal(committed.WorkingSnapshotId.Value, state.CurrentSnapshotId);
    }

    [Fact]
    public async Task DiscardAsync_SerializesAgainstCommitAndLeavesOneTerminalState()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database, "20000000-0000-0000-0000-000000000004");
        var repository = CreateRepository(database);

        var commitTask = repository.CommitAsync(CreateCommitRequest(transaction.TransactionId));
        var discardTask = repository.DiscardAsync(transaction.TransactionId);
        await Task.WhenAll(commitTask, discardTask);

        var commit = await commitTask;
        var discard = await discardTask;
        Assert.NotEqual(commit.IsCommitted, discard.IsSuccess);
        if (commit.IsCommitted)
        {
            Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, discard.Code);
            Assert.Equal(transaction.WorkingSnapshotId.Value, (await ReadStateAsync(database, transaction)).CurrentSnapshotId);
        }
        else
        {
            Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, commit.Error!.Code);
            Assert.Equal(transaction.BaseSnapshotId.Value, (await ReadStateAsync(database, transaction)).CurrentSnapshotId);
        }
    }

    [Fact]
    public async Task DiscardAsync_RollsBackBothStatesWhenTransactionUpdateFails()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database, "20000000-0000-0000-0000-000000000005");
        await database.ExecuteAsync("""
            CREATE TRIGGER dbo.KnowHowToAI_DiscardFailureProbe
            ON dbo.KnowHowToAI_Transaction
            AFTER UPDATE
            AS
            BEGIN
                THROW 51001, 'Injected discard failure.', 1;
            END;
            """);

        await Assert.ThrowsAsync<SqlException>(() => CreateRepository(database).DiscardAsync(transaction.TransactionId));
        var state = await ReadStateAsync(database, transaction);
        Assert.Equal("Working", state.SnapshotState);
        Assert.Equal("Open", state.TransactionState);
        Assert.Equal(transaction.BaseSnapshotId.Value, state.CurrentSnapshotId);
    }

    private static async Task<SqlTestDatabase> CreateDatabaseAsync()
    {
        var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        return database;
    }

    private static SqlTransactionRepository CreateRepository(SqlTestDatabase database) =>
        new(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

    private static Task<KnowledgeTransaction> BeginAsync(SqlTestDatabase database, string transactionId) =>
        CreateRepository(database).BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.Parse(transactionId)), null, null, "xUnit"));

    private static CommitTransactionRequest CreateCommitRequest(TransactionId transactionId) =>
        new(transactionId, null, WarningThresholds, WarnOnPossibleEmbeddedHeading: true);

    private static async Task InsertTombstonesAsync(SqlTestDatabase database, SnapshotId snapshotId)
    {
        var nodeId = Guid.Parse("20000000-0000-0000-0000-000000000011");
        await database.ExecuteAsync("""
            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES (@snapshotId, @nodeId, NULL, N'Gelöschter Knoten', NULL, 0, 1);
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES (@snapshotId, @nodeId, N'Default', @revisionId, 'Independent', N'Historischer Tombstone', 1);
            """,
            new SqlParameter("@snapshotId", snapshotId.Value),
            new SqlParameter("@nodeId", nodeId),
            new SqlParameter("@revisionId", Guid.Parse("20000000-0000-0000-0000-000000000012")));
    }

    private static async Task<int> CountTombstonesAsync(SqlTestDatabase database, SnapshotId snapshotId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM dbo.KnowHowToAI_Node AS nodeInfo
            INNER JOIN dbo.KnowHowToAI_NodeContent AS content
                ON content.SnapshotId = nodeInfo.SnapshotId
                AND content.NodeId = nodeInfo.NodeId
            WHERE nodeInfo.SnapshotId = @snapshotId
              AND nodeInfo.IsDeleted = 1
              AND content.IsDeleted = 1;
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<DiscardState> ReadStateAsync(SqlTestDatabase database, KnowledgeTransaction transaction)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT State FROM dbo.KnowHowToAI_Snapshot WHERE SnapshotId = @workingSnapshotId),
                (SELECT State FROM dbo.KnowHowToAI_Transaction WHERE TransactionId = @transactionId),
                (SELECT CurrentSnapshotId FROM dbo.KnowHowToAI_SystemState WHERE Id = 1),
                (SELECT CreatedAtUtc FROM dbo.KnowHowToAI_Snapshot WHERE SnapshotId = @workingSnapshotId),
                (SELECT CommittedAtUtc FROM dbo.KnowHowToAI_Snapshot WHERE SnapshotId = @workingSnapshotId),
                (SELECT CreatedAtUtc FROM dbo.KnowHowToAI_Transaction WHERE TransactionId = @transactionId),
                (SELECT CommittedAtUtc FROM dbo.KnowHowToAI_Transaction WHERE TransactionId = @transactionId);
            """;
        command.Parameters.Add(new SqlParameter("@workingSnapshotId", transaction.WorkingSnapshotId.Value));
        command.Parameters.Add(new SqlParameter("@transactionId", transaction.TransactionId.Value));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new DiscardState(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt64(2),
            reader.GetDateTime(3),
            reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            reader.GetDateTime(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6));
    }

    private sealed record DiscardState(
        string SnapshotState,
        string TransactionState,
        long CurrentSnapshotId,
        DateTime SnapshotCreatedAtUtc,
        DateTime? SnapshotCommittedAtUtc,
        DateTime TransactionCreatedAtUtc,
        DateTime? TransactionCommittedAtUtc);
}
