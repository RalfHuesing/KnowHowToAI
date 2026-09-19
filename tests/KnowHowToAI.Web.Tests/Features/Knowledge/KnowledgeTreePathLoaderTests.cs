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
    private static readonly RoleId DefaultRoleId = new("Developer");

    [Fact]
    public async Task EnsurePathLoadedAsync_WhenNodeAlreadyKnown_DoesNothing()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);

        var knownId = Guid.NewGuid();
        var expanded = false;

        var loader = new KnowledgeTreePathLoader(
            service,
            (id, ct) => { expanded = true; return Task.CompletedTask; },
            (id, ct) => Task.CompletedTask,
            id => id == knownId,
            id => null);

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
            id => knownNodes.TryGetValue(id, out var n) ? n : null);

        await loader.EnsurePathLoadedAsync(grandchildId.Value, new ReadContext(), DefaultRoleId.Value, CancellationToken.None);

        Assert.Contains(rootId.Value, expandedIds);
        Assert.Contains(childId.Value, expandedIds);
        Assert.True(knownNodes.ContainsKey(grandchildId.Value));
    }
}
