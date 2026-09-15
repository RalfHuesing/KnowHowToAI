using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Transactions;

/// <summary>Belegt die atomare Commit-Grenze von M3.5 gegen eine manuell bereitgestellte SQL-Datenbank.</summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlCommitTransactionRepositoryTests
{
    private static readonly QualityWarningThresholds WarningThresholds = new(4096, 25, 8);

    [Fact]
    public async Task CommitAsync_RejectsHardValidationErrorsWithoutStateChanges()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database, "10000000-0000-0000-0000-000000000001");
        await database.ExecuteAsync("""
            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES (@snapshotId, @nodeId, NULL, N'Ungültig', NULL, 0, 0);
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES (@snapshotId, @nodeId, N'Default', @revisionId, 'Independent', N'# Verbotene Überschrift', 0);
            """,
            new SqlParameter("@snapshotId", transaction.WorkingSnapshotId.Value),
            new SqlParameter("@nodeId", Guid.Parse("10000000-0000-0000-0000-000000000011")),
            new SqlParameter("@revisionId", Guid.Parse("10000000-0000-0000-0000-000000000012")));

        var result = await CreateRepository(database).CommitAsync(CreateRequest(transaction.TransactionId));

        Assert.False(result.IsCommitted);
        Assert.Equal("HeadingNotAllowed", result.Error!.Code);
        Assert.Contains(result.ValidationReport!.Errors, error => error.Code == "HeadingNotAllowed");
        await AssertStateAsync(database, transaction, "Working", "Open", transaction.BaseSnapshotId);
    }

    [Fact]
    public async Task CommitAsync_ReturnsStructuredConflictWithoutStateChanges()
    {
        await using var database = await CreateDatabaseAsync();
        var winner = await BeginAsync(database, "10000000-0000-0000-0000-000000000002");
        var loser = await BeginAsync(database, "10000000-0000-0000-0000-000000000003");

        var winnerResult = await CreateRepository(database).CommitAsync(CreateRequest(winner.TransactionId));
        var loserResult = await CreateRepository(database).CommitAsync(CreateRequest(loser.TransactionId));

        Assert.True(winnerResult.IsCommitted);
        Assert.False(loserResult.IsCommitted);
        Assert.Equal(TransactionValidationErrorCodes.SnapshotConflict, loserResult.Error!.Code);
        Assert.Equal(loser.BaseSnapshotId.ToString(), loserResult.Error.Details[TransactionValidationErrorCodes.BaseSnapshotIdDetail]);
        Assert.Equal(winner.WorkingSnapshotId.ToString(), loserResult.Error.Details[TransactionValidationErrorCodes.CurrentSnapshotIdDetail]);
        await AssertStateAsync(database, loser, "Working", "Open", winner.WorkingSnapshotId);
    }

    [Fact]
    public async Task CommitAsync_AllowsExactlyOneConcurrentWinner()
    {
        await using var database = await CreateDatabaseAsync();
        var first = await BeginAsync(database, "10000000-0000-0000-0000-000000000004");
        var second = await BeginAsync(database, "10000000-0000-0000-0000-000000000005");

        var results = await Task.WhenAll(
            CreateRepository(database).CommitAsync(CreateRequest(first.TransactionId)),
            CreateRepository(database).CommitAsync(CreateRequest(second.TransactionId)));

        var committed = Assert.Single(results.Where(result => result.IsCommitted));
        var conflict = Assert.Single(results.Where(result => !result.IsCommitted));
        Assert.Equal(TransactionValidationErrorCodes.SnapshotConflict, conflict.Error!.Code);
        Assert.Equal(committed.Transaction!.WorkingSnapshotId.ToString(), conflict.Error.Details[TransactionValidationErrorCodes.CurrentSnapshotIdDetail]);
    }

    [Fact]
    public async Task CommitAsync_CommitsAllStatesWithOneTimestampAndReturnsNonBlockingStaleFindings()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database, "10000000-0000-0000-0000-000000000006");
        await InsertStaleDerivedContentAsync(database, transaction.WorkingSnapshotId);

        var result = await CreateRepository(database).CommitAsync(CreateRequest(transaction.TransactionId, "Stale bleibt sichtbar"));

        Assert.True(result.IsCommitted);
        Assert.NotEmpty(result.ValidationReport!.StaleContents);
        Assert.NotEmpty(result.ValidationReport.Warnings);
        Assert.Equal("Committed", result.Transaction!.State.ToString());
        Assert.NotNull(result.Transaction.CommittedAtUtc);
        await AssertCommittedTimestampsAsync(database, transaction, result.Transaction.CommittedAtUtc!.Value);

        var repeated = await CreateRepository(database).CommitAsync(CreateRequest(transaction.TransactionId));
        Assert.False(repeated.IsCommitted);
        Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, repeated.Error!.Code);

        var discarded = await BeginAsync(database, "10000000-0000-0000-0000-000000000008");
        await database.ExecuteAsync("""
            UPDATE dbo.KnowHowToAI_Snapshot SET State = 'Discarded' WHERE SnapshotId = @workingSnapshotId;
            UPDATE dbo.KnowHowToAI_Transaction SET State = 'Discarded' WHERE TransactionId = @transactionId;
            """,
            new SqlParameter("@workingSnapshotId", discarded.WorkingSnapshotId.Value),
            new SqlParameter("@transactionId", discarded.TransactionId.Value));
        var afterDiscard = await CreateRepository(database).CommitAsync(CreateRequest(discarded.TransactionId));
        Assert.False(afterDiscard.IsCommitted);
        Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, afterDiscard.Error!.Code);
    }

    [Fact]
    public async Task CommitAsync_RollsBackAllStateWhenActivationFails()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database, "10000000-0000-0000-0000-000000000007");
        await database.ExecuteAsync("""
            CREATE TRIGGER dbo.KnowHowToAI_CommitFailureProbe
            ON dbo.KnowHowToAI_Snapshot
            AFTER UPDATE
            AS
            BEGIN
                THROW 51000, 'Injected commit failure.', 1;
            END;
            """);

        await Assert.ThrowsAsync<SqlException>(() => CreateRepository(database).CommitAsync(CreateRequest(transaction.TransactionId)));
        await AssertStateAsync(database, transaction, "Working", "Open", transaction.BaseSnapshotId);
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

    private static CommitTransactionRequest CreateRequest(TransactionId transactionId, string? commitMessage = null) =>
        new(transactionId, commitMessage, WarningThresholds, WarnOnPossibleEmbeddedHeading: true);

    private static async Task InsertStaleDerivedContentAsync(SqlTestDatabase database, SnapshotId snapshotId)
    {
        var sourceNodeId = Guid.Parse("10000000-0000-0000-0000-000000000021");
        var targetNodeId = Guid.Parse("10000000-0000-0000-0000-000000000022");
        var sourceRevisionId = Guid.Parse("10000000-0000-0000-0000-000000000023");
        var targetRevisionId = Guid.Parse("10000000-0000-0000-0000-000000000024");
        var outdatedSourceRevisionId = Guid.Parse("10000000-0000-0000-0000-000000000025");
        await database.ExecuteAsync("""
            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES
                (@snapshotId, @sourceNodeId, NULL, N'Quelle', NULL, 0, 0),
                (@snapshotId, @targetNodeId, @sourceNodeId, N'Abgeleitet', NULL, 0, 0);
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES
                (@snapshotId, @sourceNodeId, N'Default', @sourceRevisionId, 'Independent', N'Quellinhalt', 0),
                (@snapshotId, @targetNodeId, N'Default', @targetRevisionId, 'Derived', N'Abgeleiteter Inhalt', 0);
            INSERT INTO dbo.KnowHowToAI_ContentDependency (
                SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId)
            VALUES (@snapshotId, @targetNodeId, N'Default', @sourceNodeId, N'Default', @outdatedSourceRevisionId);
            """,
            new SqlParameter("@snapshotId", snapshotId.Value),
            new SqlParameter("@sourceNodeId", sourceNodeId),
            new SqlParameter("@targetNodeId", targetNodeId),
            new SqlParameter("@sourceRevisionId", sourceRevisionId),
            new SqlParameter("@targetRevisionId", targetRevisionId),
            new SqlParameter("@outdatedSourceRevisionId", outdatedSourceRevisionId));
    }

    private static async Task AssertStateAsync(
        SqlTestDatabase database,
        KnowledgeTransaction transaction,
        string expectedSnapshotState,
        string expectedTransactionState,
        SnapshotId expectedCurrentSnapshotId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT State FROM dbo.KnowHowToAI_Snapshot WHERE SnapshotId = @workingSnapshotId),
                (SELECT State FROM dbo.KnowHowToAI_Transaction WHERE TransactionId = @transactionId),
                (SELECT CurrentSnapshotId FROM dbo.KnowHowToAI_SystemState WHERE Id = 1);
            """;
        command.Parameters.Add(new SqlParameter("@workingSnapshotId", transaction.WorkingSnapshotId.Value));
        command.Parameters.Add(new SqlParameter("@transactionId", transaction.TransactionId.Value));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(expectedSnapshotState, reader.GetString(0));
        Assert.Equal(expectedTransactionState, reader.GetString(1));
        Assert.Equal(expectedCurrentSnapshotId.Value, reader.GetInt64(2));
    }

    private static async Task AssertCommittedTimestampsAsync(
        SqlTestDatabase database,
        KnowledgeTransaction transaction,
        DateTimeOffset expectedTimestamp)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT CommittedAtUtc FROM dbo.KnowHowToAI_Snapshot WHERE SnapshotId = @workingSnapshotId),
                (SELECT CommittedAtUtc FROM dbo.KnowHowToAI_Transaction WHERE TransactionId = @transactionId),
                (SELECT LastUpdatedUtc FROM dbo.KnowHowToAI_SystemState WHERE Id = 1);
            """;
        command.Parameters.Add(new SqlParameter("@workingSnapshotId", transaction.WorkingSnapshotId.Value));
        command.Parameters.Add(new SqlParameter("@transactionId", transaction.TransactionId.Value));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var expected = expectedTimestamp.UtcDateTime;
        Assert.Equal(expected, reader.GetDateTime(0));
        Assert.Equal(expected, reader.GetDateTime(1));
        Assert.Equal(expected, reader.GetDateTime(2));
    }

}
