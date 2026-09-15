using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;
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
    public async Task ValidateAsync_OverlappingMutationReadsOneCompletePreMutationState()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database);
        await InsertCompleteValidationGraphAsync(database, transaction.WorkingSnapshotId);
        var validationGuardRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowValidationReads = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var validationRepository = new RecordingValidationDataRepository(CreateValidationRepository(
            database,
            async cancellationToken =>
            {
                validationGuardRead.SetResult();
                await allowValidationReads.Task.WaitAsync(cancellationToken);
            }));
        var service = CreateService(database, validationRepository);
        var mutation = new CompleteValidationGraphMutationRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var validationTask = service.ValidateAsync(transaction.TransactionId);
        await validationGuardRead.Task;

        var mutationTask = mutation.MutateAsync(transaction.TransactionId);
        await mutation.WaitUntilStartedAsync();
        Assert.False(mutation.GuardAcquired.IsCompleted);

        allowValidationReads.SetResult();
        var overlapping = await validationTask;
        var mutationResult = await mutationTask;
        var afterMutationView = await CreateValidationRepository(database).ReadOpenWorkingAsync(transaction.TransactionId);
        var afterMutation = await CreateService(database).ValidateAsync(transaction.TransactionId);
        var afterRepeatedValidation = await ReadStateAsync(database, transaction);

        Assert.True(overlapping.IsSuccess);
        Assert.True(overlapping.Value!.IsValid);
        AssertCompletePreMutationView(validationRepository.LastRead);
        Assert.Equal(5, mutationResult.Value);
        Assert.Equal(1, mutationResult.ChangeVersion);
        Assert.True(afterMutationView.IsSuccess);
        AssertCompletePostMutationView(afterMutationView.Value);

        Assert.True(afterMutation.IsSuccess);
        Assert.False(afterMutation.Value!.IsValid);
        Assert.Contains(afterMutation.Value.Errors, error => error.Code == RoleResolutionErrorCodes.RequestedRoleDeleted);
        Assert.Contains(afterMutation.Value.Errors, error => error.Code == ContentStructureCodes.HeadingNotAllowed);
        Assert.Contains(afterMutation.Value.Warnings, warning => warning.Code == QualityWarningCodes.StaleDerivedContent);
        Assert.Equal(new TransactionStateProbe("Open", "Working", 1), afterRepeatedValidation);
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

    private static TransactionService CreateService(
        SqlTestDatabase database,
        IWorkingSnapshotValidationDataRepository? validationRepository = null) => new(
        new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 }),
        validationRepository ?? CreateValidationRepository(database),
        new IdentifierGenerator(),
        new ValidationPolicy
        {
            ContentSizeWarningBytes = 4096,
            ChildCountWarning = 25,
            HierarchyDepthWarning = 8,
            PossibleEmbeddedHeadingWarning = true
        });

    private static SqlWorkingSnapshotValidationDataRepository CreateValidationRepository(
        SqlTestDatabase database,
        Func<CancellationToken, Task>? afterGuardReadForTestAsync = null) => new(
        database.ConnectionFactory,
        new SqlStoragePolicy { CommandTimeoutSeconds = 30 },
        afterGuardReadForTestAsync);

    private static void AssertCompletePreMutationView(WorkingSnapshotValidationData? view)
    {
        Assert.NotNull(view);
        Assert.Collection(
            view.Nodes,
            source => Assert.False(source.IsDeleted),
            target =>
            {
                Assert.False(target.IsDeleted);
                Assert.Equal(new NodeId(Guid.Parse("40000000-0000-0000-0000-000000000001")), target.ParentNodeId);
            });
        var role = Assert.Single(view.Roles);
        Assert.Equal(new RoleId("Default"), role.RoleId);
        Assert.False(role.IsDeleted);
        var resolution = Assert.Single(view.RoleResolutions);
        Assert.Equal(1, resolution.Priority);
        Assert.Collection(
            view.Contents,
            source => Assert.Equal("Quellinhalt", source.ContentMd),
            target => Assert.Equal("Abgeleiteter Inhalt", target.ContentMd));
        var dependency = Assert.Single(view.Dependencies);
        Assert.Equal(new ContentRevisionId(Guid.Parse("40000000-0000-0000-0000-000000000003")), dependency.SourceContentRevisionId);
    }

    private static void AssertCompletePostMutationView(WorkingSnapshotValidationData? view)
    {
        Assert.NotNull(view);
        Assert.Collection(
            view.Nodes,
            source => Assert.Equal("Quelle", source.Title),
            target => Assert.Equal("Abgeleitet nach Mutation", target.Title));
        Assert.True(Assert.Single(view.Roles).IsDeleted);
        Assert.Equal(2, Assert.Single(view.RoleResolutions).Priority);
        Assert.Collection(
            view.Contents,
            source => Assert.Equal("Quellinhalt", source.ContentMd),
            target => Assert.Equal("# Nicht erlaubte Überschrift", target.ContentMd));
        Assert.Equal(
            new ContentRevisionId(Guid.Parse("40000000-0000-0000-0000-000000000006")),
            Assert.Single(view.Dependencies).SourceContentRevisionId);
    }

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

    private sealed class IdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => new(Guid.Parse("70000000-0000-0000-0000-000000000001"));

        public NodeId CreateNodeId() => new(Guid.Parse("70000000-0000-0000-0000-000000000002"));

        public ContentRevisionId CreateContentRevisionId() => new(Guid.Parse("70000000-0000-0000-0000-000000000003"));
    }

    private sealed class RecordingValidationDataRepository : IWorkingSnapshotValidationDataRepository
    {
        private readonly IWorkingSnapshotValidationDataRepository _inner;

        public RecordingValidationDataRepository(IWorkingSnapshotValidationDataRepository inner) => _inner = inner;

        public WorkingSnapshotValidationData? LastRead { get; private set; }

        public async Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
            TransactionId transactionId,
            CancellationToken cancellationToken = default)
        {
            var result = await _inner.ReadOpenWorkingAsync(transactionId, cancellationToken);
            LastRead = result.Value;
            return result;
        }
    }

    private sealed class CompleteValidationGraphMutationRepository : SqlRepository
    {
        private const string TombstoneRoleSql = """
            UPDATE dbo.KnowHowToAI_Role SET IsDeleted = 1
            WHERE SnapshotId = @snapshotId AND RoleId = N'Default' AND IsDeleted = 0;
            """;

        private const string ReprioritizeResolutionSql = """
            UPDATE dbo.KnowHowToAI_RoleResolution SET Priority = 2
            WHERE SnapshotId = @snapshotId AND RequestedRoleId = N'Default' AND CandidateRoleId = N'Default';
            """;

        private const string OrphanNodeSql = """
            UPDATE dbo.KnowHowToAI_Node SET Title = N'Abgeleitet nach Mutation'
            WHERE SnapshotId = @snapshotId AND NodeId = @targetNodeId;
            """;

        private const string AddHeadingSql = """
            UPDATE dbo.KnowHowToAI_NodeContent SET ContentMd = N'# Nicht erlaubte Überschrift'
            WHERE SnapshotId = @snapshotId AND NodeId = @targetNodeId AND RoleId = N'Default';
            """;

        private const string StaleDependencySql = """
            UPDATE dbo.KnowHowToAI_ContentDependency SET SourceContentRevisionId = @staleSourceRevisionId
            WHERE SnapshotId = @snapshotId AND TargetNodeId = @targetNodeId AND TargetRoleId = N'Default';
            """;

        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _guardAcquired = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CompleteValidationGraphMutationRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
            : base(connectionFactory, storagePolicy)
        {
        }

        public Task GuardAcquired => _guardAcquired.Task;

        public Task<SqlWorkingSnapshotMutationExecution<int>> MutateAsync(TransactionId transactionId)
        {
            _started.SetResult();
            return ExecuteWorkingSnapshotMutationAsync(
                transactionId,
                async (context, cancellationToken) =>
                {
                    _guardAcquired.SetResult();
                    var parameters = new
                    {
                        snapshotId = context.WorkingSnapshotId.Value,
                        targetNodeId = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                        staleSourceRevisionId = Guid.Parse("40000000-0000-0000-0000-000000000006")
                    };
                    var affectedRows = await context.ExecuteAsync(TombstoneRoleSql, parameters, cancellationToken);
                    affectedRows += await context.ExecuteAsync(ReprioritizeResolutionSql, parameters, cancellationToken);
                    affectedRows += await context.ExecuteAsync(OrphanNodeSql, parameters, cancellationToken);
                    affectedRows += await context.ExecuteAsync(AddHeadingSql, parameters, cancellationToken);
                    affectedRows += await context.ExecuteAsync(StaleDependencySql, parameters, cancellationToken);
                    return new SqlWorkingSnapshotMutationResult<int>(affectedRows, affectedRows == 5);
                });
        }

        public Task WaitUntilStartedAsync() => _started.Task;
    }
}
