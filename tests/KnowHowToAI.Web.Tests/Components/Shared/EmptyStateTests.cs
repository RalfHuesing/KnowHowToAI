using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class EmptyStateTests : BunitContext
{
    [Theory]
    [InlineData(null)]
    [InlineData("Legen Sie den ersten Node an, um zu beginnen.")]
    public void ShowsTheTitleAndTheOptionalDescription(string? description)
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(empty => empty.Title, "Noch keine Nodes vorhanden")
            .Add(empty => empty.Description, description));

        var section = cut.Find(".empty-state");
        Assert.Contains("Noch keine Nodes vorhanden", section.TextContent, StringComparison.Ordinal);
        Assert.Equal(description is null ? 0 : 1, cut.FindAll(".empty-state__description").Count);
    }

    [Fact]
    public void LabelsTheRegionWithItsOwnHeading()
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(empty => empty.Title, "Noch keine Nodes vorhanden"));

        Assert.Equal(
            "empty-state-title",
            cut.Find(".empty-state").GetAttribute("aria-labelledby"));
        Assert.Equal(
            "empty-state-title",
            cut.Find(".empty-state__title").GetAttribute("id"));
    }

    [Fact]
    public void HidesTheIconFromAssistiveTechnology()
    {
        var cut = Render<EmptyState>(parameters => parameters
            .Add(empty => empty.Title, "Noch keine Nodes vorhanden"));

        Assert.Equal("true", cut.Find(".empty-state__icon").GetAttribute("aria-hidden"));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void ShowsTheRetryButtonOnlyWithARetryCallback(bool hasRetry, int expectedCount)
    {
        var cut = Render<EmptyState>(parameters =>
        {
            parameters.Add(empty => empty.Title, "Noch keine Nodes vorhanden");
            if (hasRetry)
            {
                parameters.Add(empty => empty.OnRetry, () => { });
            }
        });

        Assert.Equal(expectedCount, cut.FindAll("button.empty-state__retry").Count);
    }

    [Fact]
    public void InvokesTheRetryCallbackExactlyOncePerActivation()
    {
        var invocationCount = 0;
        var cut = Render<EmptyState>(parameters => parameters
            .Add(empty => empty.Title, "Noch keine Nodes vorhanden")
            .Add(empty => empty.OnRetry, () => invocationCount++));

        cut.Find("button.empty-state__retry").Click();

        Assert.Equal(1, invocationCount);
        Assert.Equal("Erneut laden", cut.Find("button.empty-state__retry").TextContent.Trim());
    }
}
