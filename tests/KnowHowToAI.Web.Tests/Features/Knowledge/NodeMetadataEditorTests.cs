using Bunit;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeMetadataEditorTests : BunitContext
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly NodeId ChildNodeId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public void NodeMetadataEditor_WithoutTransaction_ExplainsWhyMutationIsUnavailable()
    {
        AddService(State(Node(RootNodeId)));
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel()));

        Assert.NotNull(cut.Find("[data-testid='node-metadata-readonly']"));
        Assert.Empty(cut.FindAll("[data-testid='save-node-metadata']"));
    }

    [Fact]
    public async Task NodeMetadataEditor_UpdatePersistsServerAcceptedMetadata()
    {
        var repository = AddService(State(Node(RootNodeId)));
        NodeMutationResult? persisted = null;
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.TransactionId, TransactionId)
            .Add(component => component.ExpectedChangeVersion, 0L)
            .Add(component => component.OnMutationSucceeded, EventCallback.Factory.Create<NodeMutationResult>(this, result => persisted = result)));

        await cut.InvokeAsync(() => cut.Find("[data-testid='edit-node-metadata']").Click());
        Assert.False(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-title']").Change("Aktualisierter Titel"));
        Assert.True(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-description']").Change("Neue Beschreibung"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-node-metadata']").Click());

        Assert.NotNull(persisted);
        Assert.False(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        Assert.Equal("Aktualisierter Titel", repository.State.Nodes.Single().Title);
        Assert.Equal("Neue Beschreibung", repository.State.Nodes.Single().Description);
        Assert.Equal(1, persisted.ChangeVersion);
    }

    [Fact]
    public async Task NodeMetadataEditor_CreateChildUsesSelectedNodeAsParentAndRefreshSignal()
    {
        var repository = AddService(State(Node(RootNodeId)));
        NodeMutationResult? persisted = null;
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.TransactionId, TransactionId)
            .Add(component => component.ExpectedChangeVersion, 0L)
            .Add(component => component.OnMutationSucceeded, EventCallback.Factory.Create<NodeMutationResult>(this, result => persisted = result)));

        await cut.InvokeAsync(() => cut.Find("[data-testid='create-child-node']").Click());
        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-title']").Change("Child"));
        Assert.True(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-node-metadata']").Click());

        Assert.NotNull(persisted);
        Assert.False(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        var child = Assert.Single(repository.State.Nodes.Where(node => node.NodeId == ChildNodeId));
        Assert.Equal(RootNodeId, child.ParentNodeId);
        Assert.Equal(ChildNodeId, persisted.Node.NodeId);
    }

    [Fact]
    public async Task NodeMetadataEditor_StaleChangeVersionShowsServerRejection()
    {
        var repository = AddService(State(Node(RootNodeId)));
        var service = Services.GetRequiredService<NodeMutationApplicationService>();
        await service.UpdateAsync(TransactionId, new UpdateNodeRequest(RootNodeId, "Anderer Client", null));
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.TransactionId, TransactionId)
            .Add(component => component.ExpectedChangeVersion, 0L));

        await cut.InvokeAsync(() => cut.Find("[data-testid='edit-node-metadata']").Click());
        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-title']").Change("Veralteter Stand"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-node-metadata']").Click());

        Assert.Contains(TransactionValidationErrorCodes.ChangeVersionConflict, cut.Markup);
        Assert.True(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        Assert.Equal("Anderer Client", repository.State.Nodes.Single().Title);
    }

    [Fact]
    public async Task NodeMetadataEditor_RevertingDraftAndCancelClearsDirty()
    {
        AddService(State(Node(RootNodeId)));
        var workspaceState = Services.GetRequiredService<WorkspaceState>();
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.TransactionId, TransactionId));

        await cut.InvokeAsync(() => cut.Find("[data-testid='edit-node-metadata']").Click());
        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-title']").Change("Geänderter Titel"));
        Assert.True(workspaceState.CurrentContext.IsDirty);

        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-title']").Change("Root"));
        Assert.False(workspaceState.CurrentContext.IsDirty);

        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-description']").Change("Entwurf"));
        Assert.True(workspaceState.CurrentContext.IsDirty);
        await cut.InvokeAsync(() => cut.Find("button[type='button']").Click());

        Assert.False(workspaceState.CurrentContext.IsDirty);
        Assert.Single(cut.FindAll("[data-testid='edit-node-metadata']"));
    }

    [Fact]
    public async Task NodeMetadataEditor_DisposeClearsDirty()
    {
        AddService(State(Node(RootNodeId)));
        var workspaceState = Services.GetRequiredService<WorkspaceState>();
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.TransactionId, TransactionId));

        await cut.InvokeAsync(() => cut.Find("[data-testid='edit-node-metadata']").Click());
        await cut.InvokeAsync(() => cut.Find("[data-testid='node-metadata-title']").Change("Entwurf"));
        Assert.True(workspaceState.CurrentContext.IsDirty);

        cut.Instance.Dispose();

        Assert.False(workspaceState.CurrentContext.IsDirty);
    }

    private InMemoryNodeMutationRepository AddService(WorkingNodeMutationState state)
    {
        var repository = new InMemoryNodeMutationRepository(state);
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton(TestNodeMutations.CreateService(repository, ChildNodeId));
        return repository;
    }

    private static WorkingNodeMutationState State(params Node[] nodes) =>
        new(SnapshotId, nodes, [], [], nodes.Select(node => node.NodeId).ToArray());

    private static Node Node(NodeId nodeId, NodeId? parentNodeId = null) =>
        new(SnapshotId, nodeId, parentNodeId, "Root", null, 0, false);

    private static NodeDetailsViewModel ViewModel() =>
        new(
            RootNodeId.Value,
            null,
            "Root",
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
