using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreeTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly RoleId DefaultRoleId = new("Developer");

    [Fact]
    public void KnowledgeTree_RendersEmptyState_WhenNoVisualRoot()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);

        var cut = Render<KnowledgeTree>();
        Assert.NotNull(cut.Find("[data-testid='tree-empty']"));
    }

    [Fact]
    public async Task KnowledgeTree_SemanticRolesAndRovingTabindex()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root Node", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child Node", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        var cut = Render<KnowledgeTree>();

        // Baum-Container
        var tree = cut.Find("div[role='tree']");
        Assert.Equal("Wissensbaum", tree.GetAttribute("aria-label"));

        // Root Node Item
        var rootItem = cut.Find($"div[data-testid='treeitem-{rootId.Value}']");
        Assert.Equal("treeitem", rootItem.GetAttribute("role"));
        Assert.Equal("1", rootItem.GetAttribute("aria-level"));
        Assert.Equal("false", rootItem.GetAttribute("aria-expanded"));
        Assert.Equal("false", rootItem.GetAttribute("aria-selected"));
        Assert.Equal("0", rootItem.GetAttribute("tabindex")); // Roving tabindex aktiv auf Root
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
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);

        Guid? selectedId = null;
        var cut = Render<KnowledgeTree>(parameters => parameters
            .Add(p => p.OnNodeSelected, EventCallback.Factory.Create<Guid>(this, id => selectedId = id)));

        // Klick auf Toggle-Button expandiert Root
        var toggleBtn = cut.Find($"button[data-testid='tree-toggle-{rootId.Value}']");
        await cut.InvokeAsync(() => toggleBtn.Click());

        // Kindergruppe gerendert
        var group = cut.Find("div[role='group']");
        Assert.NotNull(group);

        var childItem = cut.Find($"div[data-testid='treeitem-{childId.Value}']");
        Assert.Equal("2", childItem.GetAttribute("aria-level"));
        Assert.Equal("-1", childItem.GetAttribute("tabindex")); // Kind hat tabindex -1

        // Klick auf Kindknoten selektiert ihn
        await cut.InvokeAsync(() => childItem.Click());
        Assert.Equal(childId.Value, selectedId);
        Assert.Equal("true", cut.Find($"div[data-testid='treeitem-{childId.Value}']").GetAttribute("aria-selected"));

        // Klick auf Toggle-Button schließt Root wieder
        toggleBtn = cut.Find($"button[data-testid='tree-toggle-{rootId.Value}']");
        await cut.InvokeAsync(() => toggleBtn.Click());
        Assert.Empty(cut.FindAll("div[role='group']"));
    }

    [Fact]
    public async Task KnowledgeTree_KeyboardNavigation_ArrowsHomeEndAndSelection()
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
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);
        await treeState.ExpandNodeAsync(rootId.Value);

        Guid? selectedId = null;
        var cut = Render<KnowledgeTree>(parameters => parameters
            .Add(p => p.OnNodeSelected, EventCallback.Factory.Create<Guid>(this, id => selectedId = id)));

        var tree = cut.Find("div[role='tree']");

        // ArrowDown: Fokus wechselt von Root auf Child 1
        await cut.InvokeAsync(() => tree.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" }));
        Assert.Equal("0", cut.Find($"div[data-testid='treeitem-{child1Id.Value}']").GetAttribute("tabindex"));
        Assert.Equal("-1", cut.Find($"div[data-testid='treeitem-{rootId.Value}']").GetAttribute("tabindex"));

        // ArrowDown: Fokus wechselt auf Child 2
        await cut.InvokeAsync(() => tree.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" }));
        Assert.Equal("0", cut.Find($"div[data-testid='treeitem-{child2Id.Value}']").GetAttribute("tabindex"));

        // ArrowUp: Fokus wechselt zurück auf Child 1
        await cut.InvokeAsync(() => tree.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" }));
        Assert.Equal("0", cut.Find($"div[data-testid='treeitem-{child1Id.Value}']").GetAttribute("tabindex"));

        // Home: Fokus springt auf Root
        await cut.InvokeAsync(() => tree.KeyDown(new KeyboardEventArgs { Key = "Home" }));
        Assert.Equal("0", cut.Find($"div[data-testid='treeitem-{rootId.Value}']").GetAttribute("tabindex"));

        // End: Fokus springt auf Child 2
        await cut.InvokeAsync(() => tree.KeyDown(new KeyboardEventArgs { Key = "End" }));
        Assert.Equal("0", cut.Find($"div[data-testid='treeitem-{child2Id.Value}']").GetAttribute("tabindex"));

        // Enter: Selektiert Child 2
        await cut.InvokeAsync(() => tree.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
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
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);

        await treeState.InitializeAsync(new ReadContext(), DefaultRoleId.Value);
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
}
