using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreeTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly AudienceId DefaultAudienceId = new("Developer");

    private sealed class CountingHierarchyRepository(IHierarchyRepository inner) : IHierarchyRepository
    {
        public int CallCount { get; private set; }

        public async Task<IReadOnlyList<Node>> ListBySnapshotAsync(
            SnapshotId snapshotId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return await inner.ListBySnapshotAsync(snapshotId, cancellationToken);
        }

        public void Reset() => CallCount = 0;
    }

    [Fact]
    public void KnowledgeTree_RendersEmptyState_WhenNoVisualRoot()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        Services.AddKnowledgeTreeWorkspace(treeState);

        var cut = Render<KnowledgeTree>();
        Assert.NotNull(cut.Find("[data-testid='tree-empty']"));
    }

    [Fact]
    public async Task KnowledgeTree_UsesNestedListsAndNativeSelectionButtons()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root Node", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child Node", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        Services.AddKnowledgeTreeWorkspace(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);

        var cut = Render<KnowledgeTree>();

        // Baum-Container
        var tree = cut.Find("ul[data-testid='knowledge-tree']");
        Assert.Equal("Wissensbaum", tree.GetAttribute("aria-label"));

        // Root Node Item
        var rootItem = cut.Find($"button[data-testid='tree-node-{rootId.Value}']");
        Assert.Equal("button", rootItem.LocalName);
        Assert.Equal("false", rootItem.GetAttribute("aria-pressed"));
        Assert.Empty(cut.FindAll("[role='tree'], [role='treeitem'], [tabindex='-1']"));
    }

    [Fact]
    public async Task KnowledgeTree_ExpandAndCollapse_TogglesChildrenAndSelection()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root Node", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child Node", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        Services.AddKnowledgeTreeWorkspace(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);

        Guid? selectedId = null;
        var cut = Render<KnowledgeTree>(parameters => parameters
            .Add(p => p.OnNodeSelected, EventCallback.Factory.Create<Guid>(this, id => selectedId = id)));

        // Klick auf Toggle-Button expandiert Root
        var toggleBtn = cut.Find($"button[data-testid='tree-toggle-{rootId.Value}']");
        await cut.InvokeAsync(() => toggleBtn.Click());

        // Kindergruppe gerendert
        var group = cut.Find("ul.tree-children-group");
        Assert.NotNull(group);

        var childItem = cut.Find($"button[data-testid='tree-node-{childId.Value}']");
        Assert.NotNull(childItem);

        // Klick auf Kindknoten selektiert ihn
        await cut.InvokeAsync(() => cut.Instance.HandleTreeSelectionAsync(childId.Value.ToString()));
        Assert.Equal(childId.Value, selectedId);
        Assert.Equal("true", cut.Find($"button[data-testid='tree-node-{childId.Value}']").GetAttribute("aria-pressed"));

        // Klick auf Toggle-Button schließt Root wieder
        toggleBtn = cut.Find($"button[data-testid='tree-toggle-{rootId.Value}']");
        await cut.InvokeAsync(() => toggleBtn.Click());
        Assert.Empty(cut.FindAll("ul.tree-children-group"));
    }

    [Fact]
    public async Task KnowledgeTree_SelectionIsAStandardButtonAction()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root Node", null, 0, false));

        var child1Id = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, child1Id, rootId, "Child 1", null, 1, false));

        var child2Id = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, child2Id, rootId, "Child 2", null, 2, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        Services.AddKnowledgeTreeWorkspace(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        Guid? selectedId = null;
        var cut = Render<KnowledgeTree>(parameters => parameters
            .Add(p => p.OnNodeSelected, EventCallback.Factory.Create<Guid>(this, id => selectedId = id)));

        var childSelection = cut.Find($"button[data-testid='tree-node-{child2Id.Value}']");
        Assert.Equal("button", childSelection.LocalName);
        await cut.InvokeAsync(() => cut.Instance.HandleTreeSelectionAsync(child2Id.Value.ToString()));
        Assert.Equal(child2Id.Value, selectedId);
    }

    [Fact]
    public async Task KnowledgeTree_Paging_ButtonsReplaceItemsInDom()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root Node", null, 0, false));

        for (var i = 1; i <= 101; i++)
        {
            var childId = new NodeId(Guid.NewGuid());
            harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, $"Item {i:D3}", null, i, false));
        }

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        Services.AddKnowledgeTreeWorkspace(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        var cut = Render<KnowledgeTree>();

        // Seite 1: 100 Kinder gerendert, Next-Button sichtbar
        Assert.NotNull(cut.Find($"[data-testid='tree-title-{treeState.RootNode!.Children[0].NodeId}']"));
        var nextBtn = cut.Find($"button[data-testid='page-next-{rootId.Value}']");
        Assert.NotNull(nextBtn);

        // Weiterblättern auf Seite 2
        await cut.InvokeAsync(() => nextBtn.Click());

        // Seite 2: nur 1 Kind (Item 101), Prev-Button sichtbar
        var childTitles = cut.FindAll(".tree-node-title");
        // Root + 1 Child = 2 Titles
        Assert.Equal(2, childTitles.Count);
        Assert.Equal("Item 101", cut.Find($"[data-testid='tree-title-{treeState.RootNode.Children[0].NodeId}']").TextContent.Trim());

        var prevBtn = cut.Find($"button[data-testid='page-prev-{rootId.Value}']");
        Assert.NotNull(prevBtn);

        // Zurückblättern auf Seite 1
        await cut.InvokeAsync(() => prevBtn.Click());
        Assert.Equal(100, treeState.RootNode.Children.Count);
    }

    [Fact]
    public async Task KnowledgeTree_RendersAvailabilityFreshnessAndFindingBadgesFromTheChildrenPage()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        var explicitId = new NodeId(Guid.NewGuid());
        var fallbackId = new NodeId(Guid.NewGuid());
        var staleId = new NodeId(Guid.NewGuid());
        var sourceId = new NodeId(Guid.NewGuid());
        var defaultAudienceId = new AudienceId("Default");
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, explicitId, rootId, "Explicit", null, 1, false));
        harness.AddNode(new Node(DefaultSnapshotId, fallbackId, rootId, "Fallback", null, 2, false));
        harness.AddNode(new Node(DefaultSnapshotId, staleId, rootId, "Stale", null, 3, false));
        harness.AddNode(new Node(DefaultSnapshotId, sourceId, rootId, "Source", null, 4, false));
        harness.AddAudience(new Audience(DefaultSnapshotId, defaultAudienceId, "Default", null, false));
        harness.AddAudienceResolution(new AudienceResolution(DefaultSnapshotId, DefaultAudienceId, defaultAudienceId, 2));
        harness.AddContent(new NodeContent(DefaultSnapshotId, explicitId, DefaultAudienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Explicit", false));
        harness.AddContent(new NodeContent(DefaultSnapshotId, fallbackId, defaultAudienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Fallback", false));
        harness.AddContent(new NodeContent(DefaultSnapshotId, staleId, DefaultAudienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Stale", false));
        harness.AddContent(new NodeContent(DefaultSnapshotId, sourceId, DefaultAudienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Changed source", false));
        harness.AddDependency(new ContentDependency(DefaultSnapshotId, staleId, DefaultAudienceId, sourceId, DefaultAudienceId, new ContentRevisionId(Guid.NewGuid())));

        var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));
        Services.AddKnowledgeTreeWorkspace(treeState);
        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        var cut = Render<KnowledgeTree>();

        Assert.NotNull(cut.Find($"[data-testid='tree-badge-none-{rootId.Value}']"));
        Assert.NotNull(cut.Find($"[data-testid='tree-badge-explicit-{explicitId.Value}']"));
        Assert.NotNull(cut.Find($"[data-testid='tree-badge-fallback-{fallbackId.Value}']"));
        Assert.NotNull(cut.Find($"[data-testid='tree-badge-stale-{staleId.Value}']"));
        Assert.NotNull(cut.Find($"[data-testid='tree-badge-findings-{staleId.Value}']"));
        Assert.Empty(cut.FindAll($"[data-testid='tree-badge-findings-{explicitId.Value}']"));
    }

    [Fact]
    public async Task KnowledgeTree_Expand_LoadsExactlyOneHundredItemPage()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        for (var index = 1; index <= 101; index++)
            harness.AddNode(new Node(DefaultSnapshotId, new NodeId(Guid.NewGuid()), rootId, $"Child {index:D3}", null, index, false));

        var repositories = harness.CreateRepositories();
        var hierarchy = new CountingHierarchyRepository(repositories.Hierarchy);
        var service = new NavigationService(
            repositories with { Hierarchy = hierarchy },
            new RetrievalPolicy
            {
                DefaultPageSize = 100,
                MaximumPageSize = 100,
                SearchPageSize = 10,
                SearchMaximumPageSize = 100,
                SnippetMaximumCharacters = 100
            });
        var treeState = new KnowledgeTreeState(service);

        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        hierarchy.Reset();
        await treeState.ExpandNodeAsync(rootId.Value);

        Assert.Equal(1, hierarchy.CallCount);
        Assert.Equal(100, treeState.RootNode!.Children.Count);
        Assert.True(treeState.RootNode.HasNextPage);
    }

    [Fact]
    public async Task KnowledgeTree_DragAndDropHasNoSeparateMoveControlsAndKeepsMouseActions()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        var sourceId = new NodeId(Guid.NewGuid());
        var targetId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, sourceId, rootId, "Quelle", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, targetId, rootId, "Ziel", null, 1, false));

        var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));
        Services.AddKnowledgeTreeWorkspace(treeState);
        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        var cut = Render<KnowledgeTree>(parameters => parameters
            .Add(component => component.CanMove, true));

        Assert.Empty(cut.FindAll("[data-testid^='move-node-'], [data-testid^='move-before-'], [data-testid^='move-under-'], [data-testid^='move-after-']"));
        Assert.Empty(cut.FindAll(".tree-drop-targets"));
        Assert.NotNull(cut.Find($"button.tree-node-select[data-testid='tree-node-{sourceId.Value}']"));
        Assert.NotNull(cut.Find($"button[data-testid='tree-toggle-{rootId.Value}']"));
        Assert.NotNull(cut.Find($"button[data-testid='create-child-{sourceId.Value}']"));
    }

    [Theory]
    [InlineData(TreeMovePosition.Parent)]
    [InlineData(TreeMovePosition.Before)]
    [InlineData(TreeMovePosition.After)]
    public async Task KnowledgeTree_ServerRejectedDropLeavesTheRenderedTreeUntouched(TreeMovePosition position)
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        var sourceId = new NodeId(Guid.NewGuid());
        var targetId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, sourceId, rootId, "Quelle", null, 0, false));
        harness.AddNode(new Node(DefaultSnapshotId, targetId, rootId, "Ziel", null, 1, false));

        var treeState = new KnowledgeTreeState(harness.CreateService(defaultPageSize: 100, maximumPageSize: 100));
        Services.AddKnowledgeTreeWorkspace(treeState);
        await treeState.InitializeAsync(new ReadContext(), DefaultAudienceId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        var cut = Render<KnowledgeTree>(parameters => parameters.Add(component => component.CanMove, true));

        await cut.InvokeAsync(() => cut.Instance.HandleTreeDropAsync(
            sourceId.Value.ToString(),
            targetId.Value.ToString(),
            position.ToString()));
        cut.Render();

        Assert.NotNull(cut.Find($"[data-testid='tree-node-{sourceId.Value}']"));
        Assert.NotNull(cut.Find($"[data-testid='tree-node-{targetId.Value}']"));
        Assert.NotNull(cut.Find("[data-testid='tree-move-error']"));
        Assert.Equal(rootId.Value.ToString(), treeState.FindNode(sourceId.Value)!.ParentNodeId!.Value.ToString());
    }
}
