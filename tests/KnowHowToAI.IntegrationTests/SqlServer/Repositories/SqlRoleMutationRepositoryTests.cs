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
public sealed class SqlRoleMutationRepositoryTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesOnlyWorkingRoleAndIncrementsChangeVersion()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlRoleMutationRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var roleId = new AudienceId("Developer");

        var result = await repository.ExecuteAsync(transaction.TransactionId, state =>
        {
            var role = new Audience(state.SnapshotId, roleId, "Developer", null, IsDeleted: false);
            return Result<WorkingAudienceMutationDecision<AudienceId>>.Success(
                new WorkingAudienceMutationDecision<AudienceId>(roleId, state with { Audiences = [..state.Audiences, role] }));
        }, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);
        var roleRepository = new SqlRoleRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        Assert.Single(await roleRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        Assert.Equal(2, (await roleRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId)).Count);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesRoleAndSetsResolutionOnlyInWorkingSnapshotLeavingBaseSnapshotIntact()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var transaction = await new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 })
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var repository = new SqlRoleMutationRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var defaultRole = new AudienceId("Default");

        var result = await repository.ExecuteAsync(transaction.TransactionId, state =>
        {
            var existing = state.Audiences.Single(r => r.AudienceId == defaultRole);
            var updatedRole = existing with { Description = "Aktualisiert" };
            var newResolution = new AudienceResolution(state.SnapshotId, defaultRole, defaultRole, 1);
            return Result<WorkingAudienceMutationDecision<AudienceId>>.Success(
                new WorkingAudienceMutationDecision<AudienceId>(
                    defaultRole,
                    state with
                    {
                        Audiences = state.Audiences.Select(r => r.AudienceId == defaultRole ? updatedRole : r).ToArray(),
                        Resolutions = [newResolution]
                    }));
        }, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);

        var roleRepository = new SqlRoleRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var baseRole = Assert.Single(await roleRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        Assert.Equal("Allgemeine, rollenunabhängige Standardinhalte", baseRole.Description);

        var workingRole = Assert.Single(await roleRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal("Aktualisiert", workingRole.Description);

        var workingResolutions = await roleRepository.ListResolutionsBySnapshotAsync(transaction.WorkingSnapshotId);
        Assert.Single(workingResolutions);
        Assert.Equal(defaultRole, workingResolutions[0].CandidateAudienceId);
    }
}
