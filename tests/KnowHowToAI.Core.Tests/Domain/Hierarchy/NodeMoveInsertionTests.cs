using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Tests.Domain.Hierarchy;

[Trait("Category", "Unit")]
public sealed class NodeMoveInsertionTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = NodeIdFor(1);
    private static readonly NodeId FirstNodeId = NodeIdFor(2);
    private static readonly NodeId SecondNodeId = NodeIdFor(3);
    private static readonly NodeId ThirdNodeId = NodeIdFor(4);

    [Fact]
    public void Move_BeforeTarget_InsertsExactlyWhenSourceWasAfterTarget()
    {
        var result = Move(
        [
            Node(RootNodeId),
            Node(FirstNodeId, RootNodeId, "Ziel", 0),
            Node(SecondNodeId, RootNodeId, "Mitte", 1),
            Node(ThirdNodeId, RootNodeId, "Quelle", 2)
        ],
            ThirdNodeId,
            RootNodeId,
            1);

        Assert.Equal([FirstNodeId, ThirdNodeId, SecondNodeId], OrderedChildren(result));
        Assert.Equal([0, 1, 2], OrderedChildren(result).Select(nodeId => Find(result, nodeId).SortOrder));
    }

    [Fact]
    public void Move_AfterTarget_InsertsExactlyWhenSourceWasBeforeTarget()
    {
        var result = Move(
        [
            Node(RootNodeId),
            Node(FirstNodeId, RootNodeId, "Quelle", 0),
            Node(SecondNodeId, RootNodeId, "Ziel", 1),
            Node(ThirdNodeId, RootNodeId, "Ende", 2)
        ],
            FirstNodeId,
            RootNodeId,
            1);

        Assert.Equal([SecondNodeId, FirstNodeId, ThirdNodeId], OrderedChildren(result));
    }

    [Fact]
    public void Move_ToAnotherParent_InsertsAtFirstAndNormalizesBothGroups()
    {
        var result = Move(
        [
            Node(RootNodeId),
            Node(FirstNodeId, RootNodeId, "Quelle", 0),
            Node(SecondNodeId, RootNodeId, "Bestehend", 1),
            Node(ThirdNodeId, RootNodeId, "Zielparent", 2)
        ],
            FirstNodeId,
            ThirdNodeId,
            0);

        Assert.Equal(ThirdNodeId, Find(result, FirstNodeId).ParentNodeId);
        Assert.Equal(0, Find(result, FirstNodeId).SortOrder);
        Assert.Equal(1, Find(result, ThirdNodeId).SortOrder);
        Assert.Equal(0, Find(result, SecondNodeId).SortOrder);
    }

    private static IReadOnlyList<Node> Move(
        IReadOnlyList<Node> nodes,
        NodeId nodeId,
        NodeId parentNodeId,
        int sortOrder)
    {
        var service = new NodeMutationService(new FixedIdentifierGenerator());
        var result = service.Move(nodes, new MoveNodeCommand(nodeId, parentNodeId, sortOrder));
        return AssertResult(result);
    }

    private static IReadOnlyList<Node> AssertResult(Result<HierarchyMutationResult> result)
    {
        Assert.True(result.IsSuccess);
        return result.Value!.Nodes;
    }

    private static Node Find(IEnumerable<Node> nodes, NodeId nodeId) =>
        Assert.Single(nodes.Where(node => node.NodeId == nodeId));

    private static IReadOnlyList<NodeId> OrderedChildren(IEnumerable<Node> nodes) =>
        nodes.Where(node => !node.IsDeleted && node.ParentNodeId == RootNodeId)
            .OrderBy(node => node.SortOrder)
            .Select(node => node.NodeId)
            .ToArray();

    private static Node Node(NodeId nodeId, NodeId? parentNodeId = null, string title = "Titel", int sortOrder = 0) =>
        new(SnapshotId, nodeId, parentNodeId, title, null, sortOrder, IsDeleted: false);

    private static NodeId NodeIdFor(int value) => new(new Guid(value, 0, 0, new byte[8]));

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => throw new NotSupportedException();
        public NodeId CreateNodeId() => throw new NotSupportedException();
        public ContentRevisionId CreateContentRevisionId() => throw new NotSupportedException();
    }
}
