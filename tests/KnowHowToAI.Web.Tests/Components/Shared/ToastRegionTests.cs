using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;
using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class ToastRegionTests : BunitContext
{
    [Fact]
    public void ProvidesAPermanentPoliteRegionWithoutEntriesAndWithoutFocusMovement()
    {
        var cut = RenderRegion(new ToastState());

        var region = cut.Find(".toast-region");
        Assert.Equal("polite", region.GetAttribute("aria-live"));
        Assert.Equal("Meldungen", region.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll(".toast-region__toast"));
        Assert.DoesNotContain("autofocus", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnnouncesACompletedActionPolitelyAsTextPlusIcon()
    {
        var state = new ToastState();
        var cut = RenderRegion(state);

        state.Show("Die Änderung wurde gespeichert.");
        cut.WaitForState(() => cut.FindAll(".toast-region__toast").Count == 1);

        var toast = cut.Find(".toast-region__toast");
        Assert.Contains(
            "Die Änderung wurde gespeichert.",
            toast.TextContent,
            StringComparison.Ordinal);
        Assert.Equal("true", cut.Find(".toast-region__icon").GetAttribute("aria-hidden"));
        Assert.DoesNotContain("autofocus", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeepsEveryMessageUntilTheUserDismissesIt()
    {
        var state = new ToastState();
        var cut = RenderRegion(state);

        state.Show("Die Änderung wurde gespeichert.");
        state.Show("Der Snapshot wurde verglichen.");
        cut.WaitForState(() => cut.FindAll(".toast-region__toast").Count == 2);

        cut.Find(".toast-region__dismiss").Click();
        cut.WaitForState(() => cut.FindAll(".toast-region__toast").Count == 1);

        Assert.Single(cut.FindAll(".toast-region__dismiss"));
        Assert.Contains(
            "Der Snapshot wurde verglichen.",
            cut.Find(".toast-region").TextContent,
            StringComparison.Ordinal);
    }

    private IRenderedComponent<ToastRegion> RenderRegion(ToastState state) =>
        Render<ToastRegion>(parameters => parameters.Add(region => region.State, state));
}
