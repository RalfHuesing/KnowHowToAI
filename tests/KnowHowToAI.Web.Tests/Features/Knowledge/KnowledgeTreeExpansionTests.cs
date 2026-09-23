using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreeExpansionTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly AudienceId DefaultAudienceId = new("Developer");

    [Fact]
    public async Task RefreshAsync_PreservesExpansionIntentAndRehydratesRootChildren()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        var childId = new NodeId(Guid.NewGuid());
        var otherAudienceId = new AudienceId("Other audience");
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child", null, 1, false));
        harness.AddAudience(new Audience(DefaultSnapshotId, otherAudienceId, "Other", null, false));
        using var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);
        await treeState.RefreshAsync(new ReadContext(), DefaultAudienceId.Value);

        Assert.True(treeState.RootNode!.IsExpanded);
        Assert.True(treeState.RootNode.IsChildrenPageLoaded);
        Assert.Single(treeState.RootNode.Children);
        Assert.Equal(1, treeState.LoadedPageCount);

        treeState.CollapseNode(rootId.Value);
        await treeState.RefreshAsync(new ReadContext(), DefaultAudienceId.Value);
        Assert.False(treeState.RootNode!.IsExpanded);
        Assert.False(treeState.RootNode.IsChildrenPageLoaded);
        Assert.Equal(0, treeState.LoadedPageCount);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        Assert.False(treeState.RootNode!.IsExpanded);
        Assert.False(treeState.RootNode.IsChildrenPageLoaded);
        Assert.Equal(0, treeState.LoadedPageCount);

        await treeState.ExpandNodeAsync(rootId.Value);
        await treeState.InitializeAsync(new ReadContext(), otherAudienceId.Value);
        Assert.False(treeState.RootNode!.IsExpanded);
        Assert.Equal(0, treeState.LoadedPageCount);
    }

    [Fact]
    public async Task SelectNodeAsync_ChangingSelectionDoesNotCollapseOtherExpandedBranches()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        var firstBranchId = new NodeId(Guid.NewGuid());
        var firstChildId = new NodeId(Guid.NewGuid());
        var secondBranchId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, firstBranchId, rootId, "First", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, firstChildId, firstBranchId, "First child", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, secondBranchId, rootId, "Second", null, 1, false));
        using var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);
        await treeState.ExpandNodeAsync(firstBranchId.Value);
        await treeState.SelectNodeAsync(secondBranchId.Value);

        Assert.True(treeState.FindNode(firstBranchId.Value)!.IsExpanded);
        Assert.True(treeState.FindNode(firstBranchId.Value)!.IsChildrenPageLoaded);
        Assert.NotNull(treeState.FindNode(firstChildId.Value));
    }

    [Fact]
    public async Task KnowledgeTree_EvictedExpandedBranchCollapsesAndCanBeReExpanded()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        var branchIds = new NodeId[10];
        var childIds = new NodeId[10];
        for (var index = 0; index < branchIds.Length; index++)
        {
            branchIds[index] = new NodeId(Guid.NewGuid());
            childIds[index] = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, branchIds[index], rootId, $"Branch {index}", null, index, false));
            harness.AddNode(new Node(DefaultSnapshotId, childIds[index], branchIds[index], $"Child {index}", null, 0, false));
        }

        var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));
        Services.AddKnowledgeTreeWorkspace(treeState);
        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);
        await treeState.SelectNodeAsync(branchIds[0].Value);
        for (var index = 0; index < branchIds.Length; index++)
            await treeState.ExpandNodeAsync(branchIds[index].Value);

        Assert.True(treeState.LoadedPageCount <= 10);
        var cut = Render<KnowledgeTree>();
        Assert.Empty(cut.FindAll($"button[data-testid='tree-load-children-{branchIds[1].Value}']"));
        var evictedNode = treeState.FindNode(branchIds[1].Value);
        Assert.NotNull(evictedNode);
        Assert.False(evictedNode.IsExpanded);
        Assert.False(evictedNode.IsChildrenPageLoaded);

        var toggleBtn = cut.Find($"button[data-testid='tree-toggle-{branchIds[1].Value}']");
        await cut.InvokeAsync(() => toggleBtn.Click());

        Assert.True(evictedNode.IsExpanded);
        Assert.True(evictedNode.IsChildrenPageLoaded);
        Assert.NotNull(cut.Find($"[data-testid='tree-node-{childIds[1].Value}']"));
        Assert.True(treeState.LoadedPageCount <= 10);
    }
}
