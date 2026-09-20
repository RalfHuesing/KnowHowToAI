using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Repositories;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Transactions;

/// <summary>Belegt die transaktionale Guard-Primitive für M3.3 gegen SQL Server.</summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlWorkingSnapshotMutationTests
{
    [Fact]
    public async Task ExecuteAsync_TombstonesOnlyWorkingAudienceAndIncrementsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await BeginAsync(database);
        var repository = CreateMutationRepository(database);
        var audienceRepository = new SqlAudienceRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var execution = await repository.TombstoneAudienceAsync(transaction.TransactionId, new AudienceId("Default"));
        var workingAudience = Assert.Single(await audienceRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        var currentAudience = Assert.Single(await audienceRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));

        Assert.Equal(1, execution.Value);
        Assert.Equal(1, execution.ChangeVersion);
        Assert.True(workingAudience.IsDeleted);
        Assert.False(currentAudience.IsDeleted);
        Assert.Equal(1, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    [Fact]
    public async Task ExecuteAsync_UnchangedStateKeepsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await BeginAsync(database);
        var repository = CreateMutationRepository(database);

        var changed = await repository.TombstoneAudienceAsync(transaction.TransactionId, new AudienceId("Default"));
        var unchanged = await repository.TombstoneAudienceAsync(transaction.TransactionId, new AudienceId("Default"));

        Assert.Equal(1, changed.Value);
        Assert.Equal(1, changed.ChangeVersion);
        Assert.Equal(0, unchanged.Value);
        Assert.Equal(1, unchanged.ChangeVersion);
        Assert.Equal(1, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsMissingTransaction()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var repository = CreateMutationRepository(database);

        var exception = await Assert.ThrowsAsync<WorkingSnapshotMutationRejectedException>(
            () => repository.TombstoneAudienceAsync(
                new TransactionId(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")),
                new AudienceId("Default")));

        Assert.Equal("TransactionNotFound", exception.Code);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsClosedTransactionWithoutChangingWorkingSnapshot()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await BeginAsync(database);
        await SetTransactionStateAsync(database, transaction.TransactionId, "Discarded");
        var repository = CreateMutationRepository(database);

        var exception = await Assert.ThrowsAsync<WorkingSnapshotMutationRejectedException>(
            () => repository.TombstoneAudienceAsync(transaction.TransactionId, new AudienceId("Default")));

        Assert.Equal("TransactionClosed", exception.Code);
        Assert.False((await ReadAudienceAsync(database, transaction.WorkingSnapshotId, new AudienceId("Default"))).IsDeleted);
        Assert.Equal(0, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsTransactionWhoseSnapshotIsNoLongerWorking()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await BeginAsync(database);
        await SetSnapshotStateAsync(database, transaction.WorkingSnapshotId, "Discarded");
        var repository = CreateMutationRepository(database);

        var exception = await Assert.ThrowsAsync<WorkingSnapshotMutationRejectedException>(
            () => repository.TombstoneAudienceAsync(transaction.TransactionId, new AudienceId("Default")));

        Assert.Equal("WorkingSnapshotNotOpen", exception.Code);
        Assert.False((await ReadAudienceAsync(database, transaction.WorkingSnapshotId, new AudienceId("Default"))).IsDeleted);
        Assert.Equal(0, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    [Fact]
    public async Task ExecuteAsync_RollsBackMutationAndChangeVersionWhenCallbackFails()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await BeginAsync(database);
        var repository = CreateMutationRepository(database);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.TombstoneThenFailAsync(transaction.TransactionId, new AudienceId("Default")));

        Assert.False((await ReadAudienceAsync(database, transaction.WorkingSnapshotId, new AudienceId("Default"))).IsDeleted);
        Assert.Equal(0, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    [Fact]
    public async Task ExecuteAsync_SerializesParallelMutationsAndAssignsConsecutiveChangeVersions()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        await AddAudienceToCurrentSnapshotAsync(database, new AudienceId("Developer"));
        var transaction = await BeginAsync(database);
        var repository = CreateMutationRepository(database);

        var executions = await Task.WhenAll(
            repository.UpdateAudienceDescriptionAsync(transaction.TransactionId, new AudienceId("Default"), "Erste Änderung"),
            repository.UpdateAudienceDescriptionAsync(transaction.TransactionId, new AudienceId("Developer"), "Zweite Änderung"));

        Assert.Equal([1L, 2L], executions.Select(execution => execution.ChangeVersion).Order());
        Assert.Equal(2, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    private static SqlWorkingSnapshotMutationTestRepository CreateMutationRepository(SqlTestDatabase database) => new(
        database.ConnectionFactory,
        new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

    private static async Task<KnowledgeTransaction> BeginAsync(SqlTestDatabase database)
    {
        var repository = new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        return await repository.BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
    }

    private static async Task AddAudienceToCurrentSnapshotAsync(SqlTestDatabase database, AudienceId audienceId)
    {
        await ExecuteAsync(
            database,
            """
            INSERT INTO dbo.KnowHowToAI_Audience (SnapshotId, AudienceId, Name, Description, IsDeleted)
            SELECT CurrentSnapshotId, @audienceId, N'Entwicklung', N'Technische Zielgruppe', 0
            FROM dbo.KnowHowToAI_SystemState
            WHERE Id = 1;
            """,
            new SqlParameter("@audienceId", audienceId.Value));
    }

    private static async Task<AudienceProbe> ReadAudienceAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        AudienceId audienceId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IsDeleted, Description
            FROM dbo.KnowHowToAI_Audience
            WHERE SnapshotId = @snapshotId AND AudienceId = @audienceId;
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@audienceId", audienceId.Value));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new AudienceProbe(reader.GetBoolean(0), reader.GetString(1));
    }

    private static async Task<long> ReadChangeVersionAsync(SqlTestDatabase database, TransactionId transactionId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ChangeVersion
            FROM dbo.KnowHowToAI_Transaction
            WHERE TransactionId = @transactionId;
            """;
        command.Parameters.Add(new SqlParameter("@transactionId", transactionId.Value));
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static Task SetTransactionStateAsync(
        SqlTestDatabase database,
        TransactionId transactionId,
        string state) =>
        ExecuteAsync(
            database,
            "UPDATE dbo.KnowHowToAI_Transaction SET State = @state WHERE TransactionId = @transactionId;",
            new SqlParameter("@state", state),
            new SqlParameter("@transactionId", transactionId.Value));

    private static Task SetSnapshotStateAsync(SqlTestDatabase database, SnapshotId snapshotId, string state) =>
        ExecuteAsync(
            database,
            "UPDATE dbo.KnowHowToAI_Snapshot SET State = @state WHERE SnapshotId = @snapshotId;",
            new SqlParameter("@state", state),
            new SqlParameter("@snapshotId", snapshotId.Value));

    private static async Task ExecuteAsync(SqlTestDatabase database, string commandText, params SqlParameter[] parameters)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class SqlWorkingSnapshotMutationTestRepository : SqlRepository
    {
        private const string TombstoneAudienceSql = """
            UPDATE dbo.KnowHowToAI_Audience
            SET IsDeleted = 1
            WHERE SnapshotId = @snapshotId
              AND AudienceId = @audienceId
              AND IsDeleted = 0;
            """;

        private const string UpdateAudienceDescriptionSql = """
            UPDATE dbo.KnowHowToAI_Audience
            SET Description = @description
            WHERE SnapshotId = @snapshotId
              AND AudienceId = @audienceId
              AND Description <> @description;
            """;

        public SqlWorkingSnapshotMutationTestRepository(
            SqlConnectionFactory connectionFactory,
            SqlStoragePolicy storagePolicy)
            : base(connectionFactory, storagePolicy)
        {
        }

        public Task<SqlWorkingSnapshotMutationExecution<int>> TombstoneAudienceAsync(
            TransactionId transactionId,
            AudienceId audienceId) =>
            ExecuteWorkingSnapshotMutationAsync<int>(
                transactionId,
                async (context, cancellationToken) =>
                {
                    var affectedRows = await context.ExecuteAsync(
                        TombstoneAudienceSql,
                        new { snapshotId = context.WorkingSnapshotId.Value, audienceId = audienceId.Value },
                        cancellationToken);
                    return new SqlWorkingSnapshotMutationResult<int>(affectedRows, affectedRows > 0);
                });

        public Task<SqlWorkingSnapshotMutationExecution<int>> TombstoneThenFailAsync(
            TransactionId transactionId,
            AudienceId audienceId) =>
            ExecuteWorkingSnapshotMutationAsync<int>(
                transactionId,
                async (context, cancellationToken) =>
                {
                    await context.ExecuteAsync(
                        TombstoneAudienceSql,
                        new { snapshotId = context.WorkingSnapshotId.Value, audienceId = audienceId.Value },
                        cancellationToken);
                    throw new InvalidOperationException("Erzwungener Fehler nach der Mutation.");
                });

        public Task<SqlWorkingSnapshotMutationExecution<int>> UpdateAudienceDescriptionAsync(
            TransactionId transactionId,
            AudienceId audienceId,
            string description) =>
            ExecuteWorkingSnapshotMutationAsync<int>(
                transactionId,
                async (context, cancellationToken) =>
                {
                    var affectedRows = await context.ExecuteAsync(
                        UpdateAudienceDescriptionSql,
                        new
                        {
                            snapshotId = context.WorkingSnapshotId.Value,
                            audienceId = audienceId.Value,
                            description
                        },
                        cancellationToken);
                    return new SqlWorkingSnapshotMutationResult<int>(affectedRows, affectedRows > 0);
                });
    }

    private sealed record AudienceProbe(bool IsDeleted, string Description);
}
