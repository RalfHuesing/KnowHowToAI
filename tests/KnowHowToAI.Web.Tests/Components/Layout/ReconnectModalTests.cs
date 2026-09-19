using Bunit;
using KnowHowToAI.Server.Web.Components.Layout.Shell;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class ReconnectModalTests : BunitContext
{
    [Fact]
    public void RendersTheFrameworkContractDialogWithClosedDefaultState()
    {
        var cut = Render<ReconnectModal>();

        var dialog = cut.Find("#components-reconnect-modal");
        Assert.Equal("dialog", dialog.TagName.ToLowerInvariant());
        Assert.Equal("Verbindungsstatus", dialog.GetAttribute("aria-label"));
        Assert.False(dialog.HasAttribute("open"));
    }

    [Fact]
    public void RendersAllRequiredGermanStateTextsAndActions()
    {
        var cut = Render<ReconnectModal>();

        var dialog = cut.Find("#components-reconnect-modal");
        Assert.Contains("Verbindung wird wiederhergestellt …", dialog.TextContent, StringComparison.Ordinal);
        Assert.Contains("Verbindung getrennt", dialog.TextContent, StringComparison.Ordinal);
        Assert.Contains("Sitzung nicht mehr verfügbar", dialog.TextContent, StringComparison.Ordinal);
        Assert.Equal(
            "Erneut versuchen",
            cut.Find("#components-reconnect-button").TextContent.Trim());
        Assert.Equal(
            "Seite neu laden",
            cut.Find("#components-reconnect-reload-button").TextContent.Trim());
    }

    [Fact]
    public void KeepsTheFrameworkContractIdsForRetryCountdownResumeAndStateClasses()
    {
        var cut = Render<ReconnectModal>();

        cut.Find("#components-seconds-to-next-attempt");
        cut.Find("#components-resume-button");
        var dialog = cut.Find("#components-reconnect-modal");
        Assert.NotNull(dialog.QuerySelector(".components-reconnect-first-attempt-visible"));
        Assert.NotNull(dialog.QuerySelector(".components-reconnect-repeated-attempt-visible"));
        Assert.NotNull(dialog.QuerySelector(".components-reconnect-failed-visible"));
        Assert.NotNull(dialog.QuerySelector(".components-reconnect-rejected-visible"));
    }

    [Fact]
    public void AnnouncesEveryStateAsTextPlusIconInsideAPoliteStatusRegion()
    {
        var cut = Render<ReconnectModal>();

        var states = cut.FindAll("#components-reconnect-modal .components-reconnect-state");
        Assert.Equal(6, states.Count);
        foreach (var state in states)
        {
            Assert.NotNull(state.QuerySelector("svg[aria-hidden='true']"));
            Assert.False(string.IsNullOrWhiteSpace(state.QuerySelector("span")!.TextContent));
        }

        var statusRegion = cut.Find(".components-reconnect-states");
        Assert.Equal("status", statusRegion.GetAttribute("role"));
        Assert.Equal("polite", statusRegion.GetAttribute("aria-live"));
    }

    [Fact]
    public void LoadsTheOfficialReconnectIsolationModuleFromTheCollocatedScript()
    {
        var cut = Render<ReconnectModal>();

        var script = cut.Find("script[type='module']");
Assert.Contains("Components/Layout/Shell/ReconnectModal.razor.js", script.GetAttribute("src"), StringComparison.Ordinal);
    }
}
