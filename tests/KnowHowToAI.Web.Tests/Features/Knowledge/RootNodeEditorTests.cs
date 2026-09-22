using Bunit;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class RootNodeEditorTests : BunitContext
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("5c96195b-a594-4de3-9660-3e7efdcf6748"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public void RootNodeEditor_WithoutTransaction_DoesNotOfferCreation()
    {
        AddService();

        var cut = Render<RootNodeEditor>();

        Assert.Empty(cut.FindAll("[data-testid='root-node-editor']"));
        Assert.Empty(cut.FindAll("[data-testid='create-root-node']"));
    }

    [Fact]
    public async Task RootNodeEditor_EmptyWorkingTree_CreatesRootWithOptionalDescription()
    {
        var repository = AddService();
        NodeMutationResult? persisted = null;
        var cut = Render<RootNodeEditor>(parameters => parameters
            .Add(component => component.TransactionId, TransactionId)
            .Add(component => component.ExpectedChangeVersion, 0L)
            .Add(component => component.OnMutationSucceeded, EventCallback.Factory.Create<NodeMutationResult>(this, result => persisted = result)));

        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-title']").Change("Erstes Wissen"));
        Assert.True(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-description']").Change("Startpunkt"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='create-root-node']").Click());

        Assert.NotNull(persisted);
        Assert.False(Services.GetRequiredService<WorkspaceState>().CurrentContext.IsDirty);
        var root = Assert.Single(repository.State.Nodes);
        Assert.Equal(RootNodeId, root.NodeId);
        Assert.Null(root.ParentNodeId);
        Assert.Equal("Erstes Wissen", root.Title);
        Assert.Equal("Startpunkt", root.Description);
        Assert.Equal(RootNodeId, persisted.Node.NodeId);
        Assert.Equal(1, persisted.ChangeVersion);
    }

    [Fact]
    public async Task RootNodeEditor_CancelClearsDirtyDraft()
    {
        AddService();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();
        var cut = Render<RootNodeEditor>(parameters => parameters
            .Add(component => component.TransactionId, TransactionId));

        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-title']").Change("Entwurf"));
        Assert.True(workspaceState.CurrentContext.IsDirty);

        await cut.InvokeAsync(() => cut.Find("[data-testid='cancel-root-node']").Click());

        Assert.False(workspaceState.CurrentContext.IsDirty);
        Assert.Equal(string.Empty, cut.Find("[data-testid='root-node-title']").GetAttribute("value"));
    }

    private InMemoryNodeMutationRepository AddService()
    {
        var repository = new InMemoryNodeMutationRepository(
            new WorkingNodeMutationState(SnapshotId, [], [], [], []));
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton(TestNodeMutations.CreateService(repository, RootNodeId));
        return repository;
    }
}
