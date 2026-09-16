using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

internal sealed class NavigationTestHarness
{
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private SnapshotId _currentSnapshotId;
    private readonly List<Snapshot> _snapshots = [];
    private readonly Dictionary<TransactionId, KnowledgeTransaction> _transactions = new();
    private readonly List<Node> _nodes = [];
    private readonly List<Role> _roles = [];
    private readonly List<RoleResolution> _resolutions = [];
    private readonly List<NodeContent> _contents = [];
    private readonly List<ContentDependency> _dependencies = [];

    private static readonly RoleId DefaultRoleId = new("Developer");

    public NavigationTestHarness(SnapshotId currentSnapshotId)
    {
        _currentSnapshotId = currentSnapshotId;
        _snapshots.Add(new Snapshot(currentSnapshotId, null, SnapshotState.Committed, FixedTimestamp, FixedTimestamp));
        EnsureDefaultRole(currentSnapshotId);
    }

    public void SetCurrentSnapshot(SnapshotId snapshotId)
    {
        _currentSnapshotId = snapshotId;
        _snapshots.Add(new Snapshot(snapshotId, null, SnapshotState.Committed, FixedTimestamp, FixedTimestamp));
        EnsureDefaultRole(snapshotId);
    }

    public void AddHistoricalSnapshot(Snapshot snapshot)
    {
        _snapshots.Add(snapshot);
        EnsureDefaultRole(snapshot.SnapshotId);
    }

    public void SetTransaction(KnowledgeTransaction transaction)
    {
        _transactions[transaction.TransactionId] = transaction;
        EnsureDefaultRole(transaction.WorkingSnapshotId);
    }

    public void AddNode(Node node) => _nodes.Add(node);

    public void AddRole(Role role)
    {
        _roles.RemoveAll(r => r.SnapshotId == role.SnapshotId && r.RoleId == role.RoleId);
        _roles.Add(role);
    }

    public void AddRoleResolution(RoleResolution resolution)
    {
        _resolutions.RemoveAll(r => r.SnapshotId == resolution.SnapshotId
            && r.RequestedRoleId == resolution.RequestedRoleId
            && r.CandidateRoleId == resolution.CandidateRoleId);
        _resolutions.Add(resolution);
    }

    public void ClearRoles(SnapshotId snapshotId)
    {
        _roles.RemoveAll(r => r.SnapshotId == snapshotId);
        _resolutions.RemoveAll(r => r.SnapshotId == snapshotId);
    }

    private void EnsureDefaultRole(SnapshotId snapshotId)
    {
        if (!_roles.Any(r => r.SnapshotId == snapshotId && r.RoleId == DefaultRoleId))
        {
            _roles.Add(new Role(snapshotId, DefaultRoleId, "Developer", null, false));
        }

        if (!_resolutions.Any(r => r.SnapshotId == snapshotId && r.RequestedRoleId == DefaultRoleId && r.CandidateRoleId == DefaultRoleId))
        {
            _resolutions.Add(new RoleResolution(snapshotId, DefaultRoleId, DefaultRoleId, 1));
        }
    }

    public void AddContent(NodeContent content) => _contents.Add(content);
    public void AddDependency(ContentDependency dependency) => _dependencies.Add(dependency);

    public NavigationService CreateService() => new(
        new NavigationRepositories(
            new SnapshotRepoFake(_snapshots, () => _currentSnapshotId),
            new TransactionRepoFake(_transactions),
            new HierarchyRepoFake(_nodes),
            new RoleRepoFake(_roles, _resolutions),
            new ContentRepoFake(_contents),
            new DependencyRepoFake(_dependencies)),
        new RetrievalPolicy
        {
            DefaultPageSize = 10,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 100,
            SnippetMaximumCharacters = 100
        });

    private sealed class SnapshotRepoFake(List<Snapshot> snapshots, Func<SnapshotId> currentSnapshotIdProvider)
        : ISnapshotRepository
    {
        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshots.FirstOrDefault(snapshot => snapshot.SnapshotId == snapshotId));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshots.First(snapshot => snapshot.SnapshotId == currentSnapshotIdProvider()));
    }

    private sealed class TransactionRepoFake(Dictionary<TransactionId, KnowledgeTransaction> transactions)
        : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(transactions.GetValueOrDefault(transactionId));

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class HierarchyRepoFake(List<Node> nodes) : IHierarchyRepository
    {
        public Task<IReadOnlyList<Node>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Node>>(nodes.Where(node => node.SnapshotId == snapshotId).ToArray());
    }

    private sealed class RoleRepoFake(List<Role> roles, List<RoleResolution> resolutions) : IRoleRepository
    {
        public Task<IReadOnlyList<Role>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>(roles.Where(role => role.SnapshotId == snapshotId).ToArray());

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleResolution>>(resolutions.Where(resolution => resolution.SnapshotId == snapshotId).ToArray());
    }

    private sealed class ContentRepoFake(List<NodeContent> contents) : IContentRepository
    {
        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NodeContent>>(contents.Where(content => content.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DependencyRepoFake(List<ContentDependency> dependencies) : IDependencyRepository
    {
        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContentDependency>>(dependencies.Where(dependency => dependency.SnapshotId == snapshotId).ToArray());
    }
}
