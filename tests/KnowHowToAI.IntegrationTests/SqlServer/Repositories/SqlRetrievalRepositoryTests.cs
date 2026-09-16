using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;
using KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlRetrievalRepositoryTests
{
    private static readonly RoleId RoleDev = new("Developer");
    private static readonly RoleId RoleConsultant = new("Consultant");

    [Fact]
    public async Task SearchAsync_TitleHit_ReturnsRank1WithNullSnippet()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        await InsertNodeAsync(database, snapshotId, new NodeSeed(nodeId, "SQL Server Installation", "Guide", 0));

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "Installation", null, 10, null, 100);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(nodeId, hit.NodeId);
        Assert.Equal("Title", hit.HitField);
        Assert.Null(hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_WildcardEscaping_FindsExactSpecialCharactersWithoutTreatingThemAsWildcard()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var node1 = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000001"));
        var node2 = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002"));

        await InsertNodeAsync(database, snapshotId, new NodeSeed(node1, "Save 100% money", "Promo", 1));
        await InsertNodeAsync(database, snapshotId, new NodeSeed(node2, "Save 1000 money", "Promo", 2, node1));

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "100%", null, 10, null, 100);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(node1, hit.NodeId);
        Assert.Equal("Save 100% money", hit.Title);
    }

    [Fact]
    public async Task SearchAsync_DescriptionHit_ReturnsRank2WithSnippet()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var nodeId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000001"));
        await InsertNodeAsync(database, snapshotId, new NodeSeed(nodeId, "Overview", "Contains details about database clustering options", 0));

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "clustering", null, 10, null, 50);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(nodeId, hit.NodeId);
        Assert.Equal("Description", hit.HitField);
        Assert.NotNull(hit.Snippet);
        Assert.Contains("clustering", hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_ContentHitWithRoleFallback_ReturnsRank3WithSnippetAndFallbackAvailability()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var nodeId = new NodeId(Guid.Parse("40000000-0000-0000-0000-000000000001"));

        await InsertNodeAsync(database, snapshotId, new NodeSeed(nodeId, "Node Title", "Node Description", 0));
        await InsertRoleAsync(database, snapshotId, RoleDev, "Developer");
        await InsertRoleAsync(database, snapshotId, RoleConsultant, "Consultant");
        await InsertRoleResolutionAsync(database, snapshotId, RoleConsultant, RoleDev, 1);
        await InsertContentAsync(database, snapshotId, nodeId, RoleDev, "Deep architectural secrets of the engine");

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "architectural", RoleConsultant, 10, null, 50);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(nodeId, hit.NodeId);
        Assert.Equal("Content", hit.HitField);
        Assert.Equal(Availability.Fallback, hit.Availability);
        Assert.Equal(RoleDev, hit.ResolvedRoleId);
        Assert.NotNull(hit.Snippet);
        Assert.Contains("architectural", hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_RankingAndKeysetPaging_OrdersCorrectlyAndPages()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var titleNode = new NodeId(Guid.Parse("50000000-0000-0000-0000-000000000001"));
        var descNode = new NodeId(Guid.Parse("50000000-0000-0000-0000-000000000002"));
        var contentNode = new NodeId(Guid.Parse("50000000-0000-0000-0000-000000000003"));

        await InsertRoleAsync(database, snapshotId, RoleDev, "Developer");
        await InsertRoleResolutionAsync(database, snapshotId, RoleDev, RoleDev, 1);

        await InsertNodeAsync(database, snapshotId, new NodeSeed(titleNode, "Alpha match in title", "Other text", 10));
        await InsertNodeAsync(database, snapshotId, new NodeSeed(descNode, "Beta title", "Alpha match in description", 20, titleNode));
        await InsertNodeAsync(database, snapshotId, new NodeSeed(contentNode, "Gamma title", "Gamma desc", 30, titleNode));
        await InsertContentAsync(database, snapshotId, contentNode, RoleDev, "Alpha match in content body");

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        // Page 1: limit 2
        var page1 = (await repository.SearchAsync(new SearchRequest(snapshotId, "Alpha", RoleDev, 2, null, 100))).Value!;
        Assert.Equal(2, page1.Count);
        Assert.Equal(titleNode, page1[0].NodeId);
        Assert.Equal("Title", page1[0].HitField);
        Assert.Equal(descNode, page1[1].NodeId);
        Assert.Equal("Description", page1[1].HitField);

        // Page 2 using cursor
        var cursor = new SearchCursor(snapshotId, null, "Alpha", RoleDev, 2, page1[1].SortOrder, page1[1].NodeId).Encode();
        var page2 = (await repository.SearchAsync(new SearchRequest(snapshotId, "Alpha", RoleDev, 2, cursor, 100))).Value!;
        var hit = Assert.Single(page2);
        Assert.Equal(contentNode, hit.NodeId);
        Assert.Equal("Content", hit.HitField);
    }

    [Fact]
    public async Task SearchAsync_WithRoleExplicitContentHit_ReturnsExplicitAvailabilityAndRoleData()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var nodeId = new NodeId(Guid.Parse("45000000-0000-0000-0000-000000000001"));

        await InsertRoleAsync(database, snapshotId, RoleDev, "Developer");
        await InsertRoleResolutionAsync(database, snapshotId, RoleDev, RoleDev, 1);
        await InsertNodeAsync(database, snapshotId, new NodeSeed(nodeId, "Node Title", "Node Description", 0));
        await InsertContentAsync(database, snapshotId, nodeId, RoleDev, "Explicit developer content");

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var result = (await repository.SearchAsync(new SearchRequest(snapshotId, "developer", RoleDev, 10, null, 50))).Value!;

        var hit = Assert.Single(result);
        Assert.Equal("Content", hit.HitField);
        Assert.Equal(Availability.Explicit, hit.Availability);
        Assert.Equal(RoleDev, hit.ResolvedRoleId);
        Assert.Contains(result.Roles!, role => role.RoleId == RoleDev && !role.IsDeleted);
        Assert.Contains(result.Resolutions!, resolution => resolution.RequestedRoleId == RoleDev);
    }

    [Fact]
    public async Task SearchAsync_WithoutRole_DoesNotSearchContent()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var nodeId = new NodeId(Guid.Parse("46000000-0000-0000-0000-000000000001"));
        await InsertNodeAsync(database, snapshotId, new NodeSeed(nodeId, "Overview", "Describes clustering options", 0));
        await InsertContentAsync(database, snapshotId, nodeId, new RoleId("Default"), "Content about clustering internals");

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var contentOnlyResult = (await repository.SearchAsync(new SearchRequest(snapshotId, "internals", null, 10, null, 50))).Value!;
        Assert.Empty(contentOnlyResult);

        var titleResult = (await repository.SearchAsync(new SearchRequest(snapshotId, "Overview", null, 10, null, 50))).Value!;
        var hit = Assert.Single(titleResult);
        Assert.Equal("Title", hit.HitField);
        Assert.Equal(Availability.None, hit.Availability);
        Assert.Null(hit.ResolvedRoleId);
        Assert.Null(hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_WithClosedTransaction_ReturnsStableErrorInsteadOfThrowing()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var transactionRepository = new SqlTransactionRepository(
            database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var transaction = await transactionRepository.BeginAsync(new BeginTransactionRequest(
            new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var discardResult = await transactionRepository.DiscardAsync(transaction.TransactionId);
        Assert.True(discardResult.IsSuccess);

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var result = await repository.SearchAsync(new SearchRequest(
            transaction.WorkingSnapshotId, "text", null, 10, null, 50, transaction.TransactionId));

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.TransactionClosed, result.Error!.Code);
        Assert.Equal(
            transaction.TransactionId.ToString(),
            result.Error.Details[SearchErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public async Task Search_WithUnknownRequestedRole_ReturnsRequestedRoleNotFound()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(new SearchQuery("text", RoleId: new RoleId("Missing")), new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Error!.Code);
        Assert.Equal(
            "Missing",
            result.Error.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task Search_WithDeletedRequestedRole_ReturnsRequestedRoleDeleted()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var deletedRole = new RoleId("Ghost");
        await InsertRoleAsync(database, snapshotId, deletedRole, "Ghost", isDeleted: true);
        await InsertRoleResolutionAsync(database, snapshotId, deletedRole, new RoleId("Default"), 1);

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(new SearchQuery("text", RoleId: deletedRole), new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleDeleted, result.Error!.Code);
    }

    [Fact]
    public async Task Search_WithDeletedCandidateRole_ReturnsCandidateRoleDeleted()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var deletedRole = new RoleId("Ghost");
        await InsertRoleAsync(database, snapshotId, deletedRole, "Ghost", isDeleted: true);
        await InsertRoleResolutionAsync(database, snapshotId, new RoleId("Default"), deletedRole, 2);

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(new SearchQuery("text", RoleId: new RoleId("Default")), new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleDeleted, result.Error!.Code);
    }

    [Fact]
    public async Task Search_WithoutConfiguredResolutionOrder_ReturnsSuccessWithoutContentHits()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var roleWithoutOrder = new RoleId("NoOrder");
        var nodeId = new NodeId(Guid.Parse("47000000-0000-0000-0000-000000000001"));
        await InsertRoleAsync(database, snapshotId, roleWithoutOrder, "NoOrder");
        await InsertNodeAsync(database, snapshotId, new NodeSeed(nodeId, "Neuland Overview", null, 0));
        await InsertContentAsync(database, snapshotId, nodeId, roleWithoutOrder, "Content about clustering internals");

        var service = CreateSearchService(database);
        var contentResult = await service.SearchAsync(new SearchQuery("internals", RoleId: roleWithoutOrder), new ReadContext());
        Assert.True(contentResult.IsSuccess);
        Assert.Empty(contentResult.Value!.Items);

        var titleResult = await service.SearchAsync(new SearchQuery("Neuland", RoleId: roleWithoutOrder), new ReadContext());
        Assert.True(titleResult.IsSuccess);
        Assert.Single(titleResult.Value!.Items);
    }

    [Fact]
    public async Task Search_WithFallbackAndExplicitContent_ResolvesFirstCandidateContent()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await GetCurrentSnapshotIdAsync(database);
        var fallbackNode = new NodeId(Guid.Parse("48000000-0000-0000-0000-000000000001"));
        var explicitNode = new NodeId(Guid.Parse("48000000-0000-0000-0000-000000000002"));
        var defaultRole = new RoleId("Default");

        await InsertRoleAsync(database, snapshotId, RoleDev, "Developer");
        await InsertRoleResolutionAsync(database, snapshotId, RoleDev, defaultRole, 1);
        await InsertRoleResolutionAsync(database, snapshotId, RoleDev, RoleDev, 2);
        await InsertNodeAsync(database, snapshotId, new NodeSeed(fallbackNode, "Fallback Node", null, 10));
        await InsertNodeAsync(database, snapshotId, new NodeSeed(explicitNode, "Explicit Node", null, 20, fallbackNode));
        await InsertContentAsync(database, snapshotId, fallbackNode, defaultRole, "Shared content marker");
        await InsertContentAsync(database, snapshotId, explicitNode, RoleDev, "Shared content marker");

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(new SearchQuery("marker", RoleId: RoleDev), new ReadContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        var fallbackHit = Assert.Single(result.Value.Items, hit => hit.NodeId == fallbackNode);
        Assert.Equal("Content", fallbackHit.HitField);
        Assert.Equal(Availability.Fallback, fallbackHit.Availability);
        Assert.Equal(defaultRole, fallbackHit.ResolvedRoleId);
        var explicitHit = Assert.Single(result.Value.Items, hit => hit.NodeId == explicitNode);
        Assert.Equal(Availability.Explicit, explicitHit.Availability);
        Assert.Equal(RoleDev, explicitHit.ResolvedRoleId);
    }

    private static SearchService CreateSearchService(SqlTestDatabase database) => new(
        new SqlSnapshotRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 }),
        new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 }),
        new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 }),
        new RetrievalPolicy
        {
            DefaultPageSize = 10,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 100,
            SnippetMaximumCharacters = 100
        });

    private static async Task<SnapshotId> GetCurrentSnapshotIdAsync(SqlTestDatabase database)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CurrentSnapshotId FROM dbo.KnowHowToAI_SystemState WHERE Id = 1;";
        var snapshotId = (long)(await command.ExecuteScalarAsync())!;
        return new SnapshotId(snapshotId);
    }

    private sealed record NodeSeed(
        NodeId NodeId,
        string Title,
        string? Description,
        int SortOrder,
        NodeId? ParentNodeId = null);

    private static async Task InsertNodeAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        NodeSeed seed)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES (@snapshotId, @nodeId, @parentNodeId, @title, @desc, @sortOrder, 0);
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@nodeId", seed.NodeId.Value));
        command.Parameters.Add(new SqlParameter("@parentNodeId", (object?)seed.ParentNodeId?.Value ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@title", seed.Title));
        command.Parameters.Add(new SqlParameter("@desc", (object?)seed.Description ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@sortOrder", seed.SortOrder));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertRoleAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        RoleId roleId,
        string name,
        bool isDeleted = false)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_Role (SnapshotId, RoleId, Name, Description, IsDeleted)
            VALUES (@snapshotId, @roleId, @name, NULL, @isDeleted);
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@roleId", roleId.Value));
        command.Parameters.Add(new SqlParameter("@name", name));
        command.Parameters.Add(new SqlParameter("@isDeleted", isDeleted));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertRoleResolutionAsync(SqlTestDatabase database, SnapshotId snapshotId, RoleId reqRole, RoleId candRole, int priority)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_RoleResolution (SnapshotId, RequestedRoleId, CandidateRoleId, Priority)
            VALUES (@snapshotId, @reqRole, @candRole, @priority);
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@reqRole", reqRole.Value));
        command.Parameters.Add(new SqlParameter("@candRole", candRole.Value));
        command.Parameters.Add(new SqlParameter("@priority", priority));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertContentAsync(SqlTestDatabase database, SnapshotId snapshotId, NodeId nodeId, RoleId roleId, string contentMd)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES (@snapshotId, @nodeId, @roleId, NEWID(), 'Independent', @contentMd, 0);
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@nodeId", nodeId.Value));
        command.Parameters.Add(new SqlParameter("@roleId", roleId.Value));
        command.Parameters.Add(new SqlParameter("@contentMd", contentMd));
        await command.ExecuteNonQueryAsync();
    }
}
