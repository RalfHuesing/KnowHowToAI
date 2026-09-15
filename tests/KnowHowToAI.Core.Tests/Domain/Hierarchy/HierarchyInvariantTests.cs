using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Tests.Domain.Hierarchy;

[Trait("Category", "Unit")]
public sealed class NodeMutationServiceTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = NodeIdFor(1);
    private static readonly NodeId FirstNodeId = NodeIdFor(2);
    private static readonly NodeId SecondNodeId = NodeIdFor(3);
    private static readonly NodeId ThirdNodeId = NodeIdFor(4);

    [Fact]
    public void Validate_EmptyHierarchy_IsValid()
    {
        var report = HierarchyValidator.Validate([]);

        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public void Validate_MultipleActiveRoots_ReturnsRootAlreadyExists()
    {
        var report = HierarchyValidator.Validate([Node(RootNodeId), Node(FirstNodeId)]);

        var error = Assert.Single(report.Errors);
        Assert.Equal(HierarchyErrorCodes.RootAlreadyExists, error.Code);
        Assert.Equal("2", error.Details[HierarchyErrorCodes.RootCountDetail]);
    }

    [Fact]
    public void Validate_WhitespaceTitle_ReturnsTitleRequired()
    {
        var report = HierarchyValidator.Validate([Node(RootNodeId, title: " \t ")]);

        var error = Assert.Single(report.Errors);
        Assert.Equal(HierarchyErrorCodes.TitleRequired, error.Code);
        Assert.Equal(RootNodeId.ToString(), error.Details[HierarchyErrorCodes.NodeIdDetail]);
    }

    [Fact]
    public void Validate_DeletedParent_ReturnsParentNotFound()
    {
        var report = HierarchyValidator.Validate(
        [
            Node(RootNodeId, isDeleted: true),
            Node(FirstNodeId, parentNodeId: RootNodeId)
        ]);

        var error = Assert.Single(report.Errors);
        Assert.Equal(HierarchyErrorCodes.ParentNotFound, error.Code);
        Assert.Equal(RootNodeId.ToString(), error.Details[HierarchyErrorCodes.ParentNodeIdDetail]);
    }

    [Fact]
    public void Validate_ParentFromAnotherSnapshot_ReturnsSnapshotAndParentErrors()
    {
        var otherSnapshotNode = new Node(
            new SnapshotId(43),
            FirstNodeId,
            RootNodeId,
            "Child",
            null,
            0,
            IsDeleted: false);

        var report = HierarchyValidator.Validate([Node(RootNodeId), otherSnapshotNode]);

        Assert.Equal(
            [HierarchyErrorCodes.SnapshotMismatch, HierarchyErrorCodes.ParentNotFound],
            report.Errors.Select(error => error.Code));
    }

    [Fact]
    public void Validate_SelfParent_ReturnsSelfParentNotAllowed()
    {
        var report = HierarchyValidator.Validate([Node(RootNodeId, parentNodeId: RootNodeId)]);

        var error = Assert.Single(report.Errors);
        Assert.Equal(HierarchyErrorCodes.SelfParentNotAllowed, error.Code);
    }

    [Fact]
    public void Validate_IndirectCycle_ReturnsHierarchyCycle()
    {
        var report = HierarchyValidator.Validate(
        [
            Node(RootNodeId, parentNodeId: FirstNodeId),
            Node(FirstNodeId, parentNodeId: RootNodeId)
        ]);

        var error = Assert.Single(report.Errors);
        Assert.Equal(HierarchyErrorCodes.HierarchyCycle, error.Code);
    }

    [Fact]
    public void Normalize_Siblings_OrdersBySortOrderThenNodeIdAndLeavesTombstonesUntouched()
    {
        var lowerNodeId = NodeIdFor(10);
        var higherNodeId = NodeIdFor(11);
        var tombstone = Node(ThirdNodeId, parentNodeId: RootNodeId, sortOrder: 99, isDeleted: true);
        var normalizedNodes = SiblingOrderNormalizer.Normalize(
        [
            Node(RootNodeId),
            Node(higherNodeId, parentNodeId: RootNodeId, sortOrder: 5),
            Node(lowerNodeId, parentNodeId: RootNodeId, sortOrder: 5),
            Node(FirstNodeId, parentNodeId: RootNodeId, sortOrder: -4),
            tombstone
        ]);

        Assert.Equal(0, Find(normalizedNodes, FirstNodeId).SortOrder);
        Assert.Equal(1, Find(normalizedNodes, lowerNodeId).SortOrder);
        Assert.Equal(2, Find(normalizedNodes, higherNodeId).SortOrder);
        Assert.Equal(99, Find(normalizedNodes, ThirdNodeId).SortOrder);
    }

    [Fact]
    public void Create_ExistingRoot_ReturnsRootAlreadyExistsWithoutGeneratingAnId()
    {
        var generator = new CountingIdentifierGenerator(ThirdNodeId);
        var service = new NodeMutationService(generator);

        var result = service.Create(
            [Node(RootNodeId)],
            new CreateNodeCommand(SnapshotId, null, "Zweite Root", null, 0),
            [RootNodeId]);

        Assert.False(result.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.RootAlreadyExists, result.Code);
        Assert.Equal(0, generator.NodeIdRequests);
    }

    [Fact]
    public void Create_ReusedGeneratedId_ReturnsNodeIdAlreadyUsed()
    {
        var generator = new CountingIdentifierGenerator(FirstNodeId);
        var service = new NodeMutationService(generator);

        var result = service.Create(
            [Node(RootNodeId)],
            new CreateNodeCommand(SnapshotId, RootNodeId, "Child", null, 0),
            [RootNodeId, FirstNodeId]);

        Assert.False(result.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.NodeIdAlreadyUsed, result.Code);
        Assert.Equal(1, generator.NodeIdRequests);
    }

    [Fact]
    public void Create_Child_NormalizesItsSiblingGroup()
    {
        var generator = new CountingIdentifierGenerator(ThirdNodeId);
        var service = new NodeMutationService(generator);

        var result = service.Create(
            [Node(RootNodeId), Node(FirstNodeId, RootNodeId, sortOrder: 9)],
            new CreateNodeCommand(SnapshotId, RootNodeId, "Neues Child", null, -1),
            [RootNodeId, FirstNodeId]);

        Assert.True(result.IsSuccess);
        Assert.Equal(ThirdNodeId, result.Value!.ChangedNode.NodeId);
        Assert.Equal(0, Find(result.Value.Nodes, ThirdNodeId).SortOrder);
        Assert.Equal(1, Find(result.Value.Nodes, FirstNodeId).SortOrder);
        Assert.Equal(1, generator.NodeIdRequests);
    }

    [Fact]
    public void Move_CycleCandidate_FailsWithoutChangingTheSourceNodes()
    {
        Node[] nodes =
        [
            Node(RootNodeId),
            Node(FirstNodeId, parentNodeId: RootNodeId)
        ];
        var service = new NodeMutationService(new CountingIdentifierGenerator(ThirdNodeId));

        var result = service.Move(nodes, new MoveNodeCommand(RootNodeId, FirstNodeId, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.HierarchyCycle, result.Code);
        Assert.Null(nodes[0].ParentNodeId);
        Assert.Equal(RootNodeId, nodes[1].ParentNodeId);
    }

    [Fact]
    public void Move_NormalizesBothAffectedSiblingGroups()
    {
        Node[] nodes =
        [
            Node(RootNodeId),
            Node(FirstNodeId, parentNodeId: RootNodeId, sortOrder: 9),
            Node(SecondNodeId, parentNodeId: RootNodeId, sortOrder: 5),
            Node(ThirdNodeId, parentNodeId: FirstNodeId, sortOrder: 7)
        ];
        var service = new NodeMutationService(new CountingIdentifierGenerator(NodeIdFor(20)));

        var result = service.Move(nodes, new MoveNodeCommand(SecondNodeId, FirstNodeId, -1));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, Find(result.Value!.Nodes, FirstNodeId).SortOrder);
        Assert.Equal(0, Find(result.Value.Nodes, SecondNodeId).SortOrder);
        Assert.Equal(1, Find(result.Value.Nodes, ThirdNodeId).SortOrder);
    }

    [Fact]
    public void Reorder_NormalizesGapsAndKeepsDeterministicOrder()
    {
        Node[] nodes =
        [
            Node(RootNodeId),
            Node(FirstNodeId, parentNodeId: RootNodeId, sortOrder: 8),
            Node(SecondNodeId, parentNodeId: RootNodeId, sortOrder: 4),
            Node(ThirdNodeId, parentNodeId: RootNodeId, sortOrder: 1)
        ];
        var service = new NodeMutationService(new CountingIdentifierGenerator(NodeIdFor(20)));

        var result = service.Reorder(nodes, new ReorderNodeCommand(FirstNodeId, 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, Find(result.Value!.Nodes, FirstNodeId).SortOrder);
        Assert.Equal(1, Find(result.Value.Nodes, ThirdNodeId).SortOrder);
        Assert.Equal(2, Find(result.Value.Nodes, SecondNodeId).SortOrder);
    }

    private static Node Find(IEnumerable<Node> nodes, NodeId nodeId) =>
        Assert.Single(nodes.Where(node => node.NodeId == nodeId));

    private static Node Node(
        NodeId nodeId,
        NodeId? parentNodeId = null,
        string title = "Titel",
        int sortOrder = 0,
        bool isDeleted = false) =>
        new(SnapshotId, nodeId, parentNodeId, title, null, sortOrder, isDeleted);

    private static NodeId NodeIdFor(int value) => new(new Guid(value, 0, 0, new byte[8]));

    private sealed class CountingIdentifierGenerator(NodeId nodeId) : IIdentifierGenerator
    {
        public int NodeIdRequests { get; private set; }

        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId()
        {
            NodeIdRequests++;
            return nodeId;
        }

        public ContentRevisionId CreateContentRevisionId() => throw new NotSupportedException();
    }
}
