using Bunit;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeMutationSynchronizationTests : BunitContext
{
    private static readonly SnapshotId CurrentSnapshotId = new(1);
    private static readonly SnapshotId WorkingSnapshotId = new(2);
    private static readonly TransactionId TransactionId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly NodeId RootNodeId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly NodeId ChildNodeId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    public NodeMutationSynchronizationTests()
    {
        JSInterop.SetupModule("./Web/Components/Shared/Dialogs/AppDialog.razor.js").Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./Web/Features/Content/ContentEditor.razor.js").Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task CreateChild_SelectsNewNodeAndSynchronizesRouteAndContext()
    {
        var harness = CreateHarness(rootInWorkingSnapshot: true);
        harness.AddNode(Node(ChildNodeId, RootNodeId, "Child"));
        var cut = RenderWorkingPage(harness, RootNodeId.Value);

        await cut.InvokeAsync(() => cut.FindComponent<NodeMetadataEditor>().Instance.OnMutationSucceeded.InvokeAsync(
            Mutation(Node(ChildNodeId, RootNodeId, "Child"))));

        AssertSynchronized(cut, ChildNodeId);
    }

    [Fact]
    public async Task CreateRoot_SelectsNewRootAndSynchronizesRouteAndContext()
    {
        var harness = CreateHarness(rootInWorkingSnapshot: false);
        harness.AddNode(Node(RootNodeId, null, "Root"));
        var cut = RenderWorkingPage(harness, null);

        await cut.InvokeAsync(() => cut.FindComponent<KnowledgeTree>().Instance.OnNodeMutationSucceeded.InvokeAsync(
            Mutation(Node(RootNodeId, null, "Root"))));

        AssertSynchronized(cut, RootNodeId);
    }

    [Fact]
    public async Task Update_KeepsNodeSelectionAndSynchronizesRouteAndContext()
    {
        var harness = CreateHarness(rootInWorkingSnapshot: true);
        var cut = RenderWorkingPage(harness, RootNodeId.Value);

        await cut.InvokeAsync(() => cut.FindComponent<NodeMetadataEditor>().Instance.OnMutationSucceeded.InvokeAsync(
            Mutation(Node(RootNodeId, null, "Renamed Root"))));

        AssertSynchronized(cut, RootNodeId);
    }

    [Fact]
    public async Task Delete_SelectsParentFallbackAndRemovesDeletedNodeFromRouteAndContext()
    {
        var harness = CreateHarness(rootInWorkingSnapshot: true);
        harness.AddNode(Node(ChildNodeId, RootNodeId, "Child"));
        var cut = RenderWorkingPage(harness, ChildNodeId.Value);

        await cut.InvokeAsync(() => cut.FindComponent<NodeDeletionEditor>().Instance.OnMutationSucceeded.InvokeAsync(
            Mutation(Node(ChildNodeId, RootNodeId, "Child", IsDeleted: true))));

        AssertSynchronized(cut, RootNodeId);
        Assert.DoesNotContain(ChildNodeId.Value.ToString("D"), Services.GetRequiredService<NavigationManager>().Uri);
    }

    private IRenderedComponent<KnowledgePage> RenderWorkingPage(NavigationTestHarness harness, Guid? nodeId)
    {
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(service, transactionRepository: harness.CreateRepositories().Transactions);
        var mutationRepository = new InMemoryNodeMutationRepository(new WorkingNodeMutationState(
            WorkingSnapshotId,
            [Node(RootNodeId, null, "Root"), Node(ChildNodeId, RootNodeId, "Child")],
            [],
            [],
            [RootNodeId, ChildNodeId]));
        Services.AddSingleton(new NodeMutationApplicationService(
            mutationRepository,
            new NodeMutationService(new GuidIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        Services.AddSingleton(new NodeDeletionApplicationService(
            harness.CreateRepositories().WorkingSnapshots!,
            mutationRepository,
            new NodeMutationService(new GuidIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/knowledge{(nodeId.HasValue ? $"/{nodeId.Value:D}" : string.Empty)}?transactionId={TransactionId.Value:D}&audienceId=Developer");
        return nodeId.HasValue
            ? Render<KnowledgePage>(parameters => parameters.Add(page => page.NodeId, nodeId.Value))
            : Render<KnowledgePage>();
    }

    private static NavigationTestHarness CreateHarness(bool rootInWorkingSnapshot)
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var transaction = new KnowledgeTransaction(
            TransactionId,
            CurrentSnapshotId,
            WorkingSnapshotId,
            TransactionState.Open,
            0,
            DateTimeOffset.UtcNow,
            null,
            "Mutation-Synchronisation",
            "Test",
            "Web UI",
            null);
        harness.SetTransaction(transaction);
        harness.AddNode(Node(RootNodeId, null, "Root", snapshotId: rootInWorkingSnapshot ? WorkingSnapshotId : CurrentSnapshotId));
        harness.AddContent(new NodeContent(
            rootInWorkingSnapshot ? WorkingSnapshotId : CurrentSnapshotId,
            RootNodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Root content",
            false));
        return harness;
    }

    private static Node Node(NodeId nodeId, NodeId? parentNodeId, string title, bool IsDeleted = false, SnapshotId? snapshotId = null) =>
        new(snapshotId ?? WorkingSnapshotId, nodeId, parentNodeId, title, null, parentNodeId is null ? 0 : 1, IsDeleted);

    private static NodeMutationResult Mutation(Node node) =>
        new(node, WorkingSnapshotId, 1, [node.NodeId], AppliesToAllAudiences: true);

    private void AssertSynchronized(IRenderedComponent<KnowledgePage> cut, NodeId selectedNodeId)
    {
        var workspace = Services.GetRequiredService<WorkspaceState>();
        var uri = Services.GetRequiredService<NavigationManager>().Uri;
        Assert.Contains($"/knowledge/{selectedNodeId.Value:D}", uri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"transactionId={TransactionId.Value:D}", uri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audienceId=Developer", uri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("snapshotId=", uri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("releaseId=", uri, StringComparison.OrdinalIgnoreCase);
        var refreshed = Render<KnowledgePage>(parameters => parameters.Add(page => page.NodeId, selectedNodeId.Value));
        Assert.Contains(selectedNodeId.Value.ToString("D"), refreshed.Find("[data-testid='node-details-node-id']").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(selectedNodeId.Value, workspace.CurrentNodeId);
        Assert.Equal("Developer", workspace.CurrentAudienceId);
        Assert.Equal(TransactionId, workspace.CurrentReadContext.TransactionId);
    }
}
