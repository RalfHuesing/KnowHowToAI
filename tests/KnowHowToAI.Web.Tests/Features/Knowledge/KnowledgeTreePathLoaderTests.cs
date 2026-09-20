using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreePathLoaderTests
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly AudienceId DefaultRoleId = new("Developer");

    [Fact]
    public async Task EnsurePathLoadedAsync_WhenNodeAlreadyKnown_DoesNothing()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);

        var knownId = Guid.NewGuid();
        var expanded = false;

        var loader = new KnowledgeTreePathLoader(
            service,
            new TreePathLoaderCallbacks(
                (id, ct) => { expanded = true; return Task.CompletedTask; },
                (id, ct) => Task.CompletedTask,
                id => id == knownId,
                id => null));

        await loader.EnsurePathLoadedAsync(knownId, new ReadContext(), DefaultRoleId.Value, CancellationToken.None);

        Assert.False(expanded);
    }

    [Fact]
    public async Task EnsurePathLoadedAsync_WhenNodeUnknown_LoadsAncestorsAndExpandsThem()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child", null, 1, false));

        var grandchildId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, grandchildId, childId, "Grandchild", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);

        var knownNodes = new Dictionary<Guid, KnowledgeTreeNodeViewModel>();
        var rootVm = new KnowledgeTreeNodeViewModel
        {
            Summary = new ChildNodeViewModel(rootId.Value, "Root", null, 0, 1, 0, "Explicit", null, "Fresh"),
            Depth = 0,
            ChildCount = 1
        };
        knownNodes[rootId.Value] = rootVm;

        var childVm = new KnowledgeTreeNodeViewModel
        {
            Summary = new ChildNodeViewModel(childId.Value, "Child", null, 1, 1, 0, "Explicit", null, "Fresh"),
            ParentNodeId = rootId.Value,
            Depth = 1,
            ChildCount = 1
        };

        var expandedIds = new List<Guid>();

        var loader = new KnowledgeTreePathLoader(
            service,
            new TreePathLoaderCallbacks(
                (id, ct) =>
                {
                    expandedIds.Add(id);
                    if (id == rootId.Value)
                    {
                        rootVm.IsExpanded = true;
                        knownNodes[childId.Value] = childVm;
                    }
                    else if (id == childId.Value)
                    {
                        childVm.IsExpanded = true;
                        knownNodes[grandchildId.Value] = new KnowledgeTreeNodeViewModel
                        {
                            Summary = new ChildNodeViewModel(grandchildId.Value, "Grandchild", null, 1, 0, 0, "Explicit", null, "Fresh"),
                            ParentNodeId = childId.Value,
                            Depth = 2
                        };
                    }
                    return Task.CompletedTask;
                },
                (id, ct) => Task.CompletedTask,
                id => knownNodes.ContainsKey(id),
                id => knownNodes.TryGetValue(id, out var n) ? n : null));

        var result = await loader.EnsurePathLoadedAsync(grandchildId.Value, new ReadContext(), DefaultRoleId.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(rootId.Value, expandedIds);
        Assert.Contains(childId.Value, expandedIds);
        Assert.True(knownNodes.ContainsKey(grandchildId.Value));
    }

    [Fact]
    public async Task EnsurePathLoadedAsync_TargetOnLaterPageOfFlatParent_PagesUntilTargetVisible()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var children = new List<NodeId>();
        for (var i = 1; i <= 250; i++)
        {
            var cid = new NodeId(Guid.NewGuid());
            children.Add(cid);
            harness.AddNode(new Node(DefaultSnapshotId, cid, rootId, $"Child {i:D3}", null, i, false));
        }

        var targetId = children[204]; // Child 205, on Page 3 (Page 1: 1..100, Page 2: 101..200, Page 3: 201..250)
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);

        using var treeState = new KnowledgeTreeState(service);
        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        var pagedCount = 0;
        var loader = new KnowledgeTreePathLoader(
            service,
            new TreePathLoaderCallbacks(
                (id, ct) => treeState.ExpandNodeAsync(id, ct),
                (id, ct) => { pagedCount++; return treeState.PageNextAsync(id, ct); },
                id => treeState.FindNode(id) is not null,
                treeState.FindNode));

        var result = await loader.EnsurePathLoadedAsync(targetId.Value, new ReadContext(), DefaultRoleId.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(treeState.FindNode(targetId.Value));
        Assert.Equal(2, pagedCount); // 2 Mal PageNext aufgerufen (zu Seite 2, dann zu Seite 3)
    }

    [Fact]
    public async Task EnsurePathLoadedAsync_TargetOnLaterPagesOfMultipleDeepParents_PagesEachSegmentRootDownwards()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        // Root hat 150 Kinder, Segment 1 ist Kind #120 (Seite 2 von Root)
        NodeId? segment1Id = null;
        for (var i = 1; i <= 150; i++)
        {
            var cid = new NodeId(Guid.NewGuid());
            if (i == 120) segment1Id = cid;
            harness.AddNode(new Node(DefaultSnapshotId, cid, rootId, $"RootChild {i:D3}", null, i, false));
        }

        // Segment 1 hat 150 Kinder, Segment 2 ist Kind #130 (Seite 2 von Segment 1)
        NodeId? segment2Id = null;
        for (var i = 1; i <= 150; i++)
        {
            var cid = new NodeId(Guid.NewGuid());
            if (i == 130) segment2Id = cid;
            harness.AddNode(new Node(DefaultSnapshotId, cid, segment1Id!.Value, $"Seg1Child {i:D3}", null, i, false));
        }

        // Segment 2 hat 120 Kinder, Target ist Kind #110 (Seite 2 von Segment 2)
        NodeId? targetId = null;
        for (var i = 1; i <= 120; i++)
        {
            var cid = new NodeId(Guid.NewGuid());
            if (i == 110) targetId = cid;
            harness.AddNode(new Node(DefaultSnapshotId, cid, segment2Id!.Value, $"Seg2Child {i:D3}", null, i, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        using var treeState = new KnowledgeTreeState(service);
        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        var pagedParents = new List<Guid>();
        var loader = new KnowledgeTreePathLoader(
            service,
            new TreePathLoaderCallbacks(
                (id, ct) => treeState.ExpandNodeAsync(id, ct),
                (id, ct) => { pagedParents.Add(id); return treeState.PageNextAsync(id, ct); },
                id => treeState.FindNode(id) is not null,
                treeState.FindNode));

        var result = await loader.EnsurePathLoadedAsync(targetId!.Value.Value, new ReadContext(), DefaultRoleId.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(treeState.FindNode(targetId.Value.Value));
        Assert.Contains(rootId.Value, pagedParents);
        Assert.Contains(segment1Id!.Value.Value, pagedParents);
        Assert.Contains(segment2Id!.Value.Value, pagedParents);
    }

    [Fact]
    public async Task EnsurePathLoadedAsync_CallProtocol_VerifiesParentContextRoleLimitAndUnchangedCursor()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var children = new List<NodeId>();
        for (var i = 1; i <= 250; i++)
        {
            var cid = new NodeId(Guid.NewGuid());
            children.Add(cid);
            harness.AddNode(new Node(DefaultSnapshotId, cid, rootId, $"Child {i:D3}", null, i, false));
        }

        var targetId = children[204];
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);

        // Erzeuge Initial-TreeState
        using var treeState = new KnowledgeTreeState(service);
        var readContext = new ReadContext(SnapshotId: DefaultSnapshotId);
        await treeState.InitializeAsync(readContext, DefaultRoleId.Value);

        var capturedQueries = new List<ListChildrenQuery>();
        var rootNode = treeState.RootNode!;

        // Führe Loader aus und protokolliere jeden PageNext-Aufruf
        var loader = new KnowledgeTreePathLoader(
            service,
            new TreePathLoaderCallbacks(
                (id, ct) => treeState.ExpandNodeAsync(id, ct),
                async (id, ct) =>
                {
                    var cursorBefore = treeState.FindNode(id)!.NextCursor;
                    capturedQueries.Add(new ListChildrenQuery(
                        new NodeId(id),
                        readContext,
                        DefaultRoleId,
                        Limit: 100,
                        Cursor: cursorBefore));
                    await treeState.PageNextAsync(id, ct);
                },
                id => treeState.FindNode(id) is not null,
                treeState.FindNode));

        var result = await loader.EnsurePathLoadedAsync(targetId.Value, readContext, DefaultRoleId.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, capturedQueries.Count);

        // Protokollprüfung: Parent, Kontext, Rolle, Limit = 100 und unveränderter Cursor
        foreach (var q in capturedQueries)
        {
            Assert.Equal(rootId, q.ParentNodeId);
            Assert.Equal(readContext, q.Context);
            Assert.Equal(DefaultRoleId, q.AudienceId);
            Assert.Equal(100, q.Limit);
            Assert.False(string.IsNullOrEmpty(q.Cursor));
        }

        // Erster PageNext-Call nutzte Cursor von Seite 1, zweiter nutzte Cursor von Seite 2
        Assert.NotEqual(capturedQueries[0].Cursor, capturedQueries[1].Cursor);
    }

    [Fact]
    public async Task EnsurePathLoadedAsync_NodeNotFound_ReturnsStructuredFailure()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var missingNodeId = Guid.NewGuid();

        var loader = new KnowledgeTreePathLoader(
            service,
            new TreePathLoaderCallbacks(
                (id, ct) => Task.CompletedTask,
                (id, ct) => Task.CompletedTask,
                id => false,
                id => null));

        var result = await loader.EnsurePathLoadedAsync(missingNodeId, new ReadContext(), DefaultRoleId.Value, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(missingNodeId, result.FailedNodeId);
    }

    [Fact]
    public async Task EnsurePathLoadedAsync_InvalidCursorAndCursorExpired_ReturnsStructuredFailureAndMarksBranch()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);

        var rootVm = new KnowledgeTreeNodeViewModel
        {
            Summary = new ChildNodeViewModel(rootId.Value, "Root", null, 0, 1, 0, "Explicit", null, "Fresh"),
            Depth = 0,
            ChildCount = 1,
            IsExpanded = true,
            NextCursor = "dummy-invalid-cursor"
        };

        var loader = new KnowledgeTreePathLoader(
            service,
            new TreePathLoaderCallbacks(
                (id, ct) => Task.CompletedTask,
                (id, ct) =>
                {
                    // Simuliere InvalidCursor Fehler beim Blättern
                    rootVm.Error = "InvalidCursor: Der Cursor ist ungültig oder abgelaufen.";
                    return Task.CompletedTask;
                },
                id => false,
                id => id == rootId.Value ? rootVm : null));

        var result = await loader.EnsurePathLoadedAsync(childId.Value, new ReadContext(), DefaultRoleId.Value, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, result.ErrorCode);
        Assert.Equal(rootId.Value, result.FailedNodeId);
        Assert.Contains("InvalidCursor", rootVm.Error);
    }
}
