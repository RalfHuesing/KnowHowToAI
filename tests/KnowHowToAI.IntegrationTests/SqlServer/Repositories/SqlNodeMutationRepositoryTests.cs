using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

/// <summary>Belegt die atomare Persistenz einer Node-Mutation auf einem echten Working Snapshot.</summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlNodeMutationRepositoryTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesOnlyWorkingNodeAndIncrementsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlNodeMutationRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var nodeId = new NodeId(Guid.Parse("12f1ff9d-2523-44f6-8b7e-90fc5f3bd205"));

        var result = await repository.ExecuteAsync(
            transaction.TransactionId,
            state => Result<WorkingNodeMutationDecision<NodeId>>.Success(
                new WorkingNodeMutationDecision<NodeId>(
                    nodeId,
                    state with
                    {
                        Nodes =
                        [
                            ..state.Nodes,
                            new Node(state.SnapshotId, nodeId, null, "Root", null, 0, IsDeleted: false)
                        ]
                    })));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);
        var hierarchyRepository = new SqlHierarchyRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        Assert.Empty(await hierarchyRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        var workingNode = Assert.Single(await hierarchyRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal(nodeId, workingNode.NodeId);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesAndTombstonesOnlyWorkingNodeLeavingBaseSnapshotIntact()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var nodeId = new NodeId(Guid.Parse("7a4e6123-5e92-49f3-8b7a-8f12c3d4e5f6"));
        await InsertCurrentNodeAsync(database, nodeId);

        var transaction = await new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlNodeMutationRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var result = await repository.ExecuteAsync(
            transaction.TransactionId,
            state =>
            {
                var existing = state.Nodes.Single(n => n.NodeId == nodeId);
                var updated = existing with { Title = "Aktualisiert", IsDeleted = true };
                return Result<WorkingNodeMutationDecision<NodeId>>.Success(
                    new WorkingNodeMutationDecision<NodeId>(
                        nodeId,
                        state with { Nodes = state.Nodes.Select(n => n.NodeId == nodeId ? updated : n).ToArray() }));
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);

        var hierarchyRepository = new SqlHierarchyRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var baseNode = Assert.Single(await hierarchyRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        Assert.Equal("Original", baseNode.Title);
        Assert.False(baseNode.IsDeleted);

        var workingNode = Assert.Single(await hierarchyRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal("Aktualisiert", workingNode.Title);
        Assert.True(workingNode.IsDeleted);
    }

    [Fact]
    public async Task ExecuteAsync_InsertNodeAtFrontOfPopulatedGroup_RenumbersWithoutUniqueConflict()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var rootId = new NodeId(Guid.Parse("3f5a7c21-9d4e-4a1b-8c2d-5e6f7a8b9c0d"));
        var firstChildId = new NodeId(Guid.Parse("4a6b8d32-0e5f-4b2c-9d3e-6f7a8b9c0d1e"));
        var secondChildId = new NodeId(Guid.Parse("5b7c9e43-1f60-4c3d-0e4f-7a8b9c0d1e2f"));
        var newChildId = new NodeId(Guid.Parse("6c8d0f54-2071-4d4e-1f50-8b9c0d1e2f3a"));
        await InsertNodesAsync(
            database,
            Node(new SnapshotId(0), rootId, null, 0),
            Node(new SnapshotId(0), firstChildId, rootId, 0),
            Node(new SnapshotId(0), secondChildId, rootId, 1));

        var transaction = await BeginTransactionAsync(database);
        var repository = new SqlNodeMutationRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var result = await repository.ExecuteAsync(
            transaction.TransactionId,
            state => Result<WorkingNodeMutationDecision<NodeId>>.Success(
                new WorkingNodeMutationDecision<NodeId>(
                    newChildId,
                    state with
                    {
                        Nodes =
                        [
                            Node(state.SnapshotId, rootId, null, 0),
                            Node(state.SnapshotId, newChildId, rootId, 0),
                            Node(state.SnapshotId, firstChildId, rootId, 1),
                            Node(state.SnapshotId, secondChildId, rootId, 2)
                        ]
                    })));

        Assert.True(result.IsSuccess);
        var hierarchyRepository = new SqlHierarchyRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var workingNodes = await hierarchyRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId);
        Assert.Equal(4, workingNodes.Count);
        Assert.Equal(0, workingNodes.Single(node => node.NodeId == newChildId).SortOrder);
        Assert.Equal(1, workingNodes.Single(node => node.NodeId == firstChildId).SortOrder);
        Assert.Equal(2, workingNodes.Single(node => node.NodeId == secondChildId).SortOrder);
    }

    [Fact]
    public async Task ExecuteAsync_RotateLastChildToFront_RenumbersWithoutUniqueConflict()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var rootId = new NodeId(Guid.Parse("7d9e1065-3182-4e5f-2061-9c0d1e2f3a4b"));
        var firstChildId = new NodeId(Guid.Parse("8e0f2176-4293-4f60-3172-0d1e2f3a4b5c"));
        var secondChildId = new NodeId(Guid.Parse("9f103287-53a4-4071-4283-1e2f3a4b5c6d"));
        var thirdChildId = new NodeId(Guid.Parse("0a214398-64b5-4182-5394-2f3a4b5c6d7e"));
        await InsertNodesAsync(
            database,
            Node(new SnapshotId(0), rootId, null, 0),
            Node(new SnapshotId(0), firstChildId, rootId, 0),
            Node(new SnapshotId(0), secondChildId, rootId, 1),
            Node(new SnapshotId(0), thirdChildId, rootId, 2));

        var transaction = await BeginTransactionAsync(database);
        var repository = new SqlNodeMutationRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var result = await repository.ExecuteAsync(
            transaction.TransactionId,
            state => Result<WorkingNodeMutationDecision<NodeId>>.Success(
                new WorkingNodeMutationDecision<NodeId>(
                    firstChildId,
                    state with
                    {
                        Nodes =
                        [
                            Node(state.SnapshotId, rootId, null, 0),
                            Node(state.SnapshotId, thirdChildId, rootId, 0),
                            Node(state.SnapshotId, firstChildId, rootId, 1),
                            Node(state.SnapshotId, secondChildId, rootId, 2)
                        ]
                    })));

        Assert.True(result.IsSuccess);
        var hierarchyRepository = new SqlHierarchyRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var workingNodes = await hierarchyRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId);
        Assert.Equal(4, workingNodes.Count);
        Assert.Equal(0, workingNodes.Single(node => node.NodeId == thirdChildId).SortOrder);
        Assert.Equal(1, workingNodes.Single(node => node.NodeId == firstChildId).SortOrder);
        Assert.Equal(2, workingNodes.Single(node => node.NodeId == secondChildId).SortOrder);
    }

    private static async Task<KnowledgeTransaction> BeginTransactionAsync(SqlTestDatabase database) =>
        await new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));

    private static Node Node(SnapshotId snapshotId, NodeId nodeId, NodeId? parentNodeId, int sortOrder) =>
        new(snapshotId, nodeId, parentNodeId, "Titel", null, sortOrder, IsDeleted: false);

    private static async Task InsertNodesAsync(SqlTestDatabase database, params Node[] nodes)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        foreach (var node in nodes)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
                SELECT CurrentSnapshotId, @nodeId, @parentNodeId, @title, @description, @sortOrder, 0
                FROM dbo.KnowHowToAI_SystemState WHERE Id = 1;
                """;
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@nodeId", node.NodeId.Value));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter(
                "@parentNodeId", node.ParentNodeId is null ? DBNull.Value : node.ParentNodeId.Value.Value));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@title", node.Title));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter(
                "@description", node.Description is null ? DBNull.Value : node.Description));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@sortOrder", node.SortOrder));
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertCurrentNodeAsync(SqlTestDatabase database, NodeId nodeId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            SELECT CurrentSnapshotId, @nodeId, NULL, N'Original', NULL, 0, 0
            FROM dbo.KnowHowToAI_SystemState WHERE Id = 1;
            """;
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@nodeId", nodeId.Value));
        await command.ExecuteNonQueryAsync();
    }
}
