using Bunit;
using KnowHowToAI.Server.Web.Components.Shared.States;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class LoadingStateTests : BunitContext
{
    [Theory]
    [InlineData("Inhalt wird geladen …")]
    [InlineData("Transaktionen werden geladen …")]
    public void ShowsTheProvidedMessageAsPerceivableStatusInsteadOfAnEmptyArea(
        string message)
    {
        var cut = Render<LoadingState>(parameters => parameters
            .Add(loading => loading.Message, message));

        var region = cut.Find(".loading-state");
        Assert.Equal("status", region.GetAttribute("role"));
        Assert.Equal("polite", region.GetAttribute("aria-live"));
        Assert.Equal("true", region.GetAttribute("aria-busy"));
        Assert.Contains(message, region.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void AnnouncesTheMessageExactlyOnceAndHidesTheSpinnerFromAssistiveTechnology()
    {
        var cut = Render<LoadingState>(parameters => parameters
            .Add(loading => loading.Message, "Inhalt wird geladen …"));

        Assert.Single(cut.FindAll(".loading-state[role='status']"));
        var spinner = cut.Find(".loading-state__spinner");
        Assert.Equal("true", spinner.GetAttribute("aria-hidden"));
    }
}
