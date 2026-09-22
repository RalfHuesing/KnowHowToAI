using AngleSharp.Dom;
using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class PageFrameTests : BunitContext
{
    [Fact]
    public void RendersOnePageRootWithRequiredTitleAndContentContract()
    {
        var cut = RenderPageFrame();

        var root = cut.Find(".page-frame--shared");
        Assert.Equal("article", root.GetAttribute("data-region"));
        Assert.Equal("Wissensbasis", cut.Find("h1").TextContent);
        Assert.Single(cut.FindAll("h1"));
        Assert.Contains("Seiteninhalt", cut.Find(".page-frame__content").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("main"));
    }

    [Fact]
    public void RendersDescriptionBadgesActionsAndAdditionalAttributesWhenProvided()
    {
        var cut = Render<PageFrame>(parameters => parameters
            .Add(p => p.Title, "Suche")
            .Add(p => p.Description, "Durchsucht die Wissensbasis.")
            .Add(p => p.Badges, RenderFragment("<span data-testid='badge'>Bereit</span>"))
            .Add(p => p.Actions, RenderFragment("<button type='button' data-testid='action'>Suchen</button>"))
            .AddUnmatched("data-region", "article")
            .Add(p => p.ChildContent, RenderFragment("<p>Ergebnisse</p>")));

        Assert.Equal("Durchsucht die Wissensbasis.", cut.Find(".page-frame__description").TextContent);
        Assert.Equal("Bereit", cut.Find("[data-testid=badge]").TextContent);
        Assert.Equal("Suchen", cut.Find("[data-testid=action]").TextContent);
        Assert.Equal("article", cut.Find(".page-frame--shared").GetAttribute("data-region"));
    }

    [Fact]
    public void OmitsOptionalHeaderRegionsWhenNoValuesAreProvided()
    {
        var cut = RenderPageFrame(description: " ");

        Assert.Empty(cut.FindAll(".page-frame__description"));
        Assert.Empty(cut.FindAll(".page-frame__badges"));
        Assert.Empty(cut.FindAll(".page-frame__actions"));
        Assert.Single(cut.FindAll("h1"));
    }

    private IRenderedComponent<PageFrame> RenderPageFrame(string description = "") =>
        Render<PageFrame>(parameters => parameters
            .Add(p => p.Title, "Wissensbasis")
            .Add(p => p.Description, description)
            .AddUnmatched("data-region", "article")
            .Add(p => p.ChildContent, RenderFragment("<p>Seiteninhalt</p>")));

    private static RenderFragment RenderFragment(string markup) => builder =>
    {
        builder.AddMarkupContent(0, markup);
    };
}
