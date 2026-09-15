using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

/// <summary>Belegt die atomare Persistenz eines Rollen-Contents im Working Snapshot.</summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlContentMutationRepositoryTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesOnlyWorkingContentAndIncrementsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var nodeId = new NodeId(Guid.Parse("0b4ddf2b-7e36-4fe8-9295-7d0956d2aa0f"));
        await InsertCurrentNodeAsync(database, nodeId);
        var transaction = await new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlContentMutationRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var revisionId = new ContentRevisionId(Guid.Parse("3d5a9aee-72c0-43f8-8109-967e60bfa8bb"));

        var result = await repository.ExecuteAsync(
            transaction.TransactionId,
            state => Result<WorkingContentMutationDecision<ContentRevisionId>>.Success(
                new WorkingContentMutationDecision<ContentRevisionId>(
                    revisionId,
                    state with
                    {
                        Contents =
                        [
                            ..state.Contents,
                            new NodeContent(
                                state.SnapshotId,
                                nodeId,
                                new RoleId("Default"),
                                revisionId,
                                ContentMode.Independent,
                                "Text",
                                IsDeleted: false)
                        ]
                    })));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);
        var contentRepository = new SqlContentRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        Assert.Empty(await contentRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        var workingContent = Assert.Single(await contentRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal(revisionId, workingContent.ContentRevisionId);
    }

    private static async Task InsertCurrentNodeAsync(SqlTestDatabase database, NodeId nodeId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            SELECT CurrentSnapshotId, @nodeId, NULL, N'Root', NULL, 0, 0
            FROM dbo.KnowHowToAI_SystemState WHERE Id = 1;
            """;
        command.Parameters.Add(new SqlParameter("@nodeId", nodeId.Value));
        await command.ExecuteNonQueryAsync();
    }
}
