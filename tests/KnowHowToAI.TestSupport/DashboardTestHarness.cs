using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Dashboard;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Gemeinsamer Test-Harness für Dashboard-Tests (Core- und Web-Tests).
/// </summary>
public sealed class DashboardTestHarness
{
    public InMemoryKnowledgeStore Store { get; } = new();
    private readonly InMemorySnapshotRepository _snapshotRepo;
    private readonly InMemoryDashboardRepository _dashboardRepo = new();
    private readonly InMemoryWorkingSnapshotValidationDataRepository _validationDataRepo = new();
    private readonly InMemoryHierarchyRepository _hierarchyRepo;
    private readonly InMemoryContentRepository _contentRepo;
    private readonly InMemoryAudienceRepository _audienceRepo;
    private readonly InMemoryDependencyRepository _dependencyRepo;
    private readonly InMemoryTransactionRepository _txRepo;

    public DashboardTestHarness(SnapshotId? initialSnapshotId = null, DateTimeOffset? now = null)
    {
        var snapshotId = initialSnapshotId ?? new SnapshotId(1);
        var timestamp = now ?? DateTimeOffset.UtcNow;

        Store.CurrentSnapshotId = snapshotId;
        Store.Snapshots.Add(new Snapshot(snapshotId, null, SnapshotState.Committed, timestamp, timestamp));

        _snapshotRepo = new InMemorySnapshotRepository(Store);
        _hierarchyRepo = new InMemoryHierarchyRepository(Store);
        _contentRepo = new InMemoryContentRepository(Store);
        _audienceRepo = new InMemoryAudienceRepository(Store);
        _dependencyRepo = new InMemoryDependencyRepository(Store);
        _txRepo = new InMemoryTransactionRepository(Store);
    }

    public void SetCurrentSnapshot(Snapshot snapshot)
    {
        Store.CurrentSnapshotId = snapshot.SnapshotId;
        Store.Snapshots.RemoveAll(s => s.SnapshotId == snapshot.SnapshotId);
        Store.Snapshots.Add(snapshot);
    }

    public void SetLatestRelease(Release release) => _dashboardRepo.SetLatestRelease(release);

    public void AddOpenTransaction(KnowledgeTransaction tx) => _dashboardRepo.AddOpenTransaction(tx);

    public void SetOpenTransactionsFailure(Exception? exception) => _dashboardRepo.SetListOpenFailure(exception);

    public void SetTransactionValidationData(TransactionId txId, WorkingSnapshotValidationData data) =>
        _validationDataRepo.SetValidationData(txId, data);

    public void SetSnapshotNodes(SnapshotId snapshotId, IReadOnlyList<Node> nodes)
    {
        Store.Nodes.RemoveAll(n => n.SnapshotId == snapshotId);
        Store.Nodes.AddRange(nodes);
    }

    public void SetSnapshotAudiences(SnapshotId snapshotId, IReadOnlyList<Audience> audiences)
    {
        Store.Audiences.RemoveAll(r => r.SnapshotId == snapshotId);
        Store.Audiences.AddRange(audiences);
    }

    public void SetSnapshotContents(SnapshotId snapshotId, IReadOnlyList<NodeContent> contents)
    {
        Store.Contents.RemoveAll(c => c.SnapshotId == snapshotId);
        Store.Contents.AddRange(contents);
    }

    public void SetSnapshotDependencies(SnapshotId snapshotId, IReadOnlyList<ContentDependency> dependencies)
    {
        Store.Dependencies.RemoveAll(dependency => dependency.SnapshotId == snapshotId);
        Store.Dependencies.AddRange(dependencies);
    }

    public DashboardService CreateService(ValidationPolicy? validationPolicy = null)
    {
        var repos = new SnapshotReadRepositories(
            _snapshotRepo,
            _txRepo,
            _hierarchyRepo,
            _contentRepo,
            _audienceRepo,
            _dependencyRepo);

        return new DashboardService(
            _snapshotRepo,
            _dashboardRepo,
            _validationDataRepo,
            repos,
            validationPolicy ?? new ValidationPolicy
            {
                ContentSizeWarningBytes = 100_000,
                ChildCountWarning = 100,
                HierarchyDepthWarning = 10,
                PossibleEmbeddedHeadingWarning = true
            });
    }
}
