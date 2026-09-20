using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Server.Mcp.Tools.Mutations;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der Struktur-Tools (create_node, update_node, move_node,
/// reorder_node, delete_node): dünne Delegation an den NodeMutationApplicationService
/// mit protokollkonformer Error-Struktur, ID-Round-Trip und atomarer Subtree-Löschung.
/// Keine SQL- oder Server-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpNodeMutationToolsTests
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = NodeIdFor(1);
    private static readonly NodeId FirstChildNodeId = NodeIdFor(2);
    private static readonly NodeId SecondChildNodeId = NodeIdFor(3);
    private static readonly NodeId GrandchildNodeId = NodeIdFor(4);
    private static readonly NodeId UnknownNodeId = NodeIdFor(99);
    private static readonly NodeId GeneratedNodeId = NodeIdFor(10);

    [Fact]
    public async Task CreateNode_AsRoot_MapsNodeDataWithRoundTripCapableIds()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateNode(TransactionId.ToString(), "Hauptkapitel");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(GeneratedNodeId.ToString(), envelope.Data!.NodeId);
        Assert.Null(envelope.Data.ParentNodeId);
        Assert.Equal("Hauptkapitel", envelope.Data.Title);
        Assert.Equal(SnapshotId.ToString(), envelope.Data.SnapshotId);
        Assert.Equal(1, envelope.Data.ChangeVersion);
        Assert.Contains(GeneratedNodeId.ToString(), envelope.Data.AffectedNodeIds);
        Assert.Null(envelope.Message);
        Assert.Null(envelope.Details);
    }

    [Fact]
    public async Task CreateNode_UnderParent_MapsParentNodeIdForRoundTrip()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.CreateNode(
            TransactionId.ToString(), "Drittes Kind", parentNodeId: RootNodeId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal(RootNodeId.ToString(), envelope.Data!.ParentNodeId);
    }

    [Fact]
    public async Task CreateNode_SecondRoot_IsRejectedWithStableRootAlreadyExists()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.CreateNode(TransactionId.ToString(), "Zweite Wurzel");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.RootAlreadyExists, envelope.Code);
        Assert.Null(envelope.Data);
        Assert.NotEmpty(envelope.Message!);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("17")]
    public async Task CreateNode_MalformedParentNodeId_IsRejectedAsInvalidNodeId(string rawParentNodeId)
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.CreateNode(
            TransactionId.ToString(), "Kind", parentNodeId: rawParentNodeId);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidNodeId, envelope.Code);
        Assert.Equal(rawParentNodeId, envelope.Details![NavigationErrorCodes.ParentNodeIdDetail]);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task CreateNode_MalformedTransactionId_IsRejectedAsTransactionNotFound(string rawTransactionId)
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.CreateNode(rawTransactionId, "Kind");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, envelope.Code);
        Assert.Equal(rawTransactionId, envelope.Details![TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task CreateNode_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var repository = EmptyState();
        repository.Rejection = new DomainError(
            errorCode,
            "Die Transaction ist nicht offen.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.TransactionIdDetail] = TransactionId.ToString()
            });
        var tools = CreateTools(repository);

        var envelope = await tools.CreateNode(TransactionId.ToString(), "Kind");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(errorCode, envelope.Code);
        Assert.Equal(TransactionId.ToString(), envelope.Details![TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task CreateNode_StaleChangeVersion_IsRejectedWithExpectedAndActualDetails()
    {
        var repository = StateWithRootAndChildren();
        var tools = CreateTools(repository);

        var concurrentChange = await tools.UpdateNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), "Aktualisiert", expectedChangeVersion: 0);
        var stale = await tools.CreateNode(
            TransactionId.ToString(), "Veraltete Node", expectedChangeVersion: 0);

        Assert.True(concurrentChange.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details![TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal(1, repository.ChangeVersion);
        Assert.DoesNotContain(repository.State.Nodes, node => node.NodeId == GeneratedNodeId);
    }

    [Fact]
    public async Task CreateNode_WithContent_CreatesNodeAndContentAndMergesData()
    {
        var nodeRepository = StateWithRootAndChildren();
        var contentRepository = ContentStateForCreatedNode();
        var tools = CreateTools(nodeRepository, contentRepository);

        var envelope = await tools.CreateNode(
            TransactionId.ToString(), "Neues Kapitel",
            parentNodeId: RootNodeId.ToString(), sortOrder: 99,
            contentMd: "Kapitelinhalt ohne Überschrift.", roleId: "Developer");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(GeneratedNodeId.ToString(), envelope.Data!.NodeId);
        Assert.Equal("Neues Kapitel", envelope.Data.Title);
        Assert.Equal("Developer", envelope.Data.RoleId);
        Assert.Equal(ContentRevisionIdFor(10).ToString(), envelope.Data.ContentRevisionId);
        Assert.Equal("Independent", envelope.Data.ContentMode);
        Assert.Equal("Current", envelope.Data.Freshness);
        Assert.Equal(1, contentRepository.ChangeVersion);
    }

    [Fact]
    public async Task CreateNode_WithContentButMissingRoleId_IsRejectedWithoutCreatingNode()
    {
        var nodeRepository = EmptyState();
        var tools = CreateTools(nodeRepository, ContentStateForCreatedNode());

        var envelope = await tools.CreateNode(TransactionId.ToString(), "Kapitel", contentMd: "Inhalt.");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleMutationErrorCodes.RoleIdRequired, envelope.Code);
        Assert.Equal(0, nodeRepository.ChangeVersion);
    }

    [Fact]
    public async Task CreateNode_WithInvalidContentMode_IsRejectedWithoutCreatingNode()
    {
        var nodeRepository = EmptyState();
        var tools = CreateTools(nodeRepository, ContentStateForCreatedNode());

        var envelope = await tools.CreateNode(
            TransactionId.ToString(), "Kapitel", contentMd: "Inhalt.", roleId: "Developer", contentMode: "Bogus");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(DependencyErrorCodes.InvalidDependency, envelope.Code);
        Assert.Equal(0, nodeRepository.ChangeVersion);
    }

    [Fact]
    public async Task CreateNode_WithoutContent_IgnoresContentParametersAndOmitsContentFields()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateNode(TransactionId.ToString(), "Hauptkapitel", roleId: "Developer");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(GeneratedNodeId.ToString(), envelope.Data!.NodeId);
        Assert.Null(envelope.Data.RoleId);
        Assert.Null(envelope.Data.ContentRevisionId);
        Assert.Null(envelope.Data.ContentMode);
        Assert.Null(envelope.Data.Freshness);
    }

    [Fact]
    public async Task UpdateNode_ChangesTitleAndDescription()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.UpdateNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), "Neuer Titel", "Neue Beschreibung");

        Assert.True(envelope.IsSuccess);
        Assert.Equal("Neuer Titel", envelope.Data!.Title);
        Assert.Equal(FirstChildNodeId.ToString(), envelope.Data.NodeId);
        Assert.Equal(1, envelope.Data.ChangeVersion);
    }

    [Fact]
    public async Task UpdateNode_WhitespaceTitle_IsRejectedWithStableTitleRequired()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.UpdateNode(TransactionId.ToString(), FirstChildNodeId.ToString(), "   ");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.TitleRequired, envelope.Code);
    }

    [Fact]
    public async Task UpdateNode_UnknownNode_ReturnsStableNodeNotFoundWithRawId()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.UpdateNode(TransactionId.ToString(), UnknownNodeId.ToString(), "Titel");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.NodeNotFound, envelope.Code);
        Assert.Equal(UnknownNodeId.ToString(), envelope.Details![HierarchyErrorCodes.NodeIdDetail]);
    }

    [Fact]
    public async Task MoveNode_UnderSibling_MapsMovedNodeAndAffectedDescendants()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.MoveNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), sortOrder: 0, parentNodeId: SecondChildNodeId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal(SecondChildNodeId.ToString(), envelope.Data!.ParentNodeId);
        Assert.Contains(GrandchildNodeId.ToString(), envelope.Data.AffectedNodeIds);
    }

    [Fact]
    public async Task MoveNode_SelfParent_IsRejectedWithStableSelfParentNotAllowed()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.MoveNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), sortOrder: 0, parentNodeId: FirstChildNodeId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.SelfParentNotAllowed, envelope.Code);
    }

    [Fact]
    public async Task MoveNode_StaleChangeVersion_IsRejectedWithoutChangingTheWorkingTree()
    {
        var repository = StateWithRootAndChildren();
        var tools = CreateTools(repository);

        var concurrentChange = await tools.UpdateNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), "Aktualisiert", expectedChangeVersion: 0);
        var stale = await tools.MoveNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), sortOrder: 0, expectedChangeVersion: 0);

        Assert.True(concurrentChange.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details![TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal(RootNodeId, repository.State.Nodes.Single(node => node.NodeId == FirstChildNodeId).ParentNodeId);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task ReorderNode_ChangesSortOrderWithinExistingParent()
    {
        var repository = StateWithRootAndChildren();
        var tools = CreateTools(repository);

        var envelope = await tools.ReorderNode(TransactionId.ToString(), SecondChildNodeId.ToString(), 8);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(1, envelope.Data!.ChangeVersion);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task ReorderNode_StaleChangeVersion_IsRejectedWithoutChangingTheWorkingTree()
    {
        var repository = StateWithRootAndChildren();
        var tools = CreateTools(repository);

        var concurrentChange = await tools.UpdateNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), "Aktualisiert", expectedChangeVersion: 0);
        var stale = await tools.ReorderNode(
            TransactionId.ToString(), SecondChildNodeId.ToString(), 8, expectedChangeVersion: 0);

        Assert.True(concurrentChange.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details![TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal(0, repository.State.Nodes.Single(node => node.NodeId == SecondChildNodeId).SortOrder);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task DeleteNode_WithActiveChildren_IsRejectedWithoutDeleteSubtree()
    {
        var tools = CreateTools(StateWithRootAndChildren());

        var envelope = await tools.DeleteNode(TransactionId.ToString(), RootNodeId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(NodeDeletionErrorCodes.NodeHasChildren, envelope.Code);
        Assert.Equal("2", envelope.Details![NodeDeletionErrorCodes.ActiveChildCountDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task DeleteNode_WithSubtree_TombstonesNodesAndTheirContentsAtomically()
    {
        var repository = new InMemoryNodeMutationRepository(new WorkingNodeMutationState(
            SnapshotId,
            [Node(RootNodeId), Node(FirstChildNodeId, RootNodeId), Node(GrandchildNodeId, FirstChildNodeId)],
            [Content(FirstChildNodeId), Content(GrandchildNodeId)],
            [],
            [RootNodeId, FirstChildNodeId, GrandchildNodeId]));
        var tools = CreateTools(repository);

        var envelope = await tools.DeleteNode(
            TransactionId.ToString(), FirstChildNodeId.ToString(), deleteSubtree: true);

        Assert.True(envelope.IsSuccess);
        Assert.Contains(GrandchildNodeId.ToString(), envelope.Data!.AffectedNodeIds);
        Assert.All(repository.State.Nodes.Where(node => node.NodeId != RootNodeId), node => Assert.True(node.IsDeleted));
        Assert.All(repository.State.Contents, content => Assert.True(content.IsDeleted));
    }

    private static NodeMutationTools CreateTools(
        InMemoryNodeMutationRepository repository,
        InMemoryContentMutationRepository? contentRepository = null) =>
        new(
            new NodeMutationApplicationService(
                repository,
                new NodeMutationService(new FixedIdentifierGenerator()),
                new ValidationPolicy
                {
                    ContentSizeWarningBytes = 4096,
                    ChildCountWarning = 2,
                    HierarchyDepthWarning = 8,
                    PossibleEmbeddedHeadingWarning = true
                }),
            new ContentMutationApplicationService(
                contentRepository ?? new InMemoryContentMutationRepository(new WorkingContentMutationState(SnapshotId, [], [], [], [])),
                new ContentMutationService(new ContentRevisionService(new RevisionIdentifierGenerator())),
                new ValidationPolicy
                {
                    ContentSizeWarningBytes = 4096,
                    ChildCountWarning = 2,
                    HierarchyDepthWarning = 8,
                    PossibleEmbeddedHeadingWarning = true
                }));

    private static InMemoryContentMutationRepository ContentStateForCreatedNode() =>
        new(new WorkingContentMutationState(
            SnapshotId,
            [Node(GeneratedNodeId)],
            [Role(new RoleId("Developer"))],
            [],
            []));

    private static InMemoryNodeMutationRepository EmptyState() => new(new WorkingNodeMutationState(SnapshotId, [], [], [], []));

    private static InMemoryNodeMutationRepository StateWithRootAndChildren() => new(new WorkingNodeMutationState(
        SnapshotId,
        [Node(RootNodeId), Node(FirstChildNodeId, RootNodeId), Node(SecondChildNodeId, RootNodeId), Node(GrandchildNodeId, FirstChildNodeId)],
        [],
        [],
        [RootNodeId, FirstChildNodeId, SecondChildNodeId, GrandchildNodeId]));

    private static Node Node(NodeId nodeId, NodeId? parentNodeId = null) =>
        new(SnapshotId, nodeId, parentNodeId, "Titel", null, 0, IsDeleted: false);

    private static Role Role(RoleId roleId) =>
        new(SnapshotId, roleId, roleId.Value, null, IsDeleted: false);

    private static NodeContent Content(NodeId nodeId) =>
        new(
            SnapshotId,
            nodeId,
            new RoleId("Developer"),
            new ContentRevisionId(Guid.Parse("b4e0e04a-2dce-4b5e-8b34-4bd2b9f0e020")),
            ContentMode.Independent,
            "Inhalt",
            IsDeleted: false);

    private static NodeId NodeIdFor(int value) => new(new Guid(value, 0, 0, new byte[8]));

    private static ContentRevisionId ContentRevisionIdFor(int value) => new(new Guid(value, 0, 0, new byte[8]));

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => GeneratedNodeId;

        public ContentRevisionId CreateContentRevisionId() => throw new NotSupportedException();
    }

    private sealed class RevisionIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId() => ContentRevisionIdFor(10);
    }
}
