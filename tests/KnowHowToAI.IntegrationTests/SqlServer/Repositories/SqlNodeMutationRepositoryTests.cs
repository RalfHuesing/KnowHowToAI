using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
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
