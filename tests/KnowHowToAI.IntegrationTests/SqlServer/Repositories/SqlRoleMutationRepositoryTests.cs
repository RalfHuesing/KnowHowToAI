using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
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
        var roleId = new RoleId("Developer");

        var result = await repository.ExecuteAsync(transaction.TransactionId, state =>
        {
            var role = new Role(state.SnapshotId, roleId, "Developer", null, IsDeleted: false);
            return Result<WorkingRoleMutationDecision<RoleId>>.Success(
                new WorkingRoleMutationDecision<RoleId>(roleId, state with { Roles = [..state.Roles, role] }));
        });

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
        var defaultRole = new RoleId("Default");

        var result = await repository.ExecuteAsync(transaction.TransactionId, state =>
        {
            var existing = state.Roles.Single(r => r.RoleId == defaultRole);
            var updatedRole = existing with { Description = "Aktualisiert" };
            var newResolution = new RoleResolution(state.SnapshotId, defaultRole, defaultRole, 1);
            return Result<WorkingRoleMutationDecision<RoleId>>.Success(
                new WorkingRoleMutationDecision<RoleId>(
                    defaultRole,
                    state with
                    {
                        Roles = state.Roles.Select(r => r.RoleId == defaultRole ? updatedRole : r).ToArray(),
                        Resolutions = [newResolution]
                    }));
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);

        var roleRepository = new SqlRoleRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var baseRole = Assert.Single(await roleRepository.ListBySnapshotAsync(transaction.BaseSnapshotId));
        Assert.Null(baseRole.Description);

        var workingRole = Assert.Single(await roleRepository.ListBySnapshotAsync(transaction.WorkingSnapshotId));
        Assert.Equal("Aktualisiert", workingRole.Description);

        var workingResolutions = await roleRepository.ListResolutionsBySnapshotAsync(transaction.WorkingSnapshotId);
        Assert.Single(workingResolutions);
        Assert.Equal(defaultRole, workingResolutions[0].CandidateRoleId);
    }
}
