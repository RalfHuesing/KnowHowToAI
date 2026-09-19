using Bunit;
using KnowHowToAI.Server.Web.Components.Shared.States;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class BusyOverlayTests : BunitContext
{
    [Theory]
    [InlineData(true, "true", 1)]
    [InlineData(false, "false", 0)]
    public void MarksOnlyTheAffectedRegionAsBusyAndBlocksItsContentWhileBusy(
        bool isBusy, string expectedAriaBusy, int expectedBlockedCount)
    {
        var cut = RenderOverlay(isBusy);

        var region = cut.Find(".busy-overlay");
        Assert.Equal(expectedAriaBusy, region.GetAttribute("aria-busy"));
        Assert.Equal(expectedBlockedCount, cut.FindAll(".busy-overlay__content[inert]").Count);
        Assert.Equal(expectedBlockedCount, cut.FindAll(".busy-overlay__scrim").Count);
    }

    [Fact]
    public void KeepsTheCoveredContentRenderedAndVisibleWhileBusy()
    {
        var cut = RenderOverlay(isBusy: true);

        var content = cut.Find(".busy-overlay__content");
        Assert.Contains("Wissensbaum", content.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void AnnouncesTheBusyTextPolitelyAndExactlyOnceWhileBusy()
    {
        var cut = RenderOverlay(isBusy: true);

        Assert.Single(cut.FindAll(".busy-overlay__status[role='status']"));
        Assert.Equal(
            "Änderungen werden gespeichert …",
            cut.Find(".busy-overlay__status").TextContent.Trim());
    }

    [Fact]
    public void AnnouncesNoStatusAndShowsNoScrimWhenIdle()
    {
        var cut = RenderOverlay(isBusy: false);

        Assert.Empty(cut.FindAll(".busy-overlay__status"));
        Assert.Empty(cut.FindAll(".busy-overlay__scrim"));
    }

    [Fact]
    public void AllowsTheCoveredActionExactlyOnceWhenIdle()
    {
        var invocationCount = 0;
        var cut = Render<BusyOverlay>(parameters =>
        {
            parameters.Add(overlay => overlay.IsBusy, false);
            parameters.Add(overlay => overlay.BusyText, "Änderungen werden gespeichert …");
            parameters.Add(overlay => overlay.ChildContent, button =>
            {
                button.OpenElement(0, "button");
                button.AddAttribute(1, "type", "button");
                button.AddAttribute(
                    2,
                    "onclick",
                    EventCallback.Factory.Create(this, () => invocationCount++));
                button.AddContent(3, "Speichern");
                button.CloseElement();
            });
        });

        cut.Find("button").Click();

        Assert.Equal(1, invocationCount);
    }

    private IRenderedComponent<BusyOverlay> RenderOverlay(bool isBusy)
    {
        return Render<BusyOverlay>(parameters => parameters
            .Add(overlay => overlay.IsBusy, isBusy)
            .Add(overlay => overlay.BusyText, "Änderungen werden gespeichert …")
            .Add(overlay => overlay.ChildContent, content =>
                content.AddContent(0, "Wissensbaum")));
    }
}
