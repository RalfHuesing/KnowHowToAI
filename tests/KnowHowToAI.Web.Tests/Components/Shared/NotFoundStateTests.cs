using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class NotFoundStateTests : BunitContext
{
    [Theory]
    [InlineData(null)]
    [InlineData("Der Node wurde möglicherweise gelöscht.")]
    public void ShowsTheTitleAndTheOptionalDescription(string? description)
    {
        var cut = Render<NotFoundState>(parameters => parameters
            .Add(notFound => notFound.Title, "Node nicht gefunden")
            .Add(notFound => notFound.Description, description));

        var section = cut.Find(".not-found-state");
        Assert.Contains("Node nicht gefunden", section.TextContent, StringComparison.Ordinal);
        Assert.Equal(description is null ? 0 : 1, cut.FindAll(".not-found-state__description").Count);
    }

    [Fact]
    public void StaysTechnicallySeparatedFromTheEmptyState()
    {
        var cut = Render<NotFoundState>(parameters => parameters
            .Add(notFound => notFound.Title, "Node nicht gefunden"));

        Assert.NotNull(cut.Find(".not-found-state"));
        Assert.Empty(cut.FindAll(".empty-state"));
        Assert.DoesNotContain(
            "Erneut laden",
            cut.Find(".not-found-state").TextContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public void LabelsTheRegionWithItsOwnHeading()
    {
        var cut = Render<NotFoundState>(parameters => parameters
            .Add(notFound => notFound.Title, "Node nicht gefunden"));

        Assert.Equal(
            "not-found-state-title",
            cut.Find(".not-found-state").GetAttribute("aria-labelledby"));
        Assert.Equal(
            "not-found-state-title",
            cut.Find(".not-found-state__title").GetAttribute("id"));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void ShowsTheRetryButtonOnlyWithARetryCallback(bool hasRetry, int expectedCount)
    {
        var cut = Render<NotFoundState>(parameters =>
        {
            parameters.Add(notFound => notFound.Title, "Node nicht gefunden");
            if (hasRetry)
            {
                parameters.Add(notFound => notFound.OnRetry, () => { });
            }
        });

        Assert.Equal(expectedCount, cut.FindAll("button.not-found-state__retry").Count);
    }

    [Fact]
    public void InvokesTheRetryCallbackExactlyOncePerActivation()
    {
        var invocationCount = 0;
        var cut = Render<NotFoundState>(parameters => parameters
            .Add(notFound => notFound.Title, "Node nicht gefunden")
            .Add(notFound => notFound.OnRetry, () => invocationCount++));

        cut.Find("button.not-found-state__retry").Click();

        Assert.Equal(1, invocationCount);
    }
}
