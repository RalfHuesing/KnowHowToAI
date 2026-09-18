using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class StatusBannerTests : BunitContext
{
    [Theory]
    [InlineData(AlertKind.Info, "app-status--aktiv", "status-banner--info")]
    [InlineData(AlertKind.Erfolg, "app-status--erfolg", "status-banner--erfolg")]
    [InlineData(AlertKind.Warnung, "app-status--warnung", "status-banner--warnung")]
    [InlineData(AlertKind.Fehler, "app-status--fehler", "status-banner--fehler")]
    public void ShowsEveryAlertLevelAsIconPlusTextInsteadOfColorOnly(
        AlertKind kind, string expectedStatusClass, string expectedBannerClass)
    {
        var cut = Render<StatusBanner>(parameters => parameters
            .Add(banner => banner.Kind, kind)
            .Add(banner => banner.Message, "Die Verbindung zur Datenbank ist derzeit eingeschränkt."));

        var banner = cut.Find(".status-banner");
        Assert.Contains(expectedBannerClass, banner.GetAttribute("class")!, StringComparison.Ordinal);
        Assert.Equal("true", cut.Find(".status-banner .app-status__icon").GetAttribute("aria-hidden"));
        Assert.Contains(
            expectedStatusClass,
            cut.Find(".status-banner .app-status").GetAttribute("class")!,
            StringComparison.Ordinal);
        Assert.Contains(
            "Die Verbindung zur Datenbank ist derzeit eingeschränkt.",
            cut.Find(".status-banner .app-status__text").TextContent,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AlertKind.Fehler, "alert")]
    [InlineData(AlertKind.Info, "status")]
    [InlineData(AlertKind.Erfolg, "status")]
    [InlineData(AlertKind.Warnung, "status")]
    public void AnnouncesErrorsImmediatelyAndAllOtherLevelsPolitely(
        AlertKind kind, string expectedRole)
    {
        var cut = Render<StatusBanner>(parameters => parameters
            .Add(banner => banner.Kind, kind)
            .Add(banner => banner.Message, "Die Verbindung zur Datenbank ist derzeit eingeschränkt."));

        Assert.Equal(expectedRole, cut.Find(".status-banner").GetAttribute("role"));
    }
}
