using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class WorkingIndicatorTests : BunitContext
{
    [Fact]
    public void ShowsTheWorkingStateAsTextPlusIconInsteadOfColorOnly()
    {
        var cut = Render<WorkingIndicator>(parameters => parameters
            .Add(indicator => indicator.IsWorking, true)
            .Add(indicator => indicator.Text, "Änderungen werden übernommen …"));

        var indicator = cut.Find(".working-indicator");
        Assert.Equal("status", indicator.GetAttribute("role"));
        Assert.Equal("polite", indicator.GetAttribute("aria-live"));
        Assert.Equal("true", cut.Find(".working-indicator__icon").GetAttribute("aria-hidden"));
        Assert.Equal(
            "Änderungen werden übernommen …",
            cut.Find(".working-indicator__text").TextContent);
    }

    [Fact]
    public void RendersNothingWhileIdle()
    {
        var cut = Render<WorkingIndicator>(parameters => parameters
            .Add(indicator => indicator.IsWorking, false)
            .Add(indicator => indicator.Text, "Änderungen werden übernommen …"));

        Assert.Empty(cut.FindAll(".working-indicator"));
    }
}
