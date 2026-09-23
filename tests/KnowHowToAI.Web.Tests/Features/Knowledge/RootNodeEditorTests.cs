using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class RootNodeEditorTests : BunitContext
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    private static readonly NodeId ChildNodeId = new(Guid.Parse("55555555-5555-5555-5555-555555555555"));

    [Fact]
    public void RootNodeEditor_WithoutTransactionOffersCreationWithoutBeginningDraft()
    {
        AddService();

        var cut = Render<RootNodeEditor>();

        Assert.Single(cut.FindAll("[data-testid='root-node-editor']"));
        Assert.NotNull(cut.Find("[data-testid='create-root-node']"));
        Assert.Null(Services.GetRequiredService<WorkspaceState>().ActiveTransactionId);
    }

    [Fact]
    public async Task RootNodeEditor_EmptyWorkingTree_CreatesRootWithOptionalDescription()
    {
        var repository = AddService();
        NodeMutationResult? persisted = null;
        var cut = Render<RootNodeEditor>(parameters => parameters
            .Add(component => component.OnMutationSucceeded, EventCallback.Factory.Create<NodeMutationResult>(this, result => persisted = result)));

        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-title']").Input("Erstes Wissen"));
        Assert.True(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-description']").Input("Startpunkt"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='create-root-node']").Click());

        Assert.NotNull(persisted);
        Assert.NotNull(Services.GetRequiredService<WorkspaceState>().ActiveTransactionId);
        Assert.False(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
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
        var cut = Render<RootNodeEditor>();

        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-title']").Input("Entwurf"));
        Assert.True(Services.GetRequiredService<WorkspaceEditState>().IsDirty);

        await cut.InvokeAsync(() => cut.Find("[data-testid='cancel-root-node']").Click());

        Assert.False(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        Assert.Equal(string.Empty, cut.Find("[data-testid='root-node-title']").GetAttribute("value"));
    }

    [Fact]
    public async Task RootNodeEditor_CreatesChildUnderSelectedParent()
    {
        var parentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var parent = new Node(SnapshotId, new NodeId(parentId), null, "Übergeordnet", null, 0, false);
        var repository = AddService(initialNodes: [parent]);
        var cut = Render<RootNodeEditor>(parameters => parameters
            .Add(component => component.ParentNodeId, parentId)
            .Add(component => component.ParentTitle, "Übergeordnet"));

        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-title']").Input("Unterwissen"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='create-root-node']").Click());

        var child = Assert.Single(repository.State.Nodes, node => node.ParentNodeId is not null);
        Assert.Equal(new NodeId(parentId), child.ParentNodeId);
        Assert.Equal("Unterwissen", child.Title);
    }

    [Fact]
    public async Task RootNodeEditor_StaleCurrentRejectsBeforeBeginningDraft()
    {
        var repository = AddService(staleCurrent: true);
        var cut = Render<RootNodeEditor>();

        await cut.InvokeAsync(() => cut.Find("[data-testid='root-node-title']").Input("Veraltet"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='create-root-node']").Click());

        Assert.Contains("SnapshotConflict", cut.Markup, StringComparison.Ordinal);
        Assert.Null(Services.GetRequiredService<WorkspaceState>().ActiveTransactionId);
        Assert.Empty(repository.State.Nodes);
    }

    [Fact]
    public async Task RootNodeEditor_ConcurrentFirstRootAndChildWritesShareOneDraft()
    {
        var repository = AddService(identifierGenerator: new SequentialIdentifierGenerator());
        var rootEditor = Render<RootNodeEditor>();
        var childEditor = Render<RootNodeEditor>(parameters => parameters
            .Add(component => component.ParentNodeId, RootNodeId.Value)
            .Add(component => component.ParentTitle, "Erstes Wissen"));
        await rootEditor.InvokeAsync(() => rootEditor.Find("[data-testid='root-node-title']").Input("Erstes Wissen"));
        await childEditor.InvokeAsync(() => childEditor.Find("[data-testid='root-node-title']").Input("Unterwissen"));

        var rootWrite = rootEditor.Find("[data-testid='create-root-node']").ClickAsync();
        var childWrite = childEditor.Find("[data-testid='create-root-node']").ClickAsync();
        await Task.WhenAll(rootWrite, childWrite);

        Assert.Equal(RootNodeId, Assert.Single(repository.State.Nodes, node => node.ParentNodeId is null).NodeId);
        Assert.Equal(ChildNodeId, Assert.Single(repository.State.Nodes, node => node.ParentNodeId == RootNodeId).NodeId);
        Assert.Equal(2, Services.GetRequiredService<WorkspaceState>().CurrentChangeVersion);
        Assert.NotNull(Services.GetRequiredService<WorkspaceState>().ActiveTransactionId);
    }

    private InMemoryNodeMutationRepository AddService(
        bool staleCurrent = false,
        IReadOnlyList<Node>? initialNodes = null,
        IIdentifierGenerator? identifierGenerator = null)
    {
        var harness = new NavigationTestHarness(SnapshotId);
        var repository = new InMemoryNodeMutationRepository(
            new WorkingNodeMutationState(
                SnapshotId,
                initialNodes ?? [],
                [],
                [],
                initialNodes?.Select(node => node.NodeId).ToArray() ?? []));
        var workspace = new WorkspaceState();
        workspace.SetLoadedSnapshotId(staleCurrent ? SnapshotId.Value + 1 : SnapshotId.Value);
        workspace.SetAudience("Developer");
        Services.AddSingleton(workspace);
        Services.AddSingleton<WorkspaceEditState>();
        Services.AddSingleton(new NodeMutationApplicationService(
            repository,
            new NodeMutationService(identifierGenerator ?? new FixedIdentifierGenerator { FixedNodeId = RootNodeId }),
            TestPolicies.DefaultValidation));
        Services.AddSingleton<WebWriteCoordinator>(provider => WebWriteTestServices.CreateCoordinator(
            harness,
            workspace,
            provider.GetRequiredService<NavigationManager>()));
        return repository;
    }

    private sealed class SequentialIdentifierGenerator : IIdentifierGenerator
    {
        private int _createdNodes;

        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => Interlocked.Increment(ref _createdNodes) switch
        {
            1 => RootNodeId,
            2 => ChildNodeId,
            _ => throw new InvalidOperationException("Mehr als zwei Testknoten angefordert.")
        };

        public ContentRevisionId CreateContentRevisionId() => throw new NotSupportedException();
    }
}
