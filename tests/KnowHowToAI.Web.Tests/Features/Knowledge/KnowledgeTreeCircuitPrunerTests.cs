using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreeCircuitPrunerTests
{
    private static KnowledgeTreeNodeViewModel CreateNode(Guid id, Guid? parentId, string title, int childCount)
    {
        var summary = new ChildNodeViewModel(
            id,
            title,
            null,
            0,
            childCount,
            0,
            "Available",
            "Developer",
            "Current");

        return new KnowledgeTreeNodeViewModel
        {
            Summary = summary,
            ParentNodeId = parentId,
            ChildCount = childCount
        };
    }

    [Fact]
    public void Prune_RemovesOffPathNodesAndResetsVisualRootWhenEvicted()
    {
        var rootId = Guid.NewGuid();
        var rootNode = CreateNode(rootId, null, "Root", 1);

        var child1Id = Guid.NewGuid();
        var child1 = CreateNode(child1Id, rootId, "Child 1", 0);
        rootNode.Children = new[] { child1 };

        var child2Id = Guid.NewGuid();
        var child2 = CreateNode(child2Id, rootId, "Child 2", 0);

        var knownNodes = new Dictionary<Guid, KnowledgeTreeNodeViewModel>
        {
            [rootId] = rootNode,
            [child1Id] = child1,
            [child2Id] = child2
        };

        var cache = new KnowledgeTreePageCache();
        cache.RecordPageLoaded(rootId, null);

        var context = new CircuitPruneContext(rootNode, cache, knownNodes);

        // Child 2 is not in rootNode.Children (page was replaced) and not on selection path
        var (selected, visualRoot) = KnowledgeTreeCircuitPruner.Prune(context, selectedNodeId: child1Id, visualRootNodeId: child2Id);

        Assert.True(knownNodes.ContainsKey(rootId));
        Assert.True(knownNodes.ContainsKey(child1Id));
        Assert.False(knownNodes.ContainsKey(child2Id));
        Assert.Equal(child1Id, selected);
        Assert.Equal(rootId, visualRoot);
    }

    [Fact]
    public void Prune_PreservesSelectedAncestorChainEvenIfSubtreeUnloaded()
    {
        var rootId = Guid.NewGuid();
        var rootNode = CreateNode(rootId, null, "Root", 1);

        var childId = Guid.NewGuid();
        var child = CreateNode(childId, rootId, "Child", 1);

        var grandchildId = Guid.NewGuid();
        var grandchild = CreateNode(grandchildId, childId, "Grandchild", 0);

        var knownNodes = new Dictionary<Guid, KnowledgeTreeNodeViewModel>
        {
            [rootId] = rootNode,
            [childId] = child,
            [grandchildId] = grandchild
        };

        var cache = new KnowledgeTreePageCache();
        cache.RecordPageLoaded(rootId, null);

        var context = new CircuitPruneContext(rootNode, cache, knownNodes);

        var (selected, visualRoot) = KnowledgeTreeCircuitPruner.Prune(context, selectedNodeId: grandchildId, visualRootNodeId: rootId);

        Assert.Equal(grandchildId, selected);
        Assert.Equal(rootId, visualRoot);
        Assert.True(knownNodes.ContainsKey(rootId));
        Assert.True(knownNodes.ContainsKey(childId));
        Assert.True(knownNodes.ContainsKey(grandchildId));
    }
}
