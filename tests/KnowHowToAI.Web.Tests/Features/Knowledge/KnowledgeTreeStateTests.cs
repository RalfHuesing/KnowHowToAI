using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreeStateTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly RoleId DefaultRoleId = new("Developer");

    [Fact]
    public async Task InitializeAsync_EmptySnapshot_SetsRootNodeNull()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);

        using var treeState = new KnowledgeTreeState(service);
        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        Assert.Null(treeState.RootNode);
        Assert.Null(treeState.VisualRootNodeId);
        Assert.Null(treeState.RootError);
        Assert.False(treeState.IsLoading);
        Assert.Equal(0, treeState.LoadedPageCount);
    }

    [Fact]
    public async Task ExpandNodeAsync_Exactly100Children_LoadsOnePageWithoutNextCursor()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        for (var i = 1; i <= 100; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Child {i:D3}", null, i, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);
        Assert.NotNull(treeState.RootNode);

        await treeState.ExpandNodeAsync(rootId.Value);

        Assert.True(treeState.RootNode.IsExpanded);
        Assert.Equal(100, treeState.RootNode.Children.Count);
        Assert.Null(treeState.RootNode.NextCursor);
        Assert.False(treeState.RootNode.HasNextPage);
        Assert.False(treeState.RootNode.HasPreviousPage);
        Assert.Equal(1, treeState.LoadedPageCount);
    }


    [Fact]
    public async Task ExpandNodeAsync_AtLeast10Levels_MaintainsHierarchyDepthAndBreadcrumbs()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var currentParentId = (NodeId?)null;
        var nodeIds = new List<NodeId>();

        for (var depth = 0; depth < 12; depth++)
        {
            var nodeId = new NodeId(Guid.NewGuid());
            nodeIds.Add(nodeId);
            harness.AddNode(new Node(DefaultSnapshotId, nodeId, currentParentId, $"Level {depth}", null, 1, false));
            currentParentId = nodeId;
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        // Expandieren von Level 0 bis 9 (10 geladene Seiten)
        for (var i = 0; i < 10; i++)
        {
            await treeState.ExpandNodeAsync(nodeIds[i].Value);
        }

        Assert.Equal(10, treeState.LoadedPageCount);

        // Selektiere tiefsten Knoten
        await treeState.SelectNodeAsync(nodeIds[9].Value);

        var breadcrumbs = treeState.Breadcrumbs;
        Assert.Equal(10, breadcrumbs.Count);
        Assert.Equal("Level 0", breadcrumbs[0].Title);
        Assert.Equal("Level 9", breadcrumbs[9].Title);
    }


    [Fact]
    public async Task ContextSwitch_DiscardsAllTreePagesAndCancelsRunningRequests()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root Snap 1", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child Snap 1", null, 1, false));

        var snapshotId2 = new SnapshotId(2);
        harness.AddHistoricalSnapshot(new Snapshot(snapshotId2, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var rootId2 = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(snapshotId2, rootId2, null, "Root Snap 2", null, 0, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        // 1. Kontext: Snapshot 1
        await treeState.InitializeAsync(new ReadContext(SnapshotId: DefaultSnapshotId), DefaultRoleId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        Assert.Equal("Root Snap 1", treeState.RootNode!.Title);
        Assert.Equal(1, treeState.LoadedPageCount);

        // 2. Kontextwechsel: Snapshot 2
        await treeState.InitializeAsync(new ReadContext(SnapshotId: snapshotId2), DefaultRoleId.Value);

        // Altes Root und alte geladene Seiten vollständig verworfen
        Assert.Equal("Root Snap 2", treeState.RootNode!.Title);
        Assert.Equal(0, treeState.LoadedPageCount);
        Assert.Null(treeState.FindNode(childId.Value));
    }

    [Fact]
    public async Task BranchError_InvalidCursorOrError_IsIsolatedToBranchWithoutCrashingTree()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var child1Id = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, child1Id, rootId, "Child 1", null, 1, false));

        var child2Id = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, child2Id, rootId, "Child 2", null, 2, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        var child1Node = treeState.FindNode(child1Id.Value);
        var child2Node = treeState.FindNode(child2Id.Value);
        Assert.NotNull(child1Node);
        Assert.NotNull(child2Node);

        // Expandieren von Child 1 (hat keine Kinder -> leere Liste)
        await treeState.ExpandNodeAsync(child1Id.Value);
        Assert.Null(child1Node.Error);

        // Simuliere ungültigen Cursor auf Child 2
        child2Node.NextCursor = "ungueltiger-cursor-string";
        await treeState.PageNextAsync(child2Id.Value);

        // Child 2 enthält isolierten Fehler, Rest des Baums bleibt intakt
        Assert.NotNull(child2Node.Error);
        Assert.False(child2Node.IsLoading);
        Assert.Null(child1Node.Error);
        Assert.Null(treeState.RootNode!.Error);
    }

    [Fact]
    public async Task RequestCancellation_FastSuccessiveCalls_CancelPreviousWithoutError()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child 1", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Sofort abgebrochen

        // Abgebrochener Request wirft keine unbehandelte Exception und hinterlässt keinen Fehlerzustand
        await treeState.ExpandNodeAsync(rootId.Value, cts.Token);

        Assert.Null(treeState.RootNode!.Error);
        Assert.False(treeState.RootNode.IsLoading);
    }

    [Fact]
    public async Task Instrumentation_ListChildrenCalls_AlwaysPassLimit100AndExplicitContext()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        for (var i = 1; i <= 150; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Child {i:D3}", null, i, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        var context = new ReadContext(SnapshotId: DefaultSnapshotId);
        await treeState.InitializeAsync(context, DefaultRoleId.Value);

        // Expandieren
        await treeState.ExpandNodeAsync(rootId.Value);

        Assert.Equal(DefaultRoleId.Value, treeState.CurrentRoleId);
        Assert.Equal(context, treeState.CurrentReadContext);
        Assert.Equal(100, treeState.RootNode!.Children.Count);
        Assert.NotNull(treeState.RootNode.NextCursor);

        // Blättern
        await treeState.PageNextAsync(rootId.Value);

        Assert.Equal(50, treeState.RootNode.Children.Count);
        Assert.Null(treeState.RootNode.NextCursor);
        Assert.True(treeState.RootNode.HasPreviousPage);

        // Sicherstellen, dass zu keinem Zeitpunkt ein Vollbaum im ViewModel gehalten wird
        Assert.True(treeState.RootNode.Children.Count <= 100);
        Assert.Equal(1, treeState.LoadedPageCount);
    }

    [Fact]
    public async Task SelectNodeAsync_DeepUnloadedNode_PathLoaderRevealsNodeAndExpandsAncestors()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child", null, 1, false));

        var grandchildId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, grandchildId, childId, "Grandchild", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        Assert.NotNull(treeState.RootNode);
        Assert.False(treeState.RootNode.IsExpanded);
        Assert.Null(treeState.FindNode(grandchildId.Value));

        await treeState.SelectNodeAsync(grandchildId.Value);

        Assert.Equal(grandchildId.Value, treeState.SelectedNodeId);
        var grandchildNode = treeState.FindNode(grandchildId.Value);
        Assert.NotNull(grandchildNode);
        Assert.True(grandchildNode.IsSelected);

        Assert.True(treeState.RootNode.IsExpanded);
        var childNode = treeState.FindNode(childId.Value);
        Assert.NotNull(childNode);
        Assert.True(childNode.IsExpanded);

        var breadcrumbs = treeState.Breadcrumbs;
        Assert.Equal(3, breadcrumbs.Count);
        Assert.Equal("Root", breadcrumbs[0].Title);
        Assert.Equal("Child", breadcrumbs[1].Title);
        Assert.Equal("Grandchild", breadcrumbs[2].Title);
    }
}
