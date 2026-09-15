using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Roles;

[Trait("Category", "Unit")]
public sealed class RoleMutationServiceTests
{
    [Fact]
    public async Task CreateRoleAsync_MissingTransaction_ReturnsStableErrorWithoutWriting()
    {
        var service = new RoleMutationService(new RoleMutationRepositoryFake());
        var transactionId = new TransactionId(Guid.Parse("3c7a4e9d-17bf-4317-9d78-f2ee938121c9"));

        var result = await service.CreateRoleAsync(transactionId, "Developer", null);

        Assert.False(result.IsSuccess);
        Assert.Equal("TransactionNotFound", result.Code);
        Assert.Equal(transactionId.ToString(), result.Details["transactionId"]);
    }

    private sealed class RoleMutationRepositoryFake : IRoleMutationRepository
    {
        public Task<Result<WorkingRoleMutationExecution<T>>> ExecuteAsync<T>(TransactionId transactionId, Func<WorkingRoleMutationState, Result<WorkingRoleMutationDecision<T>>> mutate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Failure(new DomainError(
                "TransactionNotFound", "Die Transaction existiert nicht.", new Dictionary<string, string> { ["transactionId"] = transactionId.ToString() })));
    }
}
