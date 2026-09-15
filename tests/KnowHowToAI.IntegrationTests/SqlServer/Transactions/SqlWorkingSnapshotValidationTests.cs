using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Repositories;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Transactions;

/// <summary>Belegt die atomare Validierungsansicht gegen die manuell bereitgestellte SQL-Datenbank.</summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlWorkingSnapshotValidationTests
{
    [Fact]
    public async Task ValidateAsync_OverlappingMutationReturnsOnlyTheCompletedWorkingState()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database);
        await InsertCompleteValidationGraphAsync(database, transaction.WorkingSnapshotId);
        var service = CreateService(database);
        var initialView = await CreateValidationRepository(database).ReadOpenWorkingAsync(transaction.TransactionId);
        var initial = await service.ValidateAsync(transaction.TransactionId);
        var afterInitialValidation = await ReadStateAsync(database, transaction);

        Assert.True(initialView.IsSuccess);
        Assert.Equal(2, initialView.Value!.Nodes.Count);
        Assert.Single(initialView.Value.Roles);
        Assert.Single(initialView.Value.RoleResolutions);
        Assert.Equal(2, initialView.Value.Contents.Count);
        Assert.Single(initialView.Value.Dependencies);
        Assert.True(initial.IsSuccess);
        Assert.True(initial.Value!.IsValid);
        Assert.Equal(new TransactionStateProbe("Open", "Working", 0), afterInitialValidation);

        var mutation = new BlockingRoleTombstoneRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var mutationTask = mutation.TombstoneAfterReleaseAsync(transaction.TransactionId, new RoleId("Default"));
        await mutation.WaitUntilUpdatedAsync();

        var validationTask = service.ValidateAsync(transaction.TransactionId);
        mutation.Release();
        await mutationTask;
        var overlapping = await validationTask;
        var beforeRepeatedValidation = await ReadStateAsync(database, transaction);
        var repeated = await service.ValidateAsync(transaction.TransactionId);
        var afterRepeatedValidation = await ReadStateAsync(database, transaction);

        Assert.True(overlapping.IsSuccess);
        Assert.False(overlapping.Value!.IsValid);
        Assert.Contains(overlapping.Value.Errors, error => error.Code == "RequestedRoleDeleted");
        Assert.Equal(
            overlapping.Value.Errors.Select(CreateIssueFingerprint),
            repeated.Value!.Errors.Select(CreateIssueFingerprint));
        Assert.Equal(
            overlapping.Value.Warnings.Select(CreateIssueFingerprint),
            repeated.Value.Warnings.Select(CreateIssueFingerprint));
        Assert.Equal(overlapping.Value.StaleContents, repeated.Value.StaleContents);
        Assert.Equal(
            overlapping.Value.RefactoringCandidates.Select(candidate => (candidate.NodeId, candidate.ReasonCodes.ToArray())),
            repeated.Value.RefactoringCandidates.Select(candidate => (candidate.NodeId, candidate.ReasonCodes.ToArray())));
        Assert.Equal(new TransactionStateProbe("Open", "Working", 1), beforeRepeatedValidation);
        Assert.Equal(beforeRepeatedValidation, afterRepeatedValidation);
    }

    [Fact]
    public async Task ValidateAsync_RejectsUnavailableWorkingTransactionWithoutStateChanges()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database);
        var service = CreateService(database);

        var missing = await service.ValidateAsync(new TransactionId(Guid.Parse("30000000-0000-0000-0000-000000000001")));
        await SetTransactionStateAsync(database, transaction.TransactionId, "Discarded");
        var closed = await service.ValidateAsync(transaction.TransactionId);

        var notWorking = await BeginAsync(database);
        await SetSnapshotStateAsync(database, notWorking.WorkingSnapshotId, "Discarded");
        var rejectedSnapshot = await service.ValidateAsync(notWorking.TransactionId);

        Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, missing.Code);
        Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, closed.Code);
        Assert.Equal(TransactionValidationErrorCodes.WorkingSnapshotNotOpen, rejectedSnapshot.Code);
        Assert.Equal(new TransactionStateProbe("Discarded", "Working", 0), await ReadStateAsync(database, transaction));
        Assert.Equal(new TransactionStateProbe("Open", "Discarded", 0), await ReadStateAsync(database, notWorking));
    }

    private static async Task<SqlTestDatabase> CreateDatabaseAsync()
    {
        var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        return database;
    }

    private static TransactionService CreateService(SqlTestDatabase database) => new(
        new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 }),
        CreateValidationRepository(database),
        new ValidationPolicy
        {
            ContentSizeWarningBytes = 4096,
            ChildCountWarning = 25,
            HierarchyDepthWarning = 8,
            PossibleEmbeddedHeadingWarning = true
        });

    private static SqlWorkingSnapshotValidationDataRepository CreateValidationRepository(SqlTestDatabase database) => new(
        database.ConnectionFactory,
        new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

    private static Task<KnowledgeTransaction> BeginAsync(SqlTestDatabase database) =>
        new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));

    private static Task InsertCompleteValidationGraphAsync(SqlTestDatabase database, SnapshotId snapshotId) =>
        database.ExecuteAsync(
            """
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
            VALUES (@snapshotId, @targetNodeId, N'Default', @sourceNodeId, N'Default', @sourceRevisionId);
            """,
            new SqlParameter("@snapshotId", snapshotId.Value),
            new SqlParameter("@sourceNodeId", Guid.Parse("40000000-0000-0000-0000-000000000001")),
            new SqlParameter("@targetNodeId", Guid.Parse("40000000-0000-0000-0000-000000000002")),
            new SqlParameter("@sourceRevisionId", Guid.Parse("40000000-0000-0000-0000-000000000003")),
            new SqlParameter("@targetRevisionId", Guid.Parse("40000000-0000-0000-0000-000000000004")));

    private static Task SetTransactionStateAsync(SqlTestDatabase database, TransactionId transactionId, string state) =>
        database.ExecuteAsync(
            "UPDATE dbo.KnowHowToAI_Transaction SET State = @state WHERE TransactionId = @transactionId;",
            new SqlParameter("@state", state),
            new SqlParameter("@transactionId", transactionId.Value));

    private static Task SetSnapshotStateAsync(SqlTestDatabase database, SnapshotId snapshotId, string state) =>
        database.ExecuteAsync(
            "UPDATE dbo.KnowHowToAI_Snapshot SET State = @state WHERE SnapshotId = @snapshotId;",
            new SqlParameter("@state", state),
            new SqlParameter("@snapshotId", snapshotId.Value));

    private static async Task<TransactionStateProbe> ReadStateAsync(SqlTestDatabase database, KnowledgeTransaction transaction)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT transactionRow.State, snapshotRow.State, transactionRow.ChangeVersion
            FROM dbo.KnowHowToAI_Transaction AS transactionRow
            INNER JOIN dbo.KnowHowToAI_Snapshot AS snapshotRow
                ON snapshotRow.SnapshotId = transactionRow.WorkingSnapshotId
            WHERE transactionRow.TransactionId = @transactionId;
            """;
        command.Parameters.Add(new SqlParameter("@transactionId", transaction.TransactionId.Value));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new TransactionStateProbe(reader.GetString(0), reader.GetString(1), reader.GetInt64(2));
    }

    private static string CreateIssueFingerprint(DomainIssue issue) => string.Join(
        "|",
        issue.Code,
        issue.Message,
        string.Join(",", issue.Details.OrderBy(detail => detail.Key, StringComparer.Ordinal)
            .Select(detail => string.Concat(detail.Key, "=", detail.Value))));

    private sealed record TransactionStateProbe(string TransactionState, string SnapshotState, long ChangeVersion);

    private sealed class BlockingRoleTombstoneRepository : SqlRepository
    {
        private const string TombstoneSql = """
            UPDATE dbo.KnowHowToAI_Role
            SET IsDeleted = 1
            WHERE SnapshotId = @snapshotId AND RoleId = @roleId AND IsDeleted = 0;
            """;

        private readonly TaskCompletionSource _updated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingRoleTombstoneRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
            : base(connectionFactory, storagePolicy)
        {
        }

        public Task TombstoneAfterReleaseAsync(TransactionId transactionId, RoleId roleId) =>
            ExecuteWorkingSnapshotMutationAsync(
                transactionId,
                async (context, cancellationToken) =>
                {
                    var affectedRows = await context.ExecuteAsync(
                        TombstoneSql,
                        new { snapshotId = context.WorkingSnapshotId.Value, roleId = roleId.Value },
                        cancellationToken);
                    _updated.SetResult();
                    await _release.Task.WaitAsync(cancellationToken);
                    return new SqlWorkingSnapshotMutationResult<int>(affectedRows, affectedRows > 0);
                });

        public Task WaitUntilUpdatedAsync() => _updated.Task;

        public void Release() => _release.SetResult();
    }
}
