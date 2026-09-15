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
}
