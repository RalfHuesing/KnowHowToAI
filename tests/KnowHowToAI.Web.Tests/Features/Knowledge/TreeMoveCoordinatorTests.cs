using Bunit;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class TreeMoveCoordinatorTests : BunitContext
{
    private static readonly SnapshotId SnapshotId = new(1);
    private static readonly TransactionId TransactionId = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Theory]
    [InlineData(TreeMovePosition.Before, 2, 0, "Dritter", "Erster", "Zweiter")]
    [InlineData(TreeMovePosition.After, 0, 2, "Zweiter", "Dritter", "Erster")]
    public async Task MoveAsync_MapsVisibleDropPositionToExactInsertion(
        TreeMovePosition position,
        int sourceIndex,
        int targetIndex,
        string firstTitle,
        string secondTitle,
        string thirdTitle)
    {
        var rootId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var firstId = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        var secondId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000003"));
        var thirdId = new NodeId(Guid.Parse("40000000-0000-0000-0000-000000000004"));
        var nodes = new Node[]
        {
            new(SnapshotId, rootId, null, "Root", null, 0, false),
            new(SnapshotId, firstId, rootId, "Erster", null, 0, false),
            new(SnapshotId, secondId, rootId, "Zweiter", null, 1, false),
            new(SnapshotId, thirdId, rootId, "Dritter", null, 2, false)
        };
        var harness = new NavigationTestHarness(SnapshotId);
        foreach (var node in nodes)
            harness.AddNode(node);

        var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));
        await treeState.InitializeAsync(new ReadContext(), "Developer");
        await treeState.ExpandNodeAsync(rootId.Value);

        var workspaceState = new WorkspaceState();
        workspaceState.SetContext(
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Transaction, TransactionId.Value.ToString("D"), ChangeVersion: 0),
            new ReadContext(TransactionId: TransactionId));
        workspaceState.SetAudience("Developer");
        workspaceState.SetChangeVersion(0);

        var repository = new InMemoryNodeMutationRepository(
            new WorkingNodeMutationState(SnapshotId, nodes, Array.Empty<NodeContent>(), Array.Empty<ContentDependency>(), nodes.Select(node => node.NodeId).ToArray()));
        var coordinator = new TreeMoveCoordinator(
            new NodeMutationApplicationService(repository, new NodeMutationService(new GuidIdentifierGenerator()), TestPolicies.DefaultValidation),
            workspaceState,
            new WebReadContextResolver(new InMemoryReleaseRepository(), new InMemoryTransactionRepository(new InMemoryKnowledgeStore())),
            new PageRegionState(),
            treeState);

        var source = treeState.RootNode!.Children[sourceIndex];
        var target = treeState.RootNode.Children[targetIndex];
        var result = await coordinator.MoveAsync(
            new TreeMoveRequest(source.NodeId, target.NodeId, target.ParentNodeId, target.Summary.SortOrder, position),
            TransactionId.Value.ToString("D"),
            null,
            null);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [firstTitle, secondTitle, thirdTitle],
            repository.State.Nodes
                .Where(node => node.ParentNodeId == rootId)
                .OrderBy(node => node.SortOrder)
            .Select(node => node.Title));
    }

    [Theory]
    [InlineData(TreeMovePosition.Before)]
    [InlineData(TreeMovePosition.Parent)]
    [InlineData(TreeMovePosition.After)]
    public async Task KnowledgeTree_DropZones_MapThroughTheSharedCoordinator(TreeMovePosition position)
    {
        var rootId = new NodeId(Guid.Parse("50000000-0000-0000-0000-000000000005"));
        var sourceId = new NodeId(Guid.Parse("60000000-0000-0000-0000-000000000006"));
        var targetId = new NodeId(Guid.Parse("70000000-0000-0000-0000-000000000007"));
        var nodes = new Node[]
        {
            new(SnapshotId, rootId, null, "Root", null, 0, false),
            new(SnapshotId, sourceId, rootId, "Quelle", null, 0, false),
            new(SnapshotId, targetId, rootId, "Ziel", null, 1, false)
        };
        var harness = new NavigationTestHarness(SnapshotId);
        foreach (var node in nodes)
            harness.AddNode(node);

        var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));
        await treeState.InitializeAsync(new ReadContext(), "Developer");
        await treeState.ExpandNodeAsync(rootId.Value);
        var workspaceState = new WorkspaceState();
        workspaceState.SetContext(
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Transaction, TransactionId.Value.ToString("D"), ChangeVersion: 0),
            new ReadContext(TransactionId: TransactionId));
        workspaceState.SetAudience("Developer");
        workspaceState.SetChangeVersion(0);
        var repository = new InMemoryNodeMutationRepository(
            new WorkingNodeMutationState(SnapshotId, nodes, Array.Empty<NodeContent>(), Array.Empty<ContentDependency>(), nodes.Select(node => node.NodeId).ToArray()));
        var coordinator = new TreeMoveCoordinator(
            new NodeMutationApplicationService(repository, new NodeMutationService(new GuidIdentifierGenerator()), TestPolicies.DefaultValidation),
            workspaceState,
            new WebReadContextResolver(new InMemoryReleaseRepository(), new InMemoryTransactionRepository(new InMemoryKnowledgeStore())),
            new PageRegionState(),
            treeState);
        Services.AddWebPageStates(workspaceState: workspaceState).AddKnowledgeTreeWorkspace(treeState);
        Services.AddSingleton(coordinator);

        var cut = Render<KnowledgeTree>(parameters => parameters.Add(component => component.CanMove, true));
        await cut.InvokeAsync(() => cut.Instance.HandleTreeDropAsync(
            sourceId.Value.ToString(),
            targetId.Value.ToString(),
            position.ToString()));

        Assert.DoesNotContain("[data-testid='tree-move-error']", cut.Markup, StringComparison.Ordinal);
        var moved = Assert.Single(repository.State.Nodes, node => node.NodeId == sourceId);
        if (position == TreeMovePosition.Parent)
        {
            Assert.Equal(targetId, moved.ParentNodeId);
        }
        else
        {
            Assert.Equal(rootId, moved.ParentNodeId);
            var ordered = repository.State.Nodes
                .Where(node => node.ParentNodeId == rootId)
                .OrderBy(node => node.SortOrder)
                .Select(node => node.NodeId)
                .ToArray();
            Assert.Equal(
                position == TreeMovePosition.Before
                    ? new[] { sourceId, targetId }
                    : new[] { targetId, sourceId },
                ordered);
        }
    }
}
