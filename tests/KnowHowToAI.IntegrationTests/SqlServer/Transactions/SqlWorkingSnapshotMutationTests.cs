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
    public async Task ExecuteAsync_TombstonesOnlyWorkingRoleAndIncrementsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await BeginAsync(database);
        var repository = CreateMutationRepository(database);
        var roleRepository = new SqlRoleRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var execution = await repository.TombstoneRoleAsync(transaction.TransactionId, new AudienceId("Default"));
        var workingRole = Assert.Single(await roleRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        var currentRole = Assert.Single(await roleRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));

        Assert.Equal(1, execution.Value);
        Assert.Equal(1, execution.ChangeVersion);
        Assert.True(workingRole.IsDeleted);
        Assert.False(currentRole.IsDeleted);
        Assert.Equal(1, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    [Fact]
    public async Task ExecuteAsync_UnchangedStateKeepsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await BeginAsync(database);
        var repository = CreateMutationRepository(database);

        var changed = await repository.TombstoneRoleAsync(transaction.TransactionId, new AudienceId("Default"));
        var unchanged = await repository.TombstoneRoleAsync(transaction.TransactionId, new AudienceId("Default"));

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
            () => repository.TombstoneRoleAsync(
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
            () => repository.TombstoneRoleAsync(transaction.TransactionId, new AudienceId("Default")));

        Assert.Equal("TransactionClosed", exception.Code);
        Assert.False((await ReadRoleAsync(database, transaction.WorkingSnapshotId, new AudienceId("Default"))).IsDeleted);
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
            () => repository.TombstoneRoleAsync(transaction.TransactionId, new AudienceId("Default")));

        Assert.Equal("WorkingSnapshotNotOpen", exception.Code);
        Assert.False((await ReadRoleAsync(database, transaction.WorkingSnapshotId, new AudienceId("Default"))).IsDeleted);
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

        Assert.False((await ReadRoleAsync(database, transaction.WorkingSnapshotId, new AudienceId("Default"))).IsDeleted);
        Assert.Equal(0, await ReadChangeVersionAsync(database, transaction.TransactionId));
    }

    [Fact]
    public async Task ExecuteAsync_SerializesParallelMutationsAndAssignsConsecutiveChangeVersions()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        await AddRoleToCurrentSnapshotAsync(database, new AudienceId("Developer"));
        var transaction = await BeginAsync(database);
        var repository = CreateMutationRepository(database);

        var executions = await Task.WhenAll(
            repository.UpdateRoleDescriptionAsync(transaction.TransactionId, new AudienceId("Default"), "Erste Änderung"),
            repository.UpdateRoleDescriptionAsync(transaction.TransactionId, new AudienceId("Developer"), "Zweite Änderung"));

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

    private static async Task AddRoleToCurrentSnapshotAsync(SqlTestDatabase database, AudienceId roleId)
    {
        await ExecuteAsync(
            database,
            """
            INSERT INTO dbo.KnowHowToAI_Role (SnapshotId, AudienceId, Name, Description, IsDeleted)
            SELECT CurrentSnapshotId, @roleId, N'Entwicklung', N'Technische Rolle', 0
            FROM dbo.KnowHowToAI_SystemState
            WHERE Id = 1;
            """,
            new SqlParameter("@roleId", roleId.Value));
    }

    private static async Task<RoleProbe> ReadRoleAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        AudienceId roleId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IsDeleted, Description
            FROM dbo.KnowHowToAI_Role
            WHERE SnapshotId = @snapshotId AND AudienceId = @roleId;
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@roleId", roleId.Value));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new RoleProbe(reader.GetBoolean(0), reader.GetString(1));
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
        private const string TombstoneRoleSql = """
            UPDATE dbo.KnowHowToAI_Role
            SET IsDeleted = 1
            WHERE SnapshotId = @snapshotId
              AND AudienceId = @roleId
              AND IsDeleted = 0;
            """;

        private const string UpdateRoleDescriptionSql = """
            UPDATE dbo.KnowHowToAI_Role
            SET Description = @description
            WHERE SnapshotId = @snapshotId
              AND AudienceId = @roleId
              AND Description <> @description;
            """;

        public SqlWorkingSnapshotMutationTestRepository(
            SqlConnectionFactory connectionFactory,
            SqlStoragePolicy storagePolicy)
            : base(connectionFactory, storagePolicy)
        {
        }

        public Task<SqlWorkingSnapshotMutationExecution<int>> TombstoneRoleAsync(
            TransactionId transactionId,
            AudienceId roleId) =>
            ExecuteWorkingSnapshotMutationAsync<int>(
                transactionId,
                async (context, cancellationToken) =>
                {
                    var affectedRows = await context.ExecuteAsync(
                        TombstoneRoleSql,
                        new { snapshotId = context.WorkingSnapshotId.Value, roleId = roleId.Value },
                        cancellationToken);
                    return new SqlWorkingSnapshotMutationResult<int>(affectedRows, affectedRows > 0);
                });

        public Task<SqlWorkingSnapshotMutationExecution<int>> TombstoneThenFailAsync(
            TransactionId transactionId,
            AudienceId roleId) =>
            ExecuteWorkingSnapshotMutationAsync<int>(
                transactionId,
                async (context, cancellationToken) =>
                {
                    await context.ExecuteAsync(
                        TombstoneRoleSql,
                        new { snapshotId = context.WorkingSnapshotId.Value, roleId = roleId.Value },
                        cancellationToken);
                    throw new InvalidOperationException("Erzwungener Fehler nach der Mutation.");
                });

        public Task<SqlWorkingSnapshotMutationExecution<int>> UpdateRoleDescriptionAsync(
            TransactionId transactionId,
            AudienceId roleId,
            string description) =>
            ExecuteWorkingSnapshotMutationAsync<int>(
                transactionId,
                async (context, cancellationToken) =>
                {
                    var affectedRows = await context.ExecuteAsync(
                        UpdateRoleDescriptionSql,
                        new
                        {
                            snapshotId = context.WorkingSnapshotId.Value,
                            roleId = roleId.Value,
                            description
                        },
                        cancellationToken);
                    return new SqlWorkingSnapshotMutationResult<int>(affectedRows, affectedRows > 0);
                });
    }

    private sealed record RoleProbe(bool IsDeleted, string Description);
}
