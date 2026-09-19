using Bunit;
using KnowHowToAI.Server.Web.Components.Shared.Feedback;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class InlineAlertTests : BunitContext
{
    [Theory]
    [InlineData(AlertKind.Info, "app-status--aktiv")]
    [InlineData(AlertKind.Erfolg, "app-status--erfolg")]
    [InlineData(AlertKind.Warnung, "app-status--warnung")]
    [InlineData(AlertKind.Fehler, "app-status--fehler")]
    public void ShowsEveryAlertLevelAsIconPlusTextInsteadOfColorOnly(
        AlertKind kind, string expectedStatusClass)
    {
        var cut = Render<InlineAlert>(parameters => parameters
            .Add(alert => alert.Kind, kind)
            .Add(alert => alert.Message, "Der Name ist ein Pflichtfeld."));

        var status = cut.Find(".inline-alert .app-status");
        Assert.Contains(expectedStatusClass, status.GetAttribute("class")!, StringComparison.Ordinal);
        Assert.Equal("true", cut.Find(".inline-alert .app-status__icon").GetAttribute("aria-hidden"));
        Assert.Equal(
            "Der Name ist ein Pflichtfeld.",
            cut.Find(".inline-alert .app-status__text").TextContent);
    }

    [Theory]
    [InlineData(AlertKind.Fehler, "alert")]
    [InlineData(AlertKind.Info, "status")]
    [InlineData(AlertKind.Erfolg, "status")]
    [InlineData(AlertKind.Warnung, "status")]
    public void AnnouncesErrorsImmediatelyAndAllOtherLevelsPolitely(
        AlertKind kind, string expectedRole)
    {
        var cut = Render<InlineAlert>(parameters => parameters
            .Add(alert => alert.Kind, kind)
            .Add(alert => alert.Message, "Der Name ist ein Pflichtfeld."));

        Assert.Equal(expectedRole, cut.Find(".inline-alert").GetAttribute("role"));
    }
}
