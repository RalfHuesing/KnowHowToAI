using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
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

    [Theory]
    [MemberData(nameof(CycleLengths))]
    public void Validate_CycleOfEveryGeneratedLength_ReturnsHierarchyCycle(int cycleLength)
    {
        var nodes = Enumerable.Range(1, cycleLength)
            .Select(index => Node(
                NodeIdFor(index),
                parentNodeId: NodeIdFor(index == cycleLength ? 1 : index + 1)))
            .ToArray();

        var report = HierarchyValidator.Validate(nodes);

        Assert.Contains(report.Errors, error => error.Code == HierarchyErrorCodes.HierarchyCycle);
    }

    [Theory]
    [MemberData(nameof(SiblingOrderCases))]
    public void Normalize_EveryGeneratedSiblingOrder_UsesNodeIdAsStableTieBreaker(
        Node[] siblings,
        NodeId[] expectedNodeOrder)
    {
        var normalized = SiblingOrderNormalizer.Normalize(siblings);

        var orderedSiblings = normalized
            .Where(node => node.ParentNodeId == RootNodeId)
            .OrderBy(node => node.SortOrder)
            .ToArray();

        Assert.Equal(expectedNodeOrder, orderedSiblings.Select(node => node.NodeId));
        Assert.Equal(Enumerable.Range(0, expectedNodeOrder.Length), orderedSiblings.Select(node => node.SortOrder));
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
    public void Normalize_SiblingsFromDifferentSnapshots_NormalizesEachSnapshotIndependently()
    {
        var otherSnapshotId = new SnapshotId(43);
        var normalizedNodes = SiblingOrderNormalizer.Normalize(
        [
            Node(RootNodeId, sortOrder: 7),
            new Node(otherSnapshotId, FirstNodeId, null, "Historische Root", null, 9, IsDeleted: false)
        ]);

        Assert.Equal(0, Assert.Single(normalizedNodes.Where(node => node.SnapshotId == SnapshotId)).SortOrder);
        Assert.Equal(0, Assert.Single(normalizedNodes.Where(node => node.SnapshotId == otherSnapshotId)).SortOrder);
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
    public void Create_Child_LeavesUnrelatedSiblingGroupsUntouched()
    {
        var generator = new CountingIdentifierGenerator(NodeIdFor(20));
        var service = new NodeMutationService(generator);
        Node[] nodes =
        [
            Node(RootNodeId),
            Node(FirstNodeId, RootNodeId, sortOrder: 4),
            Node(SecondNodeId, FirstNodeId, sortOrder: 8)
        ];

        var result = service.Create(
            nodes,
            new CreateNodeCommand(SnapshotId, RootNodeId, "Neues Child", null, 0),
            nodes.Select(node => node.NodeId));

        Assert.True(result.IsSuccess);
        Assert.Equal(8, Find(result.Value!.Nodes, SecondNodeId).SortOrder);
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
    public void Move_RootWithoutChangingItsParent_RemainsValid()
    {
        var service = new NodeMutationService(new CountingIdentifierGenerator(ThirdNodeId));

        var result = service.Move([Node(RootNodeId)], new MoveNodeCommand(RootNodeId, null, 0));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ChangedNode.ParentNodeId);
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

    [Fact]
    public void Delete_NodeWithActiveChildrenWithoutSubtreeFlag_ReturnsErrorWithoutChangingTheSourceNodes()
    {
        Node[] nodes = [Node(RootNodeId), Node(FirstNodeId, RootNodeId)];
        var service = new NodeMutationService(new CountingIdentifierGenerator(ThirdNodeId));

        var result = service.Delete(nodes, [], [], new DeleteNodeCommand(RootNodeId, DeleteSubtree: false));

        Assert.False(result.IsSuccess);
        Assert.Equal(NodeDeletionErrorCodes.NodeHasActiveChildren, result.Code);
        Assert.Equal("1", result.Details[NodeDeletionErrorCodes.ActiveChildCountDetail]);
        Assert.All(nodes, node => Assert.False(node.IsDeleted));
    }

    [Fact]
    public void Delete_Subtree_TombstonesAllActiveNodesAndContentsInTheSameSnapshot()
    {
        var otherSnapshot = new SnapshotId(43);
        Node[] nodes =
        [
            Node(RootNodeId),
            Node(FirstNodeId, RootNodeId),
            Node(SecondNodeId, FirstNodeId),
            Node(ThirdNodeId, RootNodeId)
        ];
        NodeContent[] contents =
        [
            Content(SnapshotId, RootNodeId, "Root"),
            Content(SnapshotId, FirstNodeId, "Child"),
            Content(SnapshotId, SecondNodeId, "Grandchild"),
            Content(SnapshotId, ThirdNodeId, "Other root"),
            Content(otherSnapshot, RootNodeId, "Historical")
        ];
        var service = new NodeMutationService(new CountingIdentifierGenerator(NodeIdFor(20)));

        var result = service.Delete(nodes, contents, [], new DeleteNodeCommand(FirstNodeId, DeleteSubtree: true));

        Assert.True(result.IsSuccess);
        Assert.False(Find(result.Value!.Nodes, RootNodeId).IsDeleted);
        Assert.True(Find(result.Value.Nodes, FirstNodeId).IsDeleted);
        Assert.True(Find(result.Value.Nodes, SecondNodeId).IsDeleted);
        Assert.False(Find(result.Value.Nodes, ThirdNodeId).IsDeleted);
        Assert.False(FindContent(result.Value.Contents, SnapshotId, RootNodeId).IsDeleted);
        Assert.True(FindContent(result.Value.Contents, SnapshotId, FirstNodeId).IsDeleted);
        Assert.True(FindContent(result.Value.Contents, SnapshotId, SecondNodeId).IsDeleted);
        Assert.False(FindContent(result.Value.Contents, SnapshotId, ThirdNodeId).IsDeleted);
        Assert.False(FindContent(result.Value.Contents, otherSnapshot, RootNodeId).IsDeleted);
    }

    [Fact]
    public void Delete_Subtree_DeletesTargetDependenciesAndPreservesSourceProvenanceAsStale()
    {
        var sourceContent = Content(SnapshotId, FirstNodeId, "Quelle");
        var derivedContent = Content(SnapshotId, SecondNodeId, "Abgeleitet") with
        {
            RoleId = new RoleId("EndUser"),
            ContentMode = ContentMode.Derived
        };
        var dependency = new ContentDependency(
            SnapshotId,
            derivedContent.NodeId,
            derivedContent.RoleId,
            sourceContent.NodeId,
            sourceContent.RoleId,
            sourceContent.ContentRevisionId);
        var nodes = new[]
        {
            Node(RootNodeId),
            Node(FirstNodeId, RootNodeId),
            Node(SecondNodeId, RootNodeId)
        };
        var service = new NodeMutationService(new CountingIdentifierGenerator(NodeIdFor(20)));

        var deletingTarget = service.Delete(
            nodes,
            [sourceContent, derivedContent],
            [dependency],
            new DeleteNodeCommand(SecondNodeId, DeleteSubtree: false));

        Assert.True(deletingTarget.IsSuccess);
        Assert.Empty(deletingTarget.Value!.Dependencies);
        Assert.True(DependencyValidator.ValidateSnapshot(
            deletingTarget.Value.Contents,
            deletingTarget.Value.Dependencies).IsValid);

        var deletingSource = service.Delete(
            nodes,
            [sourceContent, derivedContent],
            [dependency],
            new DeleteNodeCommand(FirstNodeId, DeleteSubtree: false));

        Assert.True(deletingSource.IsSuccess);
        Assert.Equal([dependency], deletingSource.Value!.Dependencies);
        Assert.True(DependencyValidator.ValidateSnapshot(
            deletingSource.Value.Contents,
            deletingSource.Value.Dependencies).IsValid);
        Assert.Equal(
            Freshness.Stale,
            FreshnessEvaluator.Evaluate(derivedContent, deletingSource.Value.Contents, deletingSource.Value.Dependencies));
    }

    private static Node Find(IEnumerable<Node> nodes, NodeId nodeId) =>
        Assert.Single(nodes.Where(node => node.NodeId == nodeId));

    private static NodeContent Content(SnapshotId snapshotId, NodeId nodeId, string content) =>
        new(
            snapshotId,
            nodeId,
            new RoleId("Developer"),
            new ContentRevisionId(Guid.Parse("4a2c9f4a-0a77-44be-8f98-f403444d3e9f")),
            ContentMode.Independent,
            content,
            IsDeleted: false);

    private static NodeContent FindContent(IEnumerable<NodeContent> contents, SnapshotId snapshotId, NodeId nodeId) =>
        Assert.Single(contents.Where(content => content.SnapshotId == snapshotId && content.NodeId == nodeId));

    private static Node Node(
        NodeId nodeId,
        NodeId? parentNodeId = null,
        string title = "Titel",
        int sortOrder = 0,
        bool isDeleted = false) =>
        new(SnapshotId, nodeId, parentNodeId, title, null, sortOrder, isDeleted);

    private static NodeId NodeIdFor(int value) => new(new Guid(value, 0, 0, new byte[8]));

    public static IEnumerable<object[]> CycleLengths()
    {
        for (var cycleLength = 2; cycleLength <= 8; cycleLength++)
            yield return [cycleLength];
    }

    public static IEnumerable<object[]> SiblingOrderCases()
    {
        var equalOrderExpectedNodeOrder = new[] { FirstNodeId, SecondNodeId, ThirdNodeId };
        yield return
        [
            new[]
            {
                Node(ThirdNodeId, RootNodeId, sortOrder: 5),
                Node(FirstNodeId, RootNodeId, sortOrder: 5),
                Node(SecondNodeId, RootNodeId, sortOrder: 5)
            },
            equalOrderExpectedNodeOrder
        ];
        yield return
        [
            new[]
            {
                Node(SecondNodeId, RootNodeId, sortOrder: 0),
                Node(ThirdNodeId, RootNodeId, sortOrder: -2),
                Node(FirstNodeId, RootNodeId, sortOrder: -2)
            },
            new[] { FirstNodeId, ThirdNodeId, SecondNodeId }
        ];
    }

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
