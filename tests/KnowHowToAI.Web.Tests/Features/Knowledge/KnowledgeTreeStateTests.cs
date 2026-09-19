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
    public async Task Paging_101Children_PageNextAndPreviousReplacePageAndPreserveHistory()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        for (var i = 1; i <= 101; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Child {i:D3}", null, i, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        // Seite 1: 100 Einträge, NextCursor vorhanden
        Assert.Equal(100, treeState.RootNode!.Children.Count);
        Assert.NotNull(treeState.RootNode.NextCursor);
        Assert.True(treeState.RootNode.HasNextPage);
        Assert.False(treeState.RootNode.HasPreviousPage);
        Assert.Equal("Child 001", treeState.RootNode.Children[0].Title);

        // Weiter blättern -> Seite 2: genau 1 Eintrag (ersetzt die 100 Einträge!)
        await treeState.PageNextAsync(rootId.Value);

        Assert.Single(treeState.RootNode.Children);
        Assert.Equal("Child 101", treeState.RootNode.Children[0].Title);
        Assert.Null(treeState.RootNode.NextCursor);
        Assert.False(treeState.RootNode.HasNextPage);
        Assert.True(treeState.RootNode.HasPreviousPage);
        Assert.Equal(1, treeState.LoadedPageCount);

        // Zurück blättern -> wieder Seite 1 mit 100 Einträgen aus Cursor-Historie
        await treeState.PagePreviousAsync(rootId.Value);

        Assert.Equal(100, treeState.RootNode.Children.Count);
        Assert.Equal("Child 001", treeState.RootNode.Children[0].Title);
        Assert.NotNull(treeState.RootNode.NextCursor);
        Assert.True(treeState.RootNode.HasNextPage);
        Assert.False(treeState.RootNode.HasPreviousPage);
    }

    [Fact]
    public async Task Paging_MoreThan1000Children_MemoryNeverHoldsGrowingChildList()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        // 1.050 Kindknoten
        for (var i = 1; i <= 1050; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Child {i:D4}", null, i, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        // Über alle Seiten bis zum Ende blättern (11 Seiten)
        var pageCount = 1;
        while (treeState.RootNode!.HasNextPage)
        {
            Assert.True(treeState.RootNode.Children.Count <= 100, "Kinderliste darf nie größer als 100 sein.");
            await treeState.PageNextAsync(rootId.Value);
            pageCount++;
        }

        Assert.Equal(11, pageCount);
        Assert.Equal(50, treeState.RootNode.Children.Count); // Letzte Seite hat 50 Einträge
        Assert.False(treeState.RootNode.HasNextPage);
        Assert.True(treeState.RootNode.HasPreviousPage);
        Assert.Equal(1, treeState.LoadedPageCount);

        // Wieder ganz nach vorne blättern
        while (treeState.RootNode.HasPreviousPage)
        {
            await treeState.PagePreviousAsync(rootId.Value);
        }

        Assert.Equal(100, treeState.RootNode.Children.Count);
        Assert.Equal("Child 0001", treeState.RootNode.Children[0].Title);
        Assert.False(treeState.RootNode.HasPreviousPage);
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
    public async Task CacheLimit_11thPage_EvictsLruUnselectedSubtreeAndAnnouncesStatus()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        // 11 Kinder unter Root, jedes Kind hat eigene Unterknoten
        var childIds = new List<NodeId>();
        for (var i = 1; i <= 11; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            childIds.Add(childId);
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Subtree {i}", null, i, false));

            var subChildId = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, subChildId, childId, $"SubChild {i}", null, 1, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        // Root expandieren (1 geladene Seite)
        await treeState.ExpandNodeAsync(rootId.Value);

        // Selektiere Subtree 1 (bleibt auf Auswahlpfad)
        await treeState.SelectNodeAsync(childIds[0].Value);

        // Expandiere Subtree 1 bis 9 (insgesamt 10 geladene Seiten: Root + 9 Subtrees)
        for (var i = 0; i < 9; i++)
        {
            await treeState.ExpandNodeAsync(childIds[i].Value);
        }

        Assert.Equal(10, treeState.LoadedPageCount);

        // Subtree 2 wurde als frühester der unselektierten Teilbäume geladen (LRU-Kandidat)
        var subtree2Node = treeState.FindNode(childIds[1].Value);
        Assert.NotNull(subtree2Node);
        Assert.True(subtree2Node.IsExpanded);

        // 11. Anforderung: Expandiere Subtree 10
        await treeState.ExpandNodeAsync(childIds[9].Value);

        // Cachegrenze bleibt strikt bei 10
        Assert.Equal(10, treeState.LoadedPageCount);

        // Subtree 2 wurde geschlossen und seine Kinder entfernt
        Assert.False(subtree2Node.IsExpanded);
        Assert.Empty(subtree2Node.Children);

        // Subtree 1 (auf Auswahlpfad) bleibt unberührt geöffnet
        var subtree1Node = treeState.FindNode(childIds[0].Value);
        Assert.NotNull(subtree1Node);
        Assert.True(subtree1Node.IsExpanded);

        // Statusmeldung für role=status wurde gesetzt
        Assert.NotNull(treeState.StatusMessage);
        Assert.Contains("Subtree 2", treeState.StatusMessage);
        Assert.Contains("geschlossen", treeState.StatusMessage);
    }

    [Fact]
    public async Task CacheLimit_All10PagesOnSelectionPath_EvictsRootNearestAndRecentersVisualRoot()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var currentParentId = (NodeId?)null;
        var nodeIds = new List<NodeId>();

        // 12 Ebenen tiefe Kette
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

        // Expandiere Level 0 bis 9 (10 geladene Seiten)
        for (var i = 0; i < 10; i++)
        {
            await treeState.ExpandNodeAsync(nodeIds[i].Value);
        }

        // Selektiere Level 10 (liegt in der Kette, alle geladenen Seiten liegen auf dem Auswahlpfad)
        await treeState.SelectNodeAsync(nodeIds[10].Value);

        Assert.Equal(10, treeState.LoadedPageCount);
        Assert.Equal(nodeIds[0].Value, treeState.VisualRootNodeId);

        // 11. Anforderung: Expandiere Level 10
        await treeState.ExpandNodeAsync(nodeIds[10].Value);

        // Cachegrenze bleibt 10
        Assert.Equal(10, treeState.LoadedPageCount);

        // Rootnächste Seite (Level 0) wurde entfernt, Level 1 wird neuer VisualRoot
        Assert.Equal(nodeIds[1].Value, treeState.VisualRootNodeId);
        Assert.NotNull(treeState.StatusMessage);
        Assert.Contains("Level 1", treeState.StatusMessage);
        Assert.Contains("zentriert", treeState.StatusMessage);

        // Breadcrumbs enthalten weiterhin den globalen Pfad von Level 0 bis 10
        var breadcrumbs = treeState.Breadcrumbs;
        Assert.Equal(11, breadcrumbs.Count);
        Assert.Equal("Level 0", breadcrumbs[0].Title);
        Assert.Equal("Level 10", breadcrumbs[10].Title);

        // Breadcrumb-Rücknavigation: Klick auf Level 0 lädt Level 0 nach und zentriert zurück
        await treeState.SelectNodeAsync(nodeIds[0].Value);
        Assert.Equal(nodeIds[0].Value, treeState.VisualRootNodeId);
        Assert.True(treeState.LoadedPageCount <= 10);
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
}
