using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Tests.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Nodes;

[Trait("Category", "Unit")]
public sealed class NodeMutationApplicationServiceTests
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = NodeIdFor(1);
    private static readonly NodeId FirstChildNodeId = NodeIdFor(2);
    private static readonly NodeId SecondChildNodeId = NodeIdFor(3);
    private static readonly NodeId GrandchildNodeId = NodeIdFor(4);

    [Fact]
    public async Task CreateAsync_PersistsGlobalStructureAndReturnsQualityWarning()
    {
        var repository = new InMemoryNodeMutationRepository(State(Node(RootNodeId), Node(FirstChildNodeId, RootNodeId)));
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.CreateAsync(
            TransactionId,
            new CreateNodeRequest(RootNodeId, "Zweites Kind", null, 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(SnapshotId, result.Value!.SnapshotId);
        Assert.Equal(1, result.Value.ChangeVersion);
        Assert.True(result.Value.AppliesToAllRoles);
        Assert.Contains(SecondChildNodeId, result.Value.AffectedNodeIds);
        Assert.Contains(result.Warnings, warning => warning.Code == QualityWarningCodes.TooManyChildren);
        Assert.Equal(3, repository.State.Nodes.Count(node => !node.IsDeleted));
    }

    [Fact]
    public async Task UpdateAsync_ChangesOnlyGlobalNavigationMetadata()
    {
        var repository = new InMemoryNodeMutationRepository(State(Node(RootNodeId), Node(FirstChildNodeId, RootNodeId)));
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.UpdateAsync(TransactionId, FirstChildNodeId, "Neuer Titel", "Neue Beschreibung");

        Assert.True(result.IsSuccess);
        Assert.Equal("Neuer Titel", result.Value!.Node.Title);
        Assert.Equal("Neue Beschreibung", result.Value.Node.Description);
        Assert.True(result.Value.AppliesToAllRoles);
        Assert.Equal("Neuer Titel", Find(repository.State.Nodes, FirstChildNodeId).Title);
    }

    [Fact]
    public async Task MoveAndReorderAsync_NormalizeAffectedSiblingGroupsAndExposeDescendantImpact()
    {
        var repository = new InMemoryNodeMutationRepository(State(
            Node(RootNodeId),
            Node(FirstChildNodeId, RootNodeId, sortOrder: 4),
            Node(SecondChildNodeId, RootNodeId, sortOrder: 8),
            Node(GrandchildNodeId, FirstChildNodeId)));
        var service = CreateService(repository, NodeIdFor(9));

        var reorder = await service.ReorderAsync(TransactionId, SecondChildNodeId, 0);
        var move = await service.MoveAsync(TransactionId, FirstChildNodeId, SecondChildNodeId, 0);

        Assert.True(reorder.IsSuccess);
        Assert.Contains(GrandchildNodeId, move.Value!.AffectedNodeIds);
        Assert.Equal(2, move.Value!.ChangeVersion);
        Assert.Equal(0, Find(repository.State.Nodes, SecondChildNodeId).SortOrder);
    }

    [Fact]
    public async Task DeleteAsync_RequiresExplicitSubtreeAndTombstonesNodesAndContentsAtomically()
    {
        var childContent = Content(FirstChildNodeId);
        var grandchildContent = Content(GrandchildNodeId);
        var repository = new InMemoryNodeMutationRepository(State(
            [Node(RootNodeId), Node(FirstChildNodeId, RootNodeId), Node(GrandchildNodeId, FirstChildNodeId)],
            [childContent, grandchildContent]));
        var service = CreateService(repository, SecondChildNodeId);

        var rejected = await service.DeleteAsync(TransactionId, FirstChildNodeId, deleteSubtree: false);

        Assert.False(rejected.IsSuccess);
        Assert.Equal(NodeDeletionErrorCodes.NodeHasChildren, rejected.Code);
        Assert.Equal(0, repository.ChangeVersion);

        var deleted = await service.DeleteAsync(TransactionId, FirstChildNodeId, deleteSubtree: true);

        Assert.True(deleted.IsSuccess);
        Assert.Equal(1, deleted.Value!.ChangeVersion);
        Assert.Contains(FirstChildNodeId, deleted.Value.AffectedNodeIds);
        Assert.Contains(GrandchildNodeId, deleted.Value.AffectedNodeIds);
        Assert.All(repository.State.Nodes.Where(node => node.NodeId != RootNodeId), node => Assert.True(node.IsDeleted));
        Assert.All(repository.State.Contents, content => Assert.True(content.IsDeleted));
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task CreateAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryNodeMutationRepository(State(Node(RootNodeId))) { Rejection = rejection };
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.CreateAsync(TransactionId, new CreateNodeRequest(RootNodeId, "Child", null, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Single(repository.State.Nodes);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task UpdateAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryNodeMutationRepository(State(Node(RootNodeId))) { Rejection = rejection };
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.UpdateAsync(TransactionId, RootNodeId, "Neuer Titel", "Neue Beschreibung");

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal("Titel", repository.State.Nodes.Single().Title);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task MoveAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryNodeMutationRepository(State(Node(RootNodeId), Node(FirstChildNodeId, RootNodeId))) { Rejection = rejection };
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.MoveAsync(TransactionId, FirstChildNodeId, null, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal(RootNodeId, Find(repository.State.Nodes, FirstChildNodeId).ParentNodeId);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task ReorderAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryNodeMutationRepository(State(Node(RootNodeId), Node(FirstChildNodeId, RootNodeId, sortOrder: 0))) { Rejection = rejection };
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.ReorderAsync(TransactionId, FirstChildNodeId, 5);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal(0, Find(repository.State.Nodes, FirstChildNodeId).SortOrder);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task DeleteAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryNodeMutationRepository(State(Node(RootNodeId), Node(FirstChildNodeId, RootNodeId))) { Rejection = rejection };
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.DeleteAsync(TransactionId, FirstChildNodeId, deleteSubtree: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.False(Find(repository.State.Nodes, FirstChildNodeId).IsDeleted);
    }

    [Fact]
    public async Task UpdateAsync_IdenticalValues_DoesNotIncrementChangeVersion()
    {
        var node = Node(RootNodeId, sortOrder: 0) with { Title = "Titel", Description = "Beschreibung" };
        var repository = new InMemoryNodeMutationRepository(State(node));
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.UpdateAsync(TransactionId, RootNodeId, "Titel", "Beschreibung");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.ChangeVersion);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task MoveAsync_IdenticalParentAndSortOrder_DoesNotIncrementChangeVersion()
    {
        var root = Node(RootNodeId);
        var child = Node(FirstChildNodeId, RootNodeId, sortOrder: 0);
        var repository = new InMemoryNodeMutationRepository(State(root, child));
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.MoveAsync(TransactionId, FirstChildNodeId, RootNodeId, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.ChangeVersion);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task ReorderAsync_IdenticalSortOrder_DoesNotIncrementChangeVersion()
    {
        var root = Node(RootNodeId);
        var child = Node(FirstChildNodeId, RootNodeId, sortOrder: 0);
        var repository = new InMemoryNodeMutationRepository(State(root, child));
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.ReorderAsync(TransactionId, FirstChildNodeId, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.ChangeVersion);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateAsync_ChangedValues_IncrementsChangeVersionExactlyOnce()
    {
        var node = Node(RootNodeId, sortOrder: 0) with { Title = "Alt", Description = null };
        var repository = new InMemoryNodeMutationRepository(State(node));
        var service = CreateService(repository, SecondChildNodeId);

        var result = await service.UpdateAsync(TransactionId, RootNodeId, "Neu", null);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ChangeVersion);
        Assert.Equal(1, repository.ChangeVersion);
    }

    private static NodeMutationApplicationService CreateService(
        InMemoryNodeMutationRepository repository,
        NodeId generatedNodeId) =>
        new(
            repository,
            new NodeMutationService(new FixedIdentifierGenerator { FixedNodeId = generatedNodeId }),
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 2,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            });

    private static WorkingNodeMutationState State(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<NodeContent>? contents = null,
        IReadOnlyList<ContentDependency>? dependencies = null) =>
        new(SnapshotId, nodes, contents ?? [], dependencies ?? [], nodes.Select(node => node.NodeId).ToArray());

    private static WorkingNodeMutationState State(params Node[] nodes) => State(nodes, [], []);

    private static Node Node(NodeId nodeId, NodeId? parentNodeId = null, int sortOrder = 0) =>
        new(SnapshotId, nodeId, parentNodeId, "Titel", null, sortOrder, IsDeleted: false);

    private static NodeContent Content(NodeId nodeId) =>
        new(
            SnapshotId,
            nodeId,
            new RoleId("Developer"),
            new ContentRevisionId(Guid.Parse("b4e0e04a-2dce-4b5e-8b34-4bd2b9f0e020")),
            ContentMode.Independent,
            "Content",
            IsDeleted: false);

    private static Node Find(IEnumerable<Node> nodes, NodeId nodeId) =>
        Assert.Single(nodes.Where(node => node.NodeId == nodeId));

    private static NodeId NodeIdFor(int value) => new(new Guid(value, 0, 0, new byte[8]));
}
