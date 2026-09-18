using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;

namespace KnowHowToAI.Web.Tests.Features.Shared;

[Trait("Category", "Unit")]
public sealed class AppStatusTests : BunitContext
{
    [Theory]
    [InlineData(AppStatusKind.Neutral, "Neutral")]
    [InlineData(AppStatusKind.Aktiv, "Aktiv")]
    [InlineData(AppStatusKind.Erfolg, "Erfolg")]
    [InlineData(AppStatusKind.Warnung, "Warnung")]
    [InlineData(AppStatusKind.Fehler, "Fehler")]
    [InlineData(AppStatusKind.Ungespeichert, "Ungespeichert")]
    public void RendersTextWithHiddenIconForEveryStatusKind(AppStatusKind kind, string expectedClassSuffix)
    {
        var cut = Render<AppStatus>(parameters => parameters
            .Add(parameter => parameter.Kind, kind)
            .Add(parameter => parameter.Text, kind.ToString()));

        var root = cut.Find(".app-status");
        Assert.Contains($"app-status--{expectedClassSuffix.ToLowerInvariant()}", root.GetAttribute("class"), StringComparison.Ordinal);

        var icon = cut.Find(".app-status__icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));

        var text = cut.Find(".app-status__text");
        Assert.Equal(kind.ToString(), text.TextContent);
        Assert.Equal("svg", icon.TagName.ToLowerInvariant());
    }

    [Fact]
    public void ShowsTheUnsavedStateWithItsOwnTextAndIcon()
    {
        var cut = Render<AppStatus>(parameters => parameters
            .Add(parameter => parameter.Kind, AppStatusKind.Ungespeichert)
            .Add(parameter => parameter.Text, "Ungespeicherte Änderungen"));

        Assert.Contains(
            "Ungespeicherte Änderungen",
            cut.Find(".app-status--ungespeichert").TextContent,
            StringComparison.Ordinal);
    }
}
