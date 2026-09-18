using Bunit;
using KnowHowToAI.Server.Web.Components.Layout;

namespace KnowHowToAI.Web.Tests.Features.Shell;

[Trait("Category", "Unit")]
public sealed class ShellLayoutTests : BunitContext
{
    [Fact]
    public void ShowsTheBrandAsPureTextWordmarkWithoutLogoAsset()
    {
        var cut = Render<ShellLayout>(parameters => parameters.Add(
            parameter => parameter.Body,
            "<p>Inhalt</p>"));

        var brand = cut.Find(".app-shell-brand");
        Assert.Equal("KnowHowToAI", brand.TextContent);
        Assert.Equal("span", brand.TagName.ToLowerInvariant());
        Assert.Empty(cut.FindAll("img, svg"));
        Assert.Contains("Inhalt", cut.Find("main").TextContent, StringComparison.Ordinal);
    }
}
