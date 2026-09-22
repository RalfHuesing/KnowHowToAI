using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeDeletionEditorTests : BunitContext
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("fb2db604-9af8-4b70-91f9-d196dd41e9ab"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("20000000-0000-0000-0000-000000000001"));
    private static readonly NodeId ChildNodeId = new(Guid.Parse("20000000-0000-0000-0000-000000000002"));
    private static readonly NodeId GrandchildNodeId = new(Guid.Parse("20000000-0000-0000-0000-000000000003"));

    public NodeDeletionEditorTests() =>
        JSInterop.SetupModule("./Web/Components/Shared/Dialogs/AppDialog.razor.js").Mode = JSRuntimeMode.Loose;

    [Fact]
    public async Task Deletion_RequiresVisibleImpactPreviewAndConfirmedSubtreeChoice()
    {
        var repository = AddServices(changeVersion: 0);
        NodeMutationResult? persisted = null;
        var cut = RenderEditor(result => persisted = result);

        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-node']").Click());

        Assert.Equal("Child", cut.Find("[data-testid='delete-node-target']").TextContent);
        Assert.Equal("1", cut.Find("[data-testid='delete-direct-children']").TextContent);
        Assert.Equal("2", cut.Find("[data-testid='delete-subtree-nodes']").TextContent);
        Assert.Equal("2", cut.Find("[data-testid='delete-contents']").TextContent);
        Assert.True(cut.Find("[data-testid='confirm-delete-node']").HasAttribute("disabled"));
        Assert.False(Find(repository.State.Nodes, ChildNodeId).IsDeleted);

        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-subtree']").Change(true));
        await cut.InvokeAsync(() => cut.Find("[data-testid='confirm-delete-node']").Click());
        var dialog = cut.FindComponent<ConfirmationDialog>();

        Assert.Contains("Child", dialog.Markup);
        await cut.InvokeAsync(() => dialog.Find(".confirmation-dialog__button--destructive").Click());

        Assert.NotNull(persisted);
        Assert.Equal(1, persisted.ChangeVersion);
        Assert.All(repository.State.Nodes.Where(node => node.NodeId != RootNodeId), node => Assert.True(node.IsDeleted));
    }

    [Fact]
    public async Task Deletion_WithStalePreviewShowsServerRejectionAndKeepsNode()
    {
        var repository = AddServices(changeVersion: 0);
        var service = Services.GetRequiredService<NodeMutationApplicationService>();
        var cut = RenderEditor();

        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-node']").Click());
        await service.UpdateAsync(TransactionId, new UpdateNodeRequest(ChildNodeId, "Anderer Client", null));
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-subtree']").Change(true));
        await cut.InvokeAsync(() => cut.Find("[data-testid='confirm-delete-node']").Click());
        var dialog = cut.FindComponent<ConfirmationDialog>();
        await cut.InvokeAsync(() => dialog.Find(".confirmation-dialog__button--destructive").Click());

        Assert.Contains(TransactionValidationErrorCodes.ChangeVersionConflict, cut.Markup);
        Assert.Contains("Löschprüfung neu", cut.Markup);
        Assert.False(Find(repository.State.Nodes, ChildNodeId).IsDeleted);
    }

    private InMemoryNodeMutationRepository AddServices(long changeVersion)
    {
        var nodes = new[] { Node(RootNodeId), Node(ChildNodeId, RootNodeId), Node(GrandchildNodeId, ChildNodeId) };
        var contents = new[] { Content(ChildNodeId), Content(GrandchildNodeId) };
        var repository = new InMemoryNodeMutationRepository(
            new WorkingNodeMutationState(SnapshotId, nodes, contents, [], nodes.Select(node => node.NodeId).ToArray()));
        var mutationService = TestNodeMutations.CreateService(repository);
        Services.AddSingleton(mutationService);

        var store = new InMemoryKnowledgeStore();
        store.Snapshots.Add(new Snapshot(SnapshotId, new SnapshotId(41), SnapshotState.Working, DateTimeOffset.UtcNow, null));
        store.Transactions[TransactionId] = new KnowledgeTransaction(
            TransactionId, new SnapshotId(41), SnapshotId, TransactionState.Open, changeVersion,
            DateTimeOffset.UtcNow, null, "Löschprüfung", "Alice", "Web UI", null);
        store.Nodes.AddRange(nodes);
        store.Contents.AddRange(contents);
        store.Dependencies.Add(new ContentDependency(
            SnapshotId,
            RootNodeId,
            new AudienceId("Developer"),
            ChildNodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.Parse("20000000-0000-0000-0000-000000000004"))));
        Services.AddSingleton<IWorkingSnapshotReadRepository>(new InMemoryWorkingSnapshotReadRepository(store));
        Services.AddSingleton(new NodeDeletionApplicationService(
            new InMemoryWorkingSnapshotReadRepository(store),
            repository,
            new NodeMutationService(new FixedIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        return repository;
    }

    private IRenderedComponent<NodeDeletionEditor> RenderEditor(Action<NodeMutationResult>? onSuccess = null) =>
        Render<NodeDeletionEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.TransactionId, TransactionId)
            .Add(component => component.OnMutationSucceeded, EventCallback.Factory.Create<NodeMutationResult>(this, onSuccess ?? (_ => { }))));

    private static Node Node(NodeId nodeId, NodeId? parentNodeId = null) =>
        new(SnapshotId, nodeId, parentNodeId, nodeId == ChildNodeId ? "Child" : "Root", null, 0, false);

    private static NodeContent Content(NodeId nodeId) =>
        new(SnapshotId, nodeId, new AudienceId("Developer"), new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt", false);

    private static Node Find(IEnumerable<Node> nodes, NodeId nodeId) =>
        Assert.Single(nodes.Where(node => node.NodeId == nodeId));

    private static NodeDetailsViewModel ViewModel() => new(
        ChildNodeId.Value,
        RootNodeId.Value,
        "Child",
        null,
        0,
        "Developer",
        "Developer",
        false,
        "Explicit",
        "Current",
        null,
        null,
        null,
        [],
        ChangeVersion: 0);
}
