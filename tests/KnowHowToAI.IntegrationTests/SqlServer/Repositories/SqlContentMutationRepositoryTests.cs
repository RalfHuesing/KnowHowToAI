using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

/// <summary>Belegt die atomare Persistenz eines Zielgruppen-Contents im Working Snapshot.</summary>
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
                                new AudienceId("Default"),
                                revisionId,
                                ContentMode.Independent,
                                "Text",
                                IsDeleted: false)
                        ]
                    })), 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);
        var contentRepository = new SqlContentRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        Assert.Empty(await contentRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        var workingContent = Assert.Single(await contentRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal(revisionId, workingContent.ContentRevisionId);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesAndTombstonesOnlyWorkingContentLeavingBaseSnapshotIntact()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var nodeId = new NodeId(Guid.Parse("1c4e7234-6f93-4a04-9c8b-9e23d4e5f6a7"));
        var originalRevisionId = new ContentRevisionId(Guid.Parse("2d5f8345-7a04-4b15-ad9c-af34e5f6a7b8"));
        var updatedRevisionId = new ContentRevisionId(Guid.Parse("3e6a9456-8b15-4c26-be0d-bf45f6a7b8c9"));
        await InsertCurrentNodeAsync(database, nodeId);
        await InsertCurrentContentAsync(database, nodeId, originalRevisionId);

        var transaction = await new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlContentMutationRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var result = await repository.ExecuteAsync(
            transaction.TransactionId,
            state =>
            {
                var existing = state.Contents.Single(c => c.NodeId == nodeId && c.AudienceId == new AudienceId("Default"));
                var updated = existing with
                {
                    ContentRevisionId = updatedRevisionId,
                    ContentMd = "Aktualisiert",
                    IsDeleted = true
                };
                return Result<WorkingContentMutationDecision<ContentRevisionId>>.Success(
                    new WorkingContentMutationDecision<ContentRevisionId>(
                        updatedRevisionId,
                        state with
                        {
                            Contents = state.Contents.Select(c => c == existing ? updated : c).ToArray()
                        }));
            }, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);

        var contentRepository = new SqlContentRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var baseContent = Assert.Single(await contentRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        Assert.Equal("Original", baseContent.ContentMd);
        Assert.Equal(originalRevisionId, baseContent.ContentRevisionId);
        Assert.False(baseContent.IsDeleted);

        var workingContent = Assert.Single(await contentRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal("Aktualisiert", workingContent.ContentMd);
        Assert.Equal(updatedRevisionId, workingContent.ContentRevisionId);
        Assert.True(workingContent.IsDeleted);
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

    private static async Task InsertCurrentContentAsync(SqlTestDatabase database, NodeId nodeId, ContentRevisionId revisionId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            SELECT CurrentSnapshotId, @nodeId, N'Default', @revisionId, N'Independent', N'Original', 0
            FROM dbo.KnowHowToAI_SystemState WHERE Id = 1;
            """;
        command.Parameters.Add(new SqlParameter("@nodeId", nodeId.Value));
        command.Parameters.Add(new SqlParameter("@revisionId", revisionId.Value));
        await command.ExecuteNonQueryAsync();
    }
}
