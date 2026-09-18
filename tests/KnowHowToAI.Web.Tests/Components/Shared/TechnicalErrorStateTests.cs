using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class TechnicalErrorStateTests : BunitContext
{
    [Fact]
    public void ShowsOnlyNeutralGermanTextWithoutSensitiveDetails()
    {
        var cut = Render<TechnicalErrorState>(parameters => parameters
            .Add(error => error.CorrelationId, "corr-2026-09-18-01"));

        var section = cut.Find(".technical-error");
        Assert.Contains(
            "Es ist ein technischer Fehler aufgetreten. Bitte versuchen Sie es erneut.",
            section.TextContent,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", section.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQL", section.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at KnowHowToAI", section.TextContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("corr-2026-09-18-01", 1)]
    public void ShowsTheCorrelationIdOnlyWhenProvided(string? correlationId, int expectedCount)
    {
        var cut = Render<TechnicalErrorState>(parameters => parameters
            .Add(error => error.CorrelationId, correlationId));

        Assert.Equal(expectedCount, cut.FindAll(".technical-error__correlation").Count);
        if (correlationId is not null)
        {
            Assert.Contains(
                $"Fehlerkennung: {correlationId}",
                cut.Find(".technical-error__correlation").TextContent,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void IsFocusableAndLinkedToItsHeadingAsAlertRegion()
    {
        var cut = Render<TechnicalErrorState>();

        var section = cut.Find(".technical-error");
        Assert.Equal("alert", section.GetAttribute("role"));
        Assert.Equal("-1", section.GetAttribute("tabindex"));
        Assert.Equal("technical-error-title", section.GetAttribute("aria-labelledby"));
        Assert.Equal("technical-error-title", cut.Find(".technical-error__title").GetAttribute("id"));
    }

    [Fact]
    public void HidesTheIconFromAssistiveTechnology()
    {
        var cut = Render<TechnicalErrorState>();

        Assert.Equal("true", cut.Find(".technical-error__icon").GetAttribute("aria-hidden"));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void ShowsTheRetryButtonOnlyWithARetryCallback(bool hasRetry, int expectedCount)
    {
        var cut = Render<TechnicalErrorState>(parameters =>
        {
            if (hasRetry)
            {
                parameters.Add(error => error.OnRetry, () => { });
            }
        });

        Assert.Equal(expectedCount, cut.FindAll("button.technical-error__retry").Count);
    }

    [Fact]
    public void InvokesTheRetryCallbackExactlyOncePerActivation()
    {
        var invocationCount = 0;
        var cut = Render<TechnicalErrorState>(parameters => parameters
            .Add(error => error.OnRetry, () => invocationCount++));

        cut.Find("button.technical-error__retry").Click();

        Assert.Equal(1, invocationCount);
        Assert.Equal("Erneut versuchen", cut.Find("button.technical-error__retry").TextContent.Trim());
    }
}
