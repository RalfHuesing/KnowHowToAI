using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlAudienceMutationRepositoryTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesOnlyWorkingAudienceAndIncrementsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlAudienceMutationRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var audienceId = new AudienceId("Developer");

        var result = await repository.ExecuteAsync(transaction.TransactionId, state =>
        {
            var audience = new Audience(state.SnapshotId, audienceId, "Developer", null, IsDeleted: false);
            return Result<WorkingAudienceMutationDecision<AudienceId>>.Success(
                new WorkingAudienceMutationDecision<AudienceId>(audienceId, state with { Audiences = [..state.Audiences, audience] }));
        }, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);
        var audienceRepository = new SqlAudienceRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        Assert.Single(await audienceRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        Assert.Equal(2, (await audienceRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId)).Count);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesAudienceAndSetsResolutionOnlyInWorkingSnapshotLeavingBaseSnapshotIntact()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlAudienceMutationRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var defaultAudience = new AudienceId("Default");

        var result = await repository.ExecuteAsync(transaction.TransactionId, state =>
        {
            var existing = state.Audiences.Single(r => r.AudienceId == defaultAudience);
            var updatedAudience = existing with { Description = "Aktualisiert" };
            var newResolution = new AudienceResolution(state.SnapshotId, defaultAudience, defaultAudience, 1);
            return Result<WorkingAudienceMutationDecision<AudienceId>>.Success(
                new WorkingAudienceMutationDecision<AudienceId>(
                    defaultAudience,
                    state with
                    {
                        Audiences = state.Audiences.Select(r => r.AudienceId == defaultAudience ? updatedAudience : r).ToArray(),
                        Resolutions = [newResolution]
                    }));
        }, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);

        var audienceRepository = new SqlAudienceRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var baseAudience = Assert.Single(await audienceRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        Assert.Equal("Allgemeine, zielgruppenunabhängige Standardinhalte", baseAudience.Description);

        var workingAudience = Assert.Single(await audienceRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal("Aktualisiert", workingAudience.Description);

        var workingResolutions = await audienceRepository.ListResolutionsBySnapshotAsync(transaction.WorkingSnapshotId);
        Assert.Single(workingResolutions);
        Assert.Equal(defaultAudience, workingResolutions[0].CandidateAudienceId);
    }
}
