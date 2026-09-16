using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;
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

        var hits = await repository.SearchAsync(request);

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

        var hits = await repository.SearchAsync(request);

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

        var hits = await repository.SearchAsync(request);

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

        var hits = await repository.SearchAsync(request);

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
        var page1 = await repository.SearchAsync(new SearchRequest(snapshotId, "Alpha", RoleDev, 2, null, 100));
        Assert.Equal(2, page1.Count);
        Assert.Equal(titleNode, page1[0].NodeId);
        Assert.Equal("Title", page1[0].HitField);
        Assert.Equal(descNode, page1[1].NodeId);
        Assert.Equal("Description", page1[1].HitField);

        // Page 2 using cursor
        var cursor = new SearchCursor(snapshotId, null, "Alpha", RoleDev, 2, page1[1].SortOrder, page1[1].NodeId).Encode();
        var page2 = await repository.SearchAsync(new SearchRequest(snapshotId, "Alpha", RoleDev, 2, cursor, 100));
        var hit = Assert.Single(page2);
        Assert.Equal(contentNode, hit.NodeId);
        Assert.Equal("Content", hit.HitField);
    }

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

    private static async Task InsertRoleAsync(SqlTestDatabase database, SnapshotId snapshotId, RoleId roleId, string name)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_Role (SnapshotId, RoleId, Name, Description, IsDeleted)
            VALUES (@snapshotId, @roleId, @name, NULL, 0);
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@roleId", roleId.Value));
        command.Parameters.Add(new SqlParameter("@name", name));
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
