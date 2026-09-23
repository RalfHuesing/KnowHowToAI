using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using KnowHowToAI.Server.Web.Components.Shared.Tabs;

namespace KnowHowToAI.Web.Tests.Components.Shared.Tabs;

[Trait("Category", "Unit")]
public sealed class TabPanelLayoutTests : BunitContext
{
    [Fact]
    public void RendersPresentSlotsInPrimaryAdditionalStatusAndActionsOrder()
    {
        var cut = Render<TabPanelLayout>(parameters => parameters
            .Add(layout => layout.PrimaryContent, Content("Primary"))
            .Add(layout => layout.AdditionalContent, Content("Additional"))
            .Add(layout => layout.Status, Content("Status"))
            .Add(layout => layout.Actions, Content("Actions")));

        var markup = cut.Find(".tab-panel-layout").InnerHtml;
        Assert.True(markup.IndexOf("tab-panel-layout__primary", StringComparison.Ordinal)
            < markup.IndexOf("tab-panel-layout__additional", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("tab-panel-layout__additional", StringComparison.Ordinal)
            < markup.IndexOf("tab-panel-layout__status", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("tab-panel-layout__status", StringComparison.Ordinal)
            < markup.IndexOf("tab-panel-layout__actions", StringComparison.Ordinal));
    }

    [Fact]
    public void OmitsEveryRegionWhoseFragmentIsMissing()
    {
        var cut = Render<TabPanelLayout>();

        Assert.Empty(cut.FindAll(".tab-panel-layout__primary"));
        Assert.Empty(cut.FindAll(".tab-panel-layout__additional"));
        Assert.Empty(cut.FindAll(".tab-panel-layout__footer"));
    }

    [Fact]
    public void KeepsHiddenPanelMountedWithItsExistingComponentState()
    {
        var cut = Render<PanelHostFixture>();
        cut.Find(".panel-host__toggle").Click();

        Assert.NotNull(cut.Find(".tab-panel-layout[hidden]"));
        Assert.Equal("0", cut.Find(".panel-state__count").TextContent);

        cut.Find(".panel-host__toggle").Click();
        cut.Find(".panel-state__increment").Click();
        Assert.Equal("1", cut.Find(".panel-state__count").TextContent);

        cut.Find(".panel-host__toggle").Click();
        Assert.Equal("1", cut.Find(".panel-state__count").TextContent);
        cut.Find(".panel-host__toggle").Click();
        Assert.Equal("1", cut.Find(".panel-state__count").TextContent);
    }

    private static RenderFragment Content(string text) => builder => builder.AddMarkupContent(0, text);

    private sealed class PanelStateFixture : ComponentBase
    {
        private int _count;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "panel-state");
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "class", "panel-state__count");
            builder.AddContent(4, _count);
            builder.CloseElement();
            builder.OpenElement(5, "button");
            builder.AddAttribute(6, "type", "button");
            builder.AddAttribute(7, "class", "panel-state__increment");
            builder.AddAttribute(8, "onclick", EventCallback.Factory.Create(this, () => _count++));
            builder.AddContent(9, "Erhöhen");
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    private sealed class PanelHostFixture : ComponentBase
    {
        private bool _visible = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "type", "button");
            builder.AddAttribute(2, "class", "panel-host__toggle");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, ToggleVisibility));
            builder.AddContent(4, "Umschalten");
            builder.CloseElement();

            builder.OpenComponent<TabPanelLayout>(5);
            builder.AddAttribute(6, nameof(TabPanelLayout.Visible), _visible);
            builder.AddAttribute(7, nameof(TabPanelLayout.PrimaryContent), (RenderFragment)(contentBuilder =>
            {
                contentBuilder.OpenComponent<PanelStateFixture>(0);
                contentBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }

        private void ToggleVisibility()
        {
            _visible = !_visible;
        }
    }
}
