using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Nodes;

[Trait("Category", "Unit")]
public sealed class NodeDeletionApplicationServiceTests
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("e7e29121-4607-40bd-a2c2-19d6b38503cc"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly NodeId ChildNodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000002"));
    private static readonly NodeId GrandchildNodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000003"));
    private static readonly AudienceId AudienceId = new("Developer");

    [Fact]
    public async Task PreviewAsync_DescribesSubtreeContentAndDependencyEffects()
    {
        var store = CreateOpenStore();
        store.Nodes.AddRange(
        [
            Node(RootNodeId),
            Node(ChildNodeId, RootNodeId),
            Node(GrandchildNodeId, ChildNodeId)
        ]);
        store.Contents.AddRange([Content(RootNodeId), Content(ChildNodeId), Content(GrandchildNodeId)]);
        store.Dependencies.AddRange(
        [
            Dependency(GrandchildNodeId, ChildNodeId),
            Dependency(RootNodeId, ChildNodeId)
        ]);
        var service = CreateService(store);

        var result = await service.PreviewAsync(TransactionId, ChildNodeId);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsRoot);
        Assert.Equal(1, result.Value.DirectChildCount);
        Assert.Equal(2, result.Value.SubtreeNodeCount);
        Assert.Equal(2, result.Value.ContentCount);
        Assert.Equal(1, result.Value.RemovedDependencyCount);
        Assert.Equal(1, result.Value.RetainedSourceDependencyCount);
        Assert.Equal(4, result.Value.ChangeVersion);
    }

    [Fact]
    public async Task PreviewAsync_AlreadyDeletedNodeIsRejected()
    {
        var store = CreateOpenStore();
        store.Nodes.Add(Node(ChildNodeId) with { IsDeleted = true });
        var service = CreateService(store);

        var result = await service.PreviewAsync(TransactionId, ChildNodeId);

        Assert.False(result.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.NodeNotFound, result.Code);
    }

    [Fact]
    public async Task PreviewAsync_MarksTheSingleRootForItsExplicitEmptyStateWarning()
    {
        var store = CreateOpenStore();
        store.Nodes.Add(Node(RootNodeId));
        var service = CreateService(store);

        var result = await service.PreviewAsync(TransactionId, RootNodeId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsRoot);
        Assert.Equal(1, result.Value.SubtreeNodeCount);
    }

    private static InMemoryKnowledgeStore CreateOpenStore()
    {
        var store = new InMemoryKnowledgeStore();
        store.Snapshots.Add(new Snapshot(SnapshotId, new SnapshotId(41), SnapshotState.Working, DateTimeOffset.UtcNow, null));
        store.Transactions[TransactionId] = new KnowledgeTransaction(
            TransactionId,
            new SnapshotId(41),
            SnapshotId,
            TransactionState.Open,
            ChangeVersion: 4,
            DateTimeOffset.UtcNow,
            null,
            "Löschprüfung",
            "Alice",
            "Web UI",
            null);
        return store;
    }

    private static NodeDeletionApplicationService CreateService(InMemoryKnowledgeStore store) =>
        new(
            new InMemoryWorkingSnapshotReadRepository(store),
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(SnapshotId, [], [], [], [])),
            new NodeMutationService(new FixedIdentifierGenerator()),
            TestPolicies.DefaultValidation);

    private static Node Node(NodeId nodeId, NodeId? parentNodeId = null) =>
        new(SnapshotId, nodeId, parentNodeId, "Node", null, 0, IsDeleted: false);

    private static NodeContent Content(NodeId nodeId) =>
        new(SnapshotId, nodeId, AudienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt", IsDeleted: false);

    private static ContentDependency Dependency(NodeId targetNodeId, NodeId sourceNodeId) =>
        new(SnapshotId, targetNodeId, AudienceId, sourceNodeId, AudienceId, new ContentRevisionId(Guid.NewGuid()));
}
