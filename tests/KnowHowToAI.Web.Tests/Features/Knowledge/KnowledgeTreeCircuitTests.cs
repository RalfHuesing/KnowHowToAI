using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreeCircuitTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly AudienceId DefaultAudienceId = new("Developer");

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

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        // Seite 1: 100 Einträge, NextCursor vorhanden
        Assert.Equal(100, treeState.RootNode!.Children.Count);
        Assert.NotNull(treeState.RootNode.NextCursor);
        Assert.True(treeState.RootNode.HasNextPage);
        Assert.False(treeState.RootNode.HasPreviousPage);
        Assert.Equal("Child 001", treeState.RootNode.Children[0].Title);
        var firstPageNodeIds = treeState.RootNode.Children.Select(child => child.NodeId).ToHashSet();
        Assert.Equal(firstPageNodeIds.Count, treeState.RootNode.Children.Select(child => child.NodeId).Distinct().Count());

        // Weiter blättern -> Seite 2: genau 1 Eintrag (ersetzt die 100 Einträge!)
        await treeState.PageNextAsync(rootId.Value);

        Assert.Single(treeState.RootNode.Children);
        Assert.Equal("Child 101", treeState.RootNode.Children[0].Title);
        Assert.DoesNotContain(treeState.RootNode.Children[0].NodeId, firstPageNodeIds);
        Assert.Null(treeState.RootNode.NextCursor);
        Assert.False(treeState.RootNode.HasNextPage);
        Assert.True(treeState.RootNode.HasPreviousPage);
        Assert.Equal(1, treeState.LoadedPageCount);

        // Zurück blättern -> wieder Seite 1 mit 100 Einträgen aus Cursor-Historie
        await treeState.PagePreviousAsync(rootId.Value);

        Assert.Equal(100, treeState.RootNode.Children.Count);
        Assert.Equal("Child 001", treeState.RootNode.Children[0].Title);
        Assert.True(firstPageNodeIds.SetEquals(treeState.RootNode.Children.Select(child => child.NodeId)));
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
        NodeId? firstChildId = null;
        NodeId? lastChildId = null;
        for (var i = 1; i <= 1050; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            if (i == 1) firstChildId = childId;
            if (i == 1050) lastChildId = childId;
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Child {i:D4}", null, i, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        Assert.Equal(101, treeState.KnownNodeCount);
        Assert.NotNull(treeState.FindNode(firstChildId!.Value.Value));
        Assert.Null(treeState.FindNode(lastChildId!.Value.Value));

        // Über alle Seiten bis zum Ende blättern (11 Seiten)
        var pageCount = 1;
        while (treeState.RootNode!.HasNextPage)
        {
            Assert.True(treeState.RootNode.Children.Count <= 100, "Kinderliste darf nie größer als 100 sein.");
            Assert.True(treeState.KnownNodeCount <= 101, "Reale Anzahl gehaltener Knoten darf nie größer als 101 sein.");
            await treeState.PageNextAsync(rootId.Value);
            pageCount++;
        }

        Assert.Equal(11, pageCount);
        Assert.Equal(50, treeState.RootNode.Children.Count); // Letzte Seite hat 50 Einträge
        Assert.Equal(51, treeState.KnownNodeCount); // Root + 50 Kinder
        Assert.Null(treeState.FindNode(firstChildId.Value.Value));
        Assert.NotNull(treeState.FindNode(lastChildId.Value.Value));
        Assert.False(treeState.RootNode.HasNextPage);
        Assert.True(treeState.RootNode.HasPreviousPage);
        Assert.Equal(1, treeState.LoadedPageCount);

        // Wieder ganz nach vorne blättern
        while (treeState.RootNode.HasPreviousPage)
        {
            Assert.True(treeState.KnownNodeCount <= 101, "Reale Anzahl gehaltener Knoten darf nie größer als 101 sein.");
            await treeState.PagePreviousAsync(rootId.Value);
        }

        Assert.Equal(100, treeState.RootNode.Children.Count);
        Assert.Equal(101, treeState.KnownNodeCount);
        Assert.Equal("Child 0001", treeState.RootNode.Children[0].Title);
        Assert.NotNull(treeState.FindNode(firstChildId.Value.Value));
        Assert.Null(treeState.FindNode(lastChildId.Value.Value));
        Assert.False(treeState.RootNode.HasPreviousPage);
    }

    [Fact]
    public async Task CacheLimit_11thPage_EvictsLruUnselectedSubtreeAndAnnouncesStatus()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        // 11 Kinder unter Root, jedes Kind hat eigene Unterknoten
        var childIds = new List<NodeId>();
        var subChildIds = new List<NodeId>();
        for (var i = 1; i <= 11; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            childIds.Add(childId);
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Subtree {i}", null, i, false));

            var subChildId = new NodeId(Guid.NewGuid());
            subChildIds.Add(subChildId);
            harness.AddNode(new Node(DefaultSnapshotId, subChildId, childId, $"SubChild {i}", null, 1, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);

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

        // Die Aufklappabsicht bleibt erhalten, obwohl die Kindseite aus dem Cache entfernt wurde.
        Assert.True(subtree2Node.IsExpanded);
        Assert.Empty(subtree2Node.Children);
        Assert.Null(treeState.FindNode(subChildIds[1].Value));

        // Subtree 1 (auf Auswahlpfad) bleibt unberührt geöffnet und sein Kind im Index
        var subtree1Node = treeState.FindNode(childIds[0].Value);
        Assert.NotNull(subtree1Node);
        Assert.True(subtree1Node.IsExpanded);
        Assert.NotNull(treeState.FindNode(subChildIds[0].Value));

        // Statusmeldung für role="status" wurde gesetzt
        Assert.NotNull(treeState.StatusMessage);
        Assert.Contains("Subtree 2", treeState.StatusMessage);
        Assert.Contains("Zwischenspeicher", treeState.StatusMessage);

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

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);

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
    public async Task CacheLimit_ElevenWideBranchesAndRepeatedPageSwitches_MaintainsRealNodeAndPageBoundaries()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var branchIds = new List<NodeId>();
        var firstChildOfBranches = new List<NodeId>();

        for (var b = 1; b <= 11; b++)
        {
            var branchId = new NodeId(Guid.NewGuid());
            branchIds.Add(branchId);
            harness.AddNode(new Node(DefaultSnapshotId, branchId, rootId, $"Branch {b:D2}", null, b, false));

            var childCount = b == 11 ? 105 : 20;
            for (var c = 1; c <= childCount; c++)
            {
                var childId = new NodeId(Guid.NewGuid());
                if (c == 1) firstChildOfBranches.Add(childId);
                harness.AddNode(new Node(DefaultSnapshotId, childId, branchId, $"Branch {b:D2} - Child {c:D3}", null, c, false));
            }
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        Assert.Equal(1, treeState.KnownNodeCount); // Nur Root

        await treeState.ExpandNodeAsync(rootId.Value);
        Assert.Equal(1, treeState.LoadedPageCount);
        Assert.Equal(12, treeState.KnownNodeCount); // Root + 11 Branches

        // Selektiere Branch 1 (bleibt mit Root auf Auswahlpfad)
        await treeState.SelectNodeAsync(branchIds[0].Value);

        // Expandiere Branches 1 bis 9 (insgesamt 10 geladene Seiten: Root + 9 Branches)
        for (var b = 0; b < 9; b++)
        {
            await treeState.ExpandNodeAsync(branchIds[b].Value);
        }

        Assert.Equal(10, treeState.LoadedPageCount);
        Assert.Equal(12 + (9 * 20), treeState.KnownNodeCount);
        Assert.NotNull(treeState.FindNode(firstChildOfBranches[0].Value));

        // 11. Anforderung: Expandiere Branch 10 -> Branch 2 (frühester unselektierter) wird evictet
        await treeState.ExpandNodeAsync(branchIds[9].Value);
        Assert.Equal(10, treeState.LoadedPageCount);
        Assert.Equal(12 + (9 * 20), treeState.KnownNodeCount); // Branch 2 Kinder entfernt, Branch 10 Kinder hinzugefügt
        Assert.Null(treeState.FindNode(firstChildOfBranches[1].Value)); // Branch 2 Kinder sind aus dem Index entfernt
        Assert.NotNull(treeState.FindNode(firstChildOfBranches[0].Value)); // Branch 1 Kinder bleiben erhalten
        Assert.NotNull(treeState.FindNode(firstChildOfBranches[9].Value));

        // 12. Anforderung: Expandiere Branch 11 (hat 105 Kinder -> Seite 1 lädt 100) -> Branch 3 wird evictet
        await treeState.ExpandNodeAsync(branchIds[10].Value);
        Assert.Equal(10, treeState.LoadedPageCount);
        Assert.Null(treeState.FindNode(firstChildOfBranches[2].Value)); // Branch 3 Kinder entfernt
        Assert.NotNull(treeState.FindNode(firstChildOfBranches[10].Value)); // Branch 11 Kinder da
        // Bekannte Knoten: Root (1) + 11 Branches (11) + 8 Zweige à 20 Kinder (160) + Branch 11 Seite 1 (100) = 272
        Assert.Equal(272, treeState.KnownNodeCount);

        // Blättere auf Branch 11 zur Seite 2 (5 Kinder) -> Seite 1 wird ersetzt und bereinigt
        await treeState.PageNextAsync(branchIds[10].Value);
        Assert.Equal(10, treeState.LoadedPageCount);
        // Bekannte Knoten: 1 + 11 + 160 + 5 = 177
        Assert.Equal(177, treeState.KnownNodeCount);
        Assert.Null(treeState.FindNode(firstChildOfBranches[10].Value)); // Kind von Seite 1 ist nicht mehr im Index!

        // Blättere auf Branch 11 zurück zu Seite 1 -> Seite 2 wird ersetzt und bereinigt
        await treeState.PagePreviousAsync(branchIds[10].Value);
        Assert.Equal(10, treeState.LoadedPageCount);
        Assert.Equal(272, treeState.KnownNodeCount);
        Assert.NotNull(treeState.FindNode(firstChildOfBranches[10].Value));
    }

    [Fact]
    public async Task RequestRacing_DelayedRepositoryCallIgnoresCancellation_OnlySuccessorResultRemainsVisibleAndRegistered()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        for (var i = 1; i <= 200; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Child {i:D3}", null, i, false));
        }

        var repos = harness.CreateRepositories();
        var firstCallStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCallCanFinish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;

        var delayedRepo = new DelayedHierarchyRepository(repos.Hierarchy, async (snapshotId, ct) =>
        {
            var current = Interlocked.Increment(ref callCount);
            if (current == 3)
            {
                // Erster PageNext-Request (Call 3): wird verzögert und ignoriert Cancellation
                firstCallStarted.TrySetResult(true);
                await firstCallCanFinish.Task;
                // Liefert veraltetes/abweichendes Ergebnis (nur 1 künstliches Kind)
                return new List<Node>
                {
                    new(snapshotId, rootId, null, "Root", null, 0, false),
                    new(snapshotId, new NodeId(Guid.NewGuid()), rootId, "Stale Child From Call 1", null, 1, false)
                };
            }

            return await repos.Hierarchy.ListBySnapshotAsync(snapshotId, ct);
        });

        var customRepos = new SnapshotReadRepositories(
            repos.Snapshots,
            repos.Transactions,
            delayedRepo,
            repos.Contents,
            repos.Audiences,
            repos.Dependencies,
            repos.WorkingSnapshots);

        var policy = new Core.Application.Policies.RetrievalPolicy
        {
            DefaultPageSize = 100,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 100,
            SnippetMaximumCharacters = 100
        };
        var service = new NavigationService(customRepos, policy);
        using var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value); // callCount = 1 (Root), callCount = 2 (Children)

        Assert.Equal(100, treeState.RootNode!.Children.Count);
        Assert.Equal("Child 001", treeState.RootNode.Children[0].Title);

        // Starte Request 1 (PageNext) -> blockiert in delayedRepo (callCount = 3)
        var request1Task = treeState.PageNextAsync(rootId.Value);
        await firstCallStarted.Task;

        // Starte Nachfolger-Request 2 (PageNext) -> bricht Request 1 ab und läuft sofort durch (callCount = 4)
        var request2Task = treeState.PageNextAsync(rootId.Value);
        await request2Task;

        // Request 2 hat regulär Seite 2 geladen
        Assert.Equal(100, treeState.RootNode.Children.Count);
        Assert.Equal("Child 101", treeState.RootNode.Children[0].Title);
        Assert.False(treeState.RootNode.IsLoading);

        // Jetzt lassen wir den alten Request 1 nach dem Nachfolger fertig werden
        firstCallCanFinish.TrySetResult(true);
        await request1Task;

        // Überholte Antwort von Request 1 darf Zustand keinesfalls überschreiben
        Assert.Equal(100, treeState.RootNode.Children.Count);
        Assert.Equal("Child 101", treeState.RootNode.Children[0].Title);
        Assert.DoesNotContain(treeState.RootNode.Children, c => c.Title == "Stale Child From Call 1");
        Assert.False(treeState.RootNode.IsLoading);
        Assert.Null(treeState.RootNode.Error);
    }

    [Fact]
    public async Task ContextSwitch_DelayedRepositoryCallIgnoresCancellation_DoesNotOverwriteNewContext()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var root1Id = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, root1Id, null, "Root Snap 1", null, 0, false));
        var child1Id = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, child1Id, root1Id, "Child Snap 1", null, 1, false));

        var snapshotId2 = new SnapshotId(2);
        harness.AddHistoricalSnapshot(new Snapshot(snapshotId2, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var root2Id = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(snapshotId2, root2Id, null, "Root Snap 2", null, 0, false));

        var repos = harness.CreateRepositories();
        var call1Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var call1CanFinish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;

        var delayedRepo = new DelayedHierarchyRepository(repos.Hierarchy, async (snapshotId, ct) =>
        {
            var current = Interlocked.Increment(ref callCount);
            if (current == 2)
            {
                // Erster Expand-Request: wartet und ignoriert Cancellation
                call1Started.TrySetResult(true);
                await call1CanFinish.Task;
                return await repos.Hierarchy.ListBySnapshotAsync(snapshotId, CancellationToken.None);
            }

            return await repos.Hierarchy.ListBySnapshotAsync(snapshotId, ct);
        });

        var customRepos = new SnapshotReadRepositories(
            repos.Snapshots,
            repos.Transactions,
            delayedRepo,
            repos.Contents,
            repos.Audiences,
            repos.Dependencies,
            repos.WorkingSnapshots);

        var policy = new Core.Application.Policies.RetrievalPolicy
        {
            DefaultPageSize = 100,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 100,
            SnippetMaximumCharacters = 100
        };
        var service = new NavigationService(customRepos, policy);
        using var treeState = new KnowledgeTreeState(service);

        // 1. Kontext: Snapshot 1 initialisieren
        await treeState.InitializeAsync(new ReadContext(SnapshotId: DefaultSnapshotId), DefaultAudienceId.Value); // callCount = 1

        // 2. Expand auf Root 1 starten (verzögert)
        var expandTask = treeState.ExpandNodeAsync(root1Id.Value); // callCount = 2
        await call1Started.Task;

        // 3. Kontextwechsel: Snapshot 2 initialisieren
        await treeState.InitializeAsync(new ReadContext(SnapshotId: snapshotId2), DefaultAudienceId.Value); // callCount = 3

        Assert.Equal("Root Snap 2", treeState.RootNode!.Title);
        Assert.Equal(0, treeState.LoadedPageCount);

        // 4. Delayed Call 1 freigeben
        call1CanFinish.TrySetResult(true);
        await expandTask;

        // Snapshot 2 Zustand bleibt unverändert sauber
        Assert.Equal("Root Snap 2", treeState.RootNode!.Title);
        Assert.Equal(0, treeState.LoadedPageCount);
        Assert.Null(treeState.FindNode(child1Id.Value));
    }
}
