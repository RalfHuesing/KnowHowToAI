using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using KnowHowToAI.Server.Web.Components.Shared.Tabs;

namespace KnowHowToAI.Web.Tests.Components.Shared.Tabs;

[Trait("Category", "Unit")]
public sealed class TabLayoutTests : BunitContext
{
    [Fact]
    public void RendersDefinitionsInOrderAndMarksOnlyTheActiveButton()
    {
        var cut = Render<TabLayout>(parameters => parameters
            .Add(layout => layout.Tabs, CreateTabs())
            .Add(layout => layout.ActiveKey, "edit")
            .Add(layout => layout.OnTabSelected, _ => { })
            .Add(layout => layout.Panels, Content("Panels")));

        var buttons = cut.FindAll(".tab-layout__button");
        Assert.Equal(
            ["Lesen", "Bearbeiten", "Technische Details"],
            buttons.Select(button => button.TextContent.Trim()).ToArray());
        Assert.Equal("false", buttons[0].GetAttribute("aria-pressed"));
        Assert.Equal("true", buttons[1].GetAttribute("aria-pressed"));
        Assert.Equal("false", buttons[2].GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task ReportsTheSelectedKeyWhenAButtonIsClicked()
    {
        string? selectedKey = null;
        var cut = Render<TabLayout>(parameters => parameters
            .Add(layout => layout.Tabs, CreateTabs())
            .Add(layout => layout.ActiveKey, "read")
            .Add(layout => layout.OnTabSelected, key => selectedKey = key)
            .Add(layout => layout.Panels, Content("Panels")));

        await cut.FindAll(".tab-layout__button")[1].ClickAsync();

        Assert.Equal("edit", selectedKey);
    }

    [Fact]
    public void PlacesContextBesideNavigationAndPanelsAfterTheHeader()
    {
        var cut = Render<TabLayout>(parameters => parameters
            .Add(layout => layout.Tabs, CreateTabs())
            .Add(layout => layout.ActiveKey, "read")
            .Add(layout => layout.OnTabSelected, _ => { })
            .Add(layout => layout.Context, Content("Context"))
            .Add(layout => layout.Panels, Content("Panels")));

        var markup = cut.Find(".tab-layout").InnerHtml;
        Assert.True(markup.IndexOf("tab-layout__navigation", StringComparison.Ordinal)
            < markup.IndexOf("tab-layout__context", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("tab-layout__context", StringComparison.Ordinal)
            < markup.IndexOf("tab-layout__panels", StringComparison.Ordinal));
    }

    private static IReadOnlyList<TabDefinition> CreateTabs() =>
    [
        new("read", "Lesen"),
        new("edit", "Bearbeiten"),
        new("details", "Technische Details")
    ];

    private static RenderFragment Content(string text) => builder => builder.AddMarkupContent(0, text);
}
