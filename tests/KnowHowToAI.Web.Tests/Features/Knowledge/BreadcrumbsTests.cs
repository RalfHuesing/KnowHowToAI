using Bunit;
using KnowHowToAI.Server.Web.Features.Knowledge;
using Microsoft.AspNetCore.Components;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class BreadcrumbsTests : BunitContext
{
    [Fact]
    public void Breadcrumbs_RendersHierarchyAndTriggersCallback()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();

        var items = new List<KnowledgeTreeNodeViewModel>
        {
            new()
            {
                Summary = new ChildNodeViewModel(rootId, "Root", null, 0, 1, 0, "Explicit", null, "Fresh"),
                Depth = 0
            },
            new()
            {
                Summary = new ChildNodeViewModel(childId, "Kategorie A", null, 1, 1, 0, "Explicit", null, "Fresh"),
                ParentNodeId = rootId,
                Depth = 1
            },
            new()
            {
                Summary = new ChildNodeViewModel(grandchildId, "Artikel 1", null, 1, 0, 0, "Explicit", null, "Fresh"),
                ParentNodeId = childId,
                Depth = 2
            }
        };

        Guid? selectedNodeId = null;

        var cut = Render<Breadcrumbs>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnSelectNode, EventCallback.Factory.Create<Guid>(this, id => selectedNodeId = id)));

        // Prüfe semantische Struktur
        var nav = cut.Find("nav[data-testid='breadcrumbs']");
        Assert.Equal("Breadcrumb", nav.GetAttribute("aria-label"));

        // Letzter Eintrag hat aria-current="page"
        var current = cut.Find("[aria-current='page']");
        Assert.Equal("Artikel 1", current.TextContent.Trim());

        // Frühere Einträge sind anklickbare Buttons
        var rootButton = cut.Find($"button[data-testid='breadcrumb-{rootId}']");
        Assert.Equal("Root", rootButton.TextContent.Trim());

        // Klick auf Root-Button löst Callback mit rootId aus
        rootButton.Click();
        Assert.Equal(rootId, selectedNodeId);
    }
}
