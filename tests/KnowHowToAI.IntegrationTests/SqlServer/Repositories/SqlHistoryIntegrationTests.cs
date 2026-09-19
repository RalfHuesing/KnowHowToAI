using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlHistoryIntegrationTests
{
    [Fact]
    public async Task CompareSnapshotsAndTransactionChanges_RealDatabase_ComputesDeterministicDiffs()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 30 };
        var snapshotRepo = new SqlSnapshotRepository(database.ConnectionFactory, policy);
        var transactionRepo = new SqlTransactionRepository(database.ConnectionFactory, policy);
        var hierarchyRepo = new SqlHierarchyRepository(database.ConnectionFactory, policy);
        var contentRepo = new SqlContentRepository(database.ConnectionFactory, policy);
        var roleRepo = new SqlRoleRepository(database.ConnectionFactory, policy);
        var depRepo = new SqlDependencyRepository(database.ConnectionFactory, policy);

        var historyRepos = new SnapshotReadRepositories(
            snapshotRepo,
            transactionRepo,
            hierarchyRepo,
            contentRepo,
            roleRepo,
            depRepo);

        var historyService = new HistoryService(historyRepos, new RetrievalPolicy
        {
            DefaultPageSize = 10,
            MaximumPageSize = 50,
            SearchPageSize = 10,
            SearchMaximumPageSize = 50,
            SnippetMaximumCharacters = 100
        });

        // 1. Get initial committed snapshot
        var initialSnapshot = await snapshotRepo.GetCurrentAsync();
        var snapshotResult = await historyService.GetSnapshotAsync(initialSnapshot.SnapshotId);
        Assert.True(snapshotResult.IsSuccess);
        Assert.Equal(initialSnapshot.SnapshotId, snapshotResult.Value!.SnapshotId);
        Assert.Null(snapshotResult.Value.CommitMetadata);

        // 2. Begin Transaction
        var tx = await transactionRepo.BeginAsync(
            new BeginTransactionRequest(
                new TransactionId(Guid.NewGuid()),
                "History purpose",
                "History actor",
                "History client"));

        // 3. Add a Node in the working snapshot
        var newNodeId = new NodeId(Guid.Parse("90000000-0000-0000-0000-000000000001"));
        await InsertNodeAsync(database, tx.WorkingSnapshotId, newNodeId, "History New Node");

        // 4. GetTransactionChangesAsync on Open Transaction
        var txChangesResult = await historyService.GetTransactionChangesAsync(tx.TransactionId);
        Assert.True(txChangesResult.IsSuccess);
        var txDiff = txChangesResult.Value!.Changes;
        var addedNode = Assert.Single(txDiff.Nodes);
        Assert.Equal(DiffChangeKind.Added, addedNode.Kind);
        Assert.Equal(newNodeId, addedNode.After!.NodeId);

        // 5. Commit Transaction
        var commitResult = await transactionRepo.CommitAsync(
            new CommitTransactionRequest(tx.TransactionId, "Commit node for history test", new KnowHowToAI.Core.Domain.Validation.QualityWarningThresholds(4096, 50, 10), false));

        // 6. CompareSnapshotsAsync between base and committed snapshot
        Assert.True(commitResult.IsCommitted);
        var compareResult = await historyService.CompareSnapshotsAsync(tx.BaseSnapshotId, commitResult.Transaction!.WorkingSnapshotId);
        Assert.True(compareResult.IsSuccess);
        var compareDiff = compareResult.Value!;
        var committedAddedNode = Assert.Single(compareDiff.Nodes);
        Assert.Equal(DiffChangeKind.Added, committedAddedNode.Kind);
        Assert.Equal(newNodeId, committedAddedNode.After!.NodeId);

        // 7. Die Web-Historie sieht ausschließlich committed Stände und paginiert per Keyset.
        var historyPage = await historyService.ListCommittedSnapshotsAsync(limit: 1, cursor: null);
        Assert.True(historyPage.IsSuccess);
        var committedSnapshot = Assert.Single(historyPage.Value!.Items);
        Assert.Equal(commitResult.Transaction.WorkingSnapshotId, committedSnapshot.SnapshotId);
        Assert.Equal(tx.TransactionId, committedSnapshot.CommitMetadata!.TransactionId);
        Assert.Equal("History actor", committedSnapshot.CommitMetadata.Actor);
        Assert.Equal("History client", committedSnapshot.CommitMetadata.Client);
        Assert.Equal("History purpose", committedSnapshot.CommitMetadata.Purpose);
        Assert.Equal("Commit node for history test", committedSnapshot.CommitMetadata.CommitMessage);
        Assert.NotNull(historyPage.Value.NextCursor);

        var previousPage = await historyService.ListCommittedSnapshotsAsync(limit: 1, historyPage.Value.NextCursor);
        Assert.True(previousPage.IsSuccess);
        Assert.Equal(initialSnapshot.SnapshotId, Assert.Single(previousPage.Value!.Items).SnapshotId);
    }

    private static async Task InsertNodeAsync(SqlTestDatabase database, SnapshotId snapshotId, NodeId nodeId, string title)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        const string sql = "INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted) VALUES (@snapId, @id, NULL, @title, NULL, 0, 0);";
        await Dapper.SqlMapper.ExecuteAsync(connection, sql, new { snapId = snapshotId.Value, id = nodeId.Value, title });
    }
}
