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
        var service = new RoleMutationService(
            new TransactionRepositoryFake(),
            new RoleRepositoryFake(),
            new ContentRepositoryFake(),
            new DependencyRepositoryFake(),
            new RoleMutationRepositoryFake());
        var transactionId = new TransactionId(Guid.Parse("3c7a4e9d-17bf-4317-9d78-f2ee938121c9"));

        var result = await service.CreateRoleAsync(transactionId, "Developer", null);

        Assert.False(result.IsSuccess);
        Assert.Equal("TransactionNotFound", result.Code);
        Assert.Equal(transactionId.ToString(), result.Details["transactionId"]);
    }

    private sealed class TransactionRepositoryFake : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(KnowHowToAI.Core.Application.Transactions.BeginTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => Task.FromResult<KnowledgeTransaction?>(null);

        public Task<KnowHowToAI.Core.Application.Transactions.CommitTransactionResult> CommitAsync(KnowHowToAI.Core.Application.Transactions.CommitTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RoleRepositoryFake : IRoleRepository
    {
        public Task<IReadOnlyList<Role>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ContentRepositoryFake : IContentRepository
    {
        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DependencyRepositoryFake : IDependencyRepository
    {
        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RoleMutationRepositoryFake : IRoleMutationRepository
    {
        public Task SaveRolesAsync(SnapshotId snapshotId, IReadOnlyList<Role> roles, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SaveResolutionsAsync(SnapshotId snapshotId, RoleId requestedRoleId, IReadOnlyList<RoleResolution> resolutions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
