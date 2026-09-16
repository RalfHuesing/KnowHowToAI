using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Repositories;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;
using KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Transactions;

/// <summary>
/// Belegt die konsistente Read-Sicht für Working-Snapshots unter Zeilensperre und Cursor-Invalidierung.
/// </summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlWorkingSnapshotReadRepositoryTests
{
    private static readonly SqlStoragePolicy Policy = new() { CommandTimeoutSeconds = 30 };

    [Fact]
    public async Task ReadOpenWorkingAsync_SerializesAgainstConcurrentMutation_ReadsCompletePreOrPostMutationState()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database);
        await InsertCompleteGraphAsync(database, transaction.WorkingSnapshotId);

        var guardRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowReads = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readRepo = new SqlWorkingSnapshotReadRepository(database.ConnectionFactory, Policy, async ct =>
        {
            guardRead.SetResult();
            await allowReads.Task.WaitAsync(ct);
        });
        var mutationRepo = new GraphMutationRepository(database.ConnectionFactory, Policy);

        var readTask = readRepo.ReadOpenWorkingAsync(transaction.TransactionId);
        await guardRead.Task;

        var mutationTask = mutationRepo.MutateAsync(transaction.TransactionId);
        await mutationRepo.WaitUntilStartedAsync();
        Assert.False(mutationRepo.GuardAcquired.IsCompleted);

        allowReads.SetResult();
        var readResult = await readTask;
        var mutationResult = await mutationTask;

        Assert.True(readResult.IsSuccess);
        Assert.Equal(0, readResult.Value!.ChangeVersion);
        AssertPreMutationData(readResult.Value);

        Assert.Equal(1, mutationResult.ChangeVersion);

        var postRead = await new SqlWorkingSnapshotReadRepository(database.ConnectionFactory, Policy)
            .ReadOpenWorkingAsync(transaction.TransactionId);
        Assert.True(postRead.IsSuccess);
        Assert.Equal(1, postRead.Value!.ChangeVersion);
        AssertPostMutationData(postRead.Value);
    }

    [Fact]
    public async Task ReadOpenWorkingAsync_RejectsUnavailableWorkingTransactionWithoutStateChanges()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database);
        var readRepo = new SqlWorkingSnapshotReadRepository(database.ConnectionFactory, Policy);

        var missing = await readRepo.ReadOpenWorkingAsync(new TransactionId(Guid.Parse("30000000-0000-0000-0000-000000000001")));
        await SetTransactionStateAsync(database, transaction.TransactionId, "Discarded");
        var closed = await readRepo.ReadOpenWorkingAsync(transaction.TransactionId);

        var notWorking = await BeginAsync(database);
        await SetSnapshotStateAsync(database, notWorking.WorkingSnapshotId, "Discarded");
        var rejectedSnapshot = await readRepo.ReadOpenWorkingAsync(notWorking.TransactionId);

        Assert.Equal(ReadContextErrorCodes.TransactionNotFound, missing.Error!.Code);
        Assert.Equal(ReadContextErrorCodes.TransactionClosed, closed.Error!.Code);
        Assert.Equal(TransactionValidationErrorCodes.WorkingSnapshotNotOpen, rejectedSnapshot.Error!.Code);
    }

    private static readonly NodeId RootNodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000001"));
    private static readonly NodeId Child1NodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000002"));
    private static readonly NodeId Child2NodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000003"));

    [Fact]
    public async Task NavigationAndSearch_WithExpiredCursor_ReturnsCursorExpired()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database);
        await InsertCompleteGraphAsync(database, transaction.WorkingSnapshotId);

        var navService = CreateNavigationService(database);
        var searchService = CreateSearchService(database);

        var context = new ReadContext(TransactionId: transaction.TransactionId);
        var defaultRole = new RoleId("Default");
        var childrenPage1 = await navService.ListChildrenAsync(new ListChildrenQuery(RootNodeId, context, defaultRole, Limit: 1));
        var rolesPage1 = await navService.ListRolesAsync(new ListRolesQuery(context, Limit: 1));
        var searchPage1 = await searchService.SearchAsync(new SearchQuery("Quelle", Limit: 1), context);

        Assert.True(childrenPage1.IsSuccess);
        Assert.NotNull(childrenPage1.Value!.NextCursor);
        Assert.True(rolesPage1.IsSuccess);
        Assert.NotNull(rolesPage1.Value!.NextCursor);
        Assert.True(searchPage1.IsSuccess);
        Assert.NotNull(searchPage1.Value!.NextCursor);

        var mutationRepo = new GraphMutationRepository(database.ConnectionFactory, Policy);
        await mutationRepo.MutateAsync(transaction.TransactionId);

        var expChildren = await navService.ListChildrenAsync(
            new ListChildrenQuery(RootNodeId, context, defaultRole, Cursor: childrenPage1.Value.NextCursor));
        var expRoles = await navService.ListRolesAsync(
            new ListRolesQuery(context, Cursor: rolesPage1.Value.NextCursor));
        var expSearch = await searchService.SearchAsync(
            new SearchQuery("Quelle", Cursor: searchPage1.Value.NextCursor), context);

        Assert.Equal(NavigationErrorCodes.CursorExpired, expChildren.Error!.Code);
        Assert.Equal(NavigationErrorCodes.CursorExpired, expRoles.Error!.Code);
        Assert.Equal(SearchErrorCodes.CursorExpired, expSearch.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_SerializesWithWorkingMutation_NoMixedState()
    {
        await using var database = await CreateDatabaseAsync();
        var transaction = await BeginAsync(database);
        await InsertCompleteGraphAsync(database, transaction.WorkingSnapshotId);

        var guardRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowReads = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var retrievalRepo = new SqlRetrievalRepository(database.ConnectionFactory, Policy, async ct =>
        {
            guardRead.SetResult();
            await allowReads.Task.WaitAsync(ct);
        });

        var request = new SearchRequest(
            transaction.WorkingSnapshotId, "Quellinhalt 1", new RoleId("Default"), 10, null, 100, transaction.TransactionId);
        var searchTask = retrievalRepo.SearchAsync(request);
        await guardRead.Task;

        var mutationRepo = new GraphMutationRepository(database.ConnectionFactory, Policy);
        var mutationTask = mutationRepo.MutateAsync(transaction.TransactionId);
        await mutationRepo.WaitUntilStartedAsync();
        Assert.False(mutationRepo.GuardAcquired.IsCompleted);

        allowReads.SetResult();
        var searchResult = (await searchTask).Value!;
        var mutationResult = await mutationTask;

        Assert.Single(searchResult);
        Assert.Equal(0, searchResult.ChangeVersion);
        Assert.Equal("Quellinhalt 1", searchResult[0].Snippet);
        Assert.Equal(1, mutationResult.ChangeVersion);

        var postMutationRequest = new SearchRequest(
            transaction.WorkingSnapshotId, "Quellinhalt nach Mutation", new RoleId("Default"), 10, null, 100, transaction.TransactionId);
        var repeatedResult = (await new SqlRetrievalRepository(database.ConnectionFactory, Policy).SearchAsync(postMutationRequest)).Value!;
        Assert.Single(repeatedResult);
        Assert.Equal(1, repeatedResult.ChangeVersion);
    }

    private static async Task<SqlTestDatabase> CreateDatabaseAsync()
    {
        var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        return database;
    }

    private static Task<KnowledgeTransaction> BeginAsync(SqlTestDatabase database) =>
        new SqlTransactionRepository(database.ConnectionFactory, Policy)
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));

    private static NavigationService CreateNavigationService(SqlTestDatabase database) => new(
        new SnapshotReadRepositories(
            new SqlSnapshotRepository(database.ConnectionFactory, Policy),
            new SqlTransactionRepository(database.ConnectionFactory, Policy),
            new SqlHierarchyRepository(database.ConnectionFactory, Policy),
            new SqlContentRepository(database.ConnectionFactory, Policy),
            new SqlRoleRepository(database.ConnectionFactory, Policy),
            new SqlDependencyRepository(database.ConnectionFactory, Policy),
            new SqlWorkingSnapshotReadRepository(database.ConnectionFactory, Policy)),
        CreateRetrievalPolicy());

    private static SearchService CreateSearchService(SqlTestDatabase database, IRetrievalRepository? retrieval = null) => new(
        new SqlSnapshotRepository(database.ConnectionFactory, Policy),
        new SqlTransactionRepository(database.ConnectionFactory, Policy),
        retrieval ?? new SqlRetrievalRepository(database.ConnectionFactory, Policy),
        CreateRetrievalPolicy());

    private static RetrievalPolicy CreateRetrievalPolicy() => new()
    {
        DefaultPageSize = 10,
        MaximumPageSize = 100,
        SearchPageSize = 10,
        SearchMaximumPageSize = 100,
        SnippetMaximumCharacters = 100
    };

    private static Task InsertCompleteGraphAsync(SqlTestDatabase database, SnapshotId snapshotId) =>
        database.ExecuteAsync(
            """
            INSERT INTO dbo.KnowHowToAI_Role (SnapshotId, RoleId, Name, Description, IsDeleted)
            VALUES (@snapshotId, N'Secondary', N'Secondary Role', NULL, 0);
            INSERT INTO dbo.KnowHowToAI_RoleResolution (SnapshotId, RequestedRoleId, CandidateRoleId, Priority)
            VALUES (@snapshotId, N'Secondary', N'Secondary', 1);
            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES
                (@snapshotId, @rootNodeId, NULL, N'Wurzel', NULL, 0, 0),
                (@snapshotId, @child1NodeId, @rootNodeId, N'Quelle 1', NULL, 0, 0),
                (@snapshotId, @child2NodeId, @rootNodeId, N'Quelle 2', NULL, 1, 0);
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES
                (@snapshotId, @child1NodeId, N'Default', @rev1, 'Independent', N'Quellinhalt 1', 0),
                (@snapshotId, @child2NodeId, N'Default', @rev2, 'Derived', N'Quellinhalt 2', 0);
            INSERT INTO dbo.KnowHowToAI_ContentDependency (
                SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId)
            VALUES (@snapshotId, @child2NodeId, N'Default', @child1NodeId, N'Default', @rev1);
            """,
            new SqlParameter("@snapshotId", snapshotId.Value),
            new SqlParameter("@rootNodeId", RootNodeId.Value),
            new SqlParameter("@child1NodeId", Child1NodeId.Value),
            new SqlParameter("@child2NodeId", Child2NodeId.Value),
            new SqlParameter("@rev1", Guid.Parse("40000000-0000-0000-0000-000000000004")),
            new SqlParameter("@rev2", Guid.Parse("40000000-0000-0000-0000-000000000005")));

    private static void AssertPreMutationData(WorkingSnapshotReadData data)
    {
        Assert.Equal(3, data.Nodes.Count);
        Assert.Equal("Wurzel", data.Nodes[0].Title);
        Assert.Equal("Quelle 1", data.Nodes[1].Title);
        Assert.Equal("Quelle 2", data.Nodes[2].Title);
        Assert.Equal(2, data.Roles.Count);
        Assert.Equal(2, data.Contents.Count);
        Assert.Equal("Quellinhalt 1", data.Contents[0].ContentMd);
        Assert.Single(data.Dependencies);
    }

    private static void AssertPostMutationData(WorkingSnapshotReadData data)
    {
        Assert.Equal(3, data.Nodes.Count);
        Assert.Equal("Quelle nach Mutation", data.Nodes[1].Title);
        Assert.Equal(2, data.Contents.Count);
        Assert.Equal("Quellinhalt nach Mutation", data.Contents[0].ContentMd);
    }

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

    private sealed class GraphMutationRepository : SqlRepository
    {
        private const string UpdateNodeSql = """
            UPDATE dbo.KnowHowToAI_Node SET Title = N'Quelle nach Mutation'
            WHERE SnapshotId = @snapshotId AND NodeId = @nodeId;
            """;

        private const string UpdateContentSql = """
            UPDATE dbo.KnowHowToAI_NodeContent SET ContentMd = N'Quellinhalt nach Mutation'
            WHERE SnapshotId = @snapshotId AND NodeId = @nodeId AND RoleId = N'Default';
            """;

        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _guardAcquired = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public GraphMutationRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
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
                        nodeId = Child1NodeId.Value
                    };
                    var affectedRows = await context.ExecuteAsync(UpdateNodeSql, parameters, cancellationToken);
                    affectedRows += await context.ExecuteAsync(UpdateContentSql, parameters, cancellationToken);
                    return new SqlWorkingSnapshotMutationResult<int>(affectedRows, true);
                });
        }

        public Task WaitUntilStartedAsync() => _started.Task;
    }
}
