using Bunit;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;
using KnowHowToAI.Server.Web.Workflow;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeMetadataEditorTests : BunitContext
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact]
    public async Task UpdatePersistsMetadataThroughTheSelectedDraft()
    {
        var repository = AddServices(State(Node(RootNodeId)));
        NodeMutationResult? persisted = null;
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.ExpectedChangeVersion, 0L)
            .Add(component => component.OnMutationSucceeded,
                EventCallback.Factory.Create<NodeMutationResult>(this, result => persisted = result)));

        await cut.Find("[data-testid='node-metadata-title']").InputAsync("Aktualisierter Titel");
        await cut.Find("[data-testid='node-metadata-description']").InputAsync("Neue Beschreibung");
        Assert.True(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        await cut.Find("[data-testid='save-node-metadata']").ClickAsync();

        Assert.NotNull(persisted);
        Assert.False(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        Assert.Equal("Aktualisierter Titel", repository.State.Nodes.Single().Title);
        Assert.Equal("Neue Beschreibung", repository.State.Nodes.Single().Description);
        Assert.Equal(1, persisted.ChangeVersion);
        Assert.Equal(KnowledgeReadContextKind.Transaction, Services.GetRequiredService<WorkspaceState>().CurrentContext.ReadContext);
    }

    [Fact]
    public async Task StaleChangeVersionShowsRejectionAndKeepsTheEditedValueDirty()
    {
        var repository = AddServices(State(Node(RootNodeId)));
        var service = Services.GetRequiredService<NodeMutationApplicationService>();
        await service.UpdateAsync(TransactionId, new UpdateNodeRequest(RootNodeId, "Anderer Client", null));
        var cut = Render<NodeMetadataEditor>(parameters => parameters
            .Add(component => component.Node, ViewModel())
            .Add(component => component.ExpectedChangeVersion, 0L));

        await cut.Find("[data-testid='node-metadata-title']").InputAsync("Veralteter Stand");
        await cut.Find("[data-testid='save-node-metadata']").ClickAsync();

        Assert.Contains(TransactionValidationErrorCodes.ChangeVersionConflict, cut.Markup);
        Assert.True(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        Assert.Equal("Anderer Client", repository.State.Nodes.Single().Title);
        Assert.Equal("Veralteter Stand", cut.Find("[data-testid='node-metadata-title']").GetAttribute("value"));
    }

    [Fact]
    public async Task CancellingMetadataInputDoesNotClearOtherDirtyEditorSources()
    {
        AddServices(State(Node(RootNodeId)));
        var workspace = Services.GetRequiredService<WorkspaceEditState>();
        var cut = Render<NodeMetadataEditor>(parameters => parameters.Add(component => component.Node, ViewModel()));

        await cut.Find("[data-testid='node-metadata-title']").InputAsync("Änderung");
        workspace.SetDirty(true, "content:test");
        await cut.Find("button[type='button']").ClickAsync();

        Assert.True(workspace.IsDirty);
        Assert.Equal("Root", cut.Find("[data-testid='node-metadata-title']").GetAttribute("value"));
    }

    private InMemoryNodeMutationRepository AddServices(WorkingNodeMutationState state)
    {
        var repository = new InMemoryNodeMutationRepository(state);
        var workspace = new WorkspaceState();
        var editState = new WorkspaceEditState();
        workspace.SetAudience("Developer");
        workspace.SetLoadedSnapshotId(SnapshotId.Value);
        workspace.SetChangeVersion(0);
        workspace.SetContext(
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Transaction, TransactionId.Value.ToString("D"), ChangeVersion: 0),
            new ReadContext(TransactionId: TransactionId));
        Services.AddSingleton(workspace);
        Services.AddSingleton(editState);
        var harness = new NavigationTestHarness(SnapshotId);
        Services.AddSingleton<WebWriteCoordinator>(serviceProvider => WebWriteTestServices.CreateCoordinator(
            harness, workspace, serviceProvider.GetRequiredService<NavigationManager>()));
        Services.AddSingleton(TestNodeMutations.CreateService(repository));
        return repository;
    }

    private static WorkingNodeMutationState State(params Node[] nodes) =>
        new(SnapshotId, nodes, [], [], nodes.Select(node => node.NodeId).ToArray());

    private static Node Node(NodeId nodeId) =>
        new(SnapshotId, nodeId, null, "Root", null, 0, false);

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
            "Independent",
            "Vorhandener Inhalt",
            [],
            ChangeVersion: 0);
}
