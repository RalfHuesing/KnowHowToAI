using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NavigationServiceTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly NodeId RootNodeId = new(Guid.Parse("b764fc68-d485-4bca-8617-b33a51d838ae"));
    private static readonly NodeId ChildNodeId = new(Guid.Parse("9bcf48dd-8a8e-4af2-a7a7-baa40c9148cb"));
    private static readonly RoleId RoleId = new("Developer");

    [Fact]
    public async Task ListChildrenAsync_CursorOutsideResultSet_ReturnsInvalidCursorInsteadOfSkippingItems()
    {
        var service = CreateService();
        var cursorFromAnotherResultSet = Guid.Parse("22b4219d-b8f9-46e6-b246-59ba0f43a2ba").ToString("D");

        var result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleId,
            Cursor: cursorFromAnotherResultSet));

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, result.Code);
        Assert.Equal(cursorFromAnotherResultSet, result.Details[NavigationErrorCodes.CursorDetail]);
    }

    private static NavigationService CreateService() => new(
        new NavigationRepositories(
            new SnapshotRepositoryFake(),
            new TransactionRepositoryFake(),
            new HierarchyRepositoryFake(),
            new RoleRepositoryFake(),
            new ContentRepositoryFake(),
            new DependencyRepositoryFake()),
        new RetrievalPolicy
        {
            DefaultPageSize = 10,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 100,
            SnippetMaximumCharacters = 100
        });

    private sealed class SnapshotRepositoryFake : ISnapshotRepository
    {
        private static readonly Snapshot Current = new(CurrentSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Snapshot?>(snapshotId == CurrentSnapshotId ? Current : null);

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(Current);
    }

    private sealed class TransactionRepositoryFake : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(KnowHowToAI.Core.Application.Transactions.BeginTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => Task.FromResult<KnowledgeTransaction?>(null);

        public Task<KnowHowToAI.Core.Application.Transactions.CommitTransactionResult> CommitAsync(KnowHowToAI.Core.Application.Transactions.CommitTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class HierarchyRepositoryFake : IHierarchyRepository
    {
        public Task<IReadOnlyList<Node>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Node>>(
            [
                new Node(snapshotId, RootNodeId, null, "Root", null, 0, false),
                new Node(snapshotId, ChildNodeId, RootNodeId, "Kind", null, 0, false)
            ]);
    }

    private sealed class RoleRepositoryFake : IRoleRepository
    {
        public Task<IReadOnlyList<Role>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>([new Role(snapshotId, RoleId, "Developer", null, false)]);

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleResolution>>([new RoleResolution(snapshotId, RoleId, RoleId, 1)]);
    }

    private sealed class ContentRepositoryFake : IContentRepository
    {
        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NodeContent>>([]);
    }

    private sealed class DependencyRepositoryFake : IDependencyRepository
    {
        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContentDependency>>([]);
    }
}
