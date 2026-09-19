using KnowHowToAI.Core.Application.Dashboard;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Dashboard;

[Trait("Category", "Unit")]
public sealed class DashboardServiceTests
{
    private static readonly SnapshotId InitialSnapshotId = new(1);
    private static readonly SnapshotId CommittedBaseSnapshotId = new(2);
    private static readonly SnapshotId CurrentSnapshotId = new(3);
    private static readonly RoleId DeveloperRoleId = new("Developer");

    [Fact]
    public async Task GetDashboardAsync_ReturnsCurrentSnapshotAndNullRelease_WhenNoReleaseExists()
    {
        var harness = new DashboardTestHarness();
        harness.SetCurrentSnapshot(new Snapshot(InitialSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var service = harness.CreateService();
        var result = await service.GetDashboardAsync(new DashboardQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(InitialSnapshotId, result.Value.CurrentSnapshot.SnapshotId);
        Assert.Null(result.Value.LatestRelease);
        Assert.Empty(result.Value.OpenTransactions);
        Assert.Empty(result.Value.RecentNodeChanges);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsLatestRelease_WhenReleaseExists()
    {
        var harness = new DashboardTestHarness();
        var release = new Release(new ReleaseId(10), CurrentSnapshotId, "v1.0.0", "Erster Release", DateTimeOffset.UtcNow);
        harness.SetLatestRelease(release);

        var service = harness.CreateService();
        var result = await service.GetDashboardAsync(new DashboardQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.LatestRelease);
        Assert.Equal("v1.0.0", result.Value.LatestRelease.Name);
    }

    [Fact]
    public async Task GetDashboardAsync_ValidatesOpenTransactions_AndIncludesHardErrors()
    {
        var harness = new DashboardTestHarness();
        var txId = new TransactionId(Guid.NewGuid());
        var tx = new KnowledgeTransaction(
            txId,
            CommittedBaseSnapshotId,
            new SnapshotId(100),
            TransactionState.Open,
            ChangeVersion: 1,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
            CommittedAtUtc: null,
            Purpose: "Test Transaction",
            Actor: "Alice",
            Client: "WebUI",
            CommitMessage: null);

        harness.AddOpenTransaction(tx);

        // Set invalid data for this open transaction (e.g. node without title)
        var invalidNode = new Node(new SnapshotId(100), new NodeId(Guid.NewGuid()), null, "", null, 1, false);
        harness.SetTransactionValidationData(txId, new WorkingSnapshotValidationData(
            Nodes: [invalidNode],
            Roles: [],
            RoleResolutions: [],
            Contents: [],
            Dependencies: []));

        var service = harness.CreateService();
        var result = await service.GetDashboardAsync(new DashboardQuery());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.OpenTransactions);

        var txSummary = result.Value.OpenTransactions[0];
        Assert.Equal(txId, txSummary.Transaction.TransactionId);
        Assert.NotEmpty(txSummary.ValidationErrors);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenCurrentSnapshotHasNoPredecessor_RecentChangesIsEmpty()
    {
        var harness = new DashboardTestHarness();
        harness.SetCurrentSnapshot(new Snapshot(InitialSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var service = harness.CreateService();
        var result = await service.GetDashboardAsync(new DashboardQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.RecentNodeChanges);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenCurrentSnapshotHasPredecessor_ComputesRecentNodeChanges()
    {
        var harness = new DashboardTestHarness();
        harness.SetCurrentSnapshot(new Snapshot(CurrentSnapshotId, CommittedBaseSnapshotId, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var nodeAId = new NodeId(Guid.NewGuid());
        var nodeBId = new NodeId(Guid.NewGuid());
        var nodeCId = new NodeId(Guid.NewGuid());

        // Base snapshot has Node A and Node C
        var baseNodeA = new Node(CommittedBaseSnapshotId, nodeAId, null, "Node A Original", null, 1, false);
        var baseNodeC = new Node(CommittedBaseSnapshotId, nodeCId, null, "Node C ToDelete", null, 2, false);
        harness.SetSnapshotNodes(CommittedBaseSnapshotId, [baseNodeA, baseNodeC]);

        // Current snapshot has Node A (modified title), Node B (added), and Node C is omitted (deleted)
        var currentNodeA = new Node(CurrentSnapshotId, nodeAId, null, "Node A Geändert", null, 1, false);
        var currentNodeB = new Node(CurrentSnapshotId, nodeBId, null, "Node B Neu", null, 2, false);
        harness.SetSnapshotNodes(CurrentSnapshotId, [currentNodeA, currentNodeB]);

        var service = harness.CreateService();
        var result = await service.GetDashboardAsync(new DashboardQuery());

        Assert.True(result.IsSuccess);
        var changes = result.Value!.RecentNodeChanges;
        Assert.Equal(3, changes.Count);

        var changeA = Assert.Single(changes, c => c.NodeId == nodeAId);
        Assert.Equal(DiffChangeKind.Modified, changeA.Kind);
        Assert.Equal("Node A Geändert", changeA.Title);

        var changeB = Assert.Single(changes, c => c.NodeId == nodeBId);
        Assert.Equal(DiffChangeKind.Added, changeB.Kind);
        Assert.Equal("Node B Neu", changeB.Title);

        var changeC = Assert.Single(changes, c => c.NodeId == nodeCId);
        Assert.Equal(DiffChangeKind.Deleted, changeC.Kind);
        Assert.Equal("Node C ToDelete", changeC.Title);
    }

    [Fact]
    public async Task GetDashboardAsync_EvaluatesCurrentQuality_StaleContentsAndWarnings()
    {
        var harness = new DashboardTestHarness();
        harness.SetCurrentSnapshot(new Snapshot(CurrentSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var node = new Node(CurrentSnapshotId, new NodeId(Guid.NewGuid()), null, "Test Node", null, 1, false);
        var role = new Role(CurrentSnapshotId, DeveloperRoleId, "Developer", null, false);

        // Add a node with large content to trigger a warning
        var largeContent = new string('x', 100_000);
        var content = new NodeContent(
            CurrentSnapshotId,
            node.NodeId,
            DeveloperRoleId,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            largeContent,
            false);

        harness.SetSnapshotNodes(CurrentSnapshotId, [node]);
        harness.SetSnapshotRoles(CurrentSnapshotId, [role]);
        harness.SetSnapshotContents(CurrentSnapshotId, [content]);

        var service = harness.CreateService(new ValidationPolicy
        {
            ContentSizeWarningBytes = 50_000,
            ChildCountWarning = 10,
            HierarchyDepthWarning = 5,
            PossibleEmbeddedHeadingWarning = true
        });

        var result = await service.GetDashboardAsync(new DashboardQuery());

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.QualitySummary.Warnings);
    }
}
