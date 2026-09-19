using AngleSharp.Dom;
using Bunit;
using KnowHowToAI.Server.Web.Components.Layout.Shell;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using KnowHowToAI.Web.Tests.TestSupport;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class MainLayoutTests : ShellTestContext
{
    [Fact]
    public void ShowsTheBrandAsPureTextWordmarkWithoutLogoAsset()
    {
        var cut = RenderMainLayout();

        var brand = cut.Find(".shell-brand");
        Assert.Equal("KnowHowToAI", brand.TextContent);
        Assert.Equal("span", brand.TagName.ToLowerInvariant());
        Assert.Empty(cut.FindAll("img, svg"));
        Assert.Contains("Inhalt", cut.Find("main").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ProvidesSkipLinkHeaderNavAndExactlyOneMainAsNamedLandmarks()
    {
        var cut = RenderMainLayout();

        var skipLink = cut.Find("a.shell-skip-link");
        Assert.Equal("#shell-main", skipLink.Attributes["href"]?.Value);
        Assert.Equal("Zum Hauptinhalt springen", skipLink.TextContent);
        var firstShellChild = Assert.IsAssignableFrom<IElement>(cut.Find(".shell-root").ChildNodes[0]);
        Assert.Equal("a", firstShellChild.TagName.ToLowerInvariant());

        Assert.NotNull(cut.Find("header"));
        var navigation = cut.Find("nav[aria-label='Hauptnavigation']");
        var startLink = navigation.QuerySelector("a[href='/']");
        Assert.NotNull(startLink);
        Assert.Equal("Start", startLink!.TextContent);

        var mainAreas = cut.FindAll("main");
        var main = Assert.Single(mainAreas);
        Assert.Equal("shell-main", main.Id);
        Assert.Equal("-1", main.Attributes["tabindex"]?.Value);

        Assert.Empty(cut.FindAll("aside"));
    }

    [Fact]
    public void EmptyRegionsRenderNothingInsteadOfDummyContent()
    {
        var cut = RenderMainLayout();

        Assert.Empty(cut.FindAll("nav[aria-label='Breadcrumbs']"));
        Assert.Empty(cut.FindAll(".shell-page-actions"));
        Assert.Empty(cut.FindAll("aside"));
        Assert.DoesNotContain("Kontext", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WideModeShowsSideRegionsSideBySideWithoutToggleButtons()
    {
        var cut = RenderMainLayoutWithAttachPage();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("aside")));

        Assert.Single(cut.FindAll("nav[aria-label='Hauptnavigation']"));
        Assert.Single(cut.FindAll("nav[aria-label='Breadcrumbs']"));
        Assert.Single(cut.FindAll(".shell-page-actions"));
        Assert.Empty(cut.FindAll(".shell-toggle"));
    }

    [Fact]
    public void FeaturePagesAttachBreadcrumbsActionsAndContextWithoutKnowingThePageGrid()
    {
        var cut = RenderMainLayoutWithAttachPage();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("aside")));

        Assert.Equal("Start", cut.Find("nav[aria-label='Breadcrumbs']").TextContent.Trim());
        var actionButton = cut.Find(".shell-page-actions button");
        Assert.Equal("Testaktion", actionButton.TextContent);
        Assert.Contains("Kontextinhalt", cut.Find("aside").TextContent, StringComparison.Ordinal);
        Assert.Contains("Seiteninhalt", cut.Find("main").TextContent, StringComparison.Ordinal);

        actionButton.Click();
        var attachingPage = cut.FindComponent<RegionAttachingPage>().Instance;
        Assert.True(attachingPage.WasActionTriggered);
    }

    [Fact]
    public void SwitchingTheBodyResetsAttachedRegions()
    {
        var cut = RenderMainLayoutWithAttachPage();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("aside")));
        cut.Render(parameters => parameters.Add(
            parameter => parameter.Body,
            "<p>Andere Seite</p>"));

        Assert.Empty(cut.FindAll("nav[aria-label='Breadcrumbs']"));
        Assert.Empty(cut.FindAll(".shell-page-actions"));
        Assert.Empty(cut.FindAll("aside"));
        Assert.Empty(cut.FindAll(".knowledge-context"));
    }

    [Fact]
    public void RendersTheKnowledgeContextBarExactlyOnceGloballyNearTheWordmark()
    {
        var cut = RenderMainLayoutWithAttachPage();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".knowledge-context")));

        var bar = cut.Find(".shell-header-brand .knowledge-context");
        Assert.Contains("Current Snapshot", bar.TextContent, StringComparison.Ordinal);
        Assert.Contains("Keine Rolle ausgewählt", bar.TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".app-status--ungespeichert"));
        Assert.Single(cut.FindAll(".knowledge-context"));
    }

    [Fact]
    public async Task CompactModeCollapsesRegionsBehindLabeledToggles()
    {
        var cut = RenderMainLayoutWithAttachPage();
        await SwitchToCompactModeAsync(cut);

        cut.WaitForState(() => cut.FindAll("button[aria-controls='shell-navigation']").Count == 1);
        var navigationToggle = cut.Find("button[aria-controls='shell-navigation']");
        var contextToggle = cut.Find("button[aria-controls='shell-context']");
        Assert.Equal("Navigation einblenden", navigationToggle.TextContent);
        Assert.Equal("false", navigationToggle.Attributes["aria-expanded"]?.Value);
        Assert.Equal("Kontext einblenden", contextToggle.TextContent);
        Assert.Equal("false", contextToggle.Attributes["aria-expanded"]?.Value);
        Assert.Empty(cut.FindAll("nav[aria-label='Hauptnavigation']"));
        Assert.Empty(cut.FindAll("aside"));

        navigationToggle.Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 1);
        Assert.Equal("Navigation ausblenden", navigationToggle.TextContent);
        Assert.Equal("true", navigationToggle.Attributes["aria-expanded"]?.Value);
        Assert.Single(cut.FindAll("a[href='/']"));

        navigationToggle.Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 0);
        Assert.Equal("Navigation einblenden", navigationToggle.TextContent);

        contextToggle.Click();
        cut.WaitForState(() => cut.FindAll("aside").Count == 1);
        Assert.Equal("Kontext ausblenden", contextToggle.TextContent);

        contextToggle.Click();
        cut.WaitForState(() => cut.FindAll("aside").Count == 0);
        Assert.Equal("Kontext einblenden", contextToggle.TextContent);
    }

    [Fact]
    public async Task OpeningARegionHandsFocusToItsTitleAndClosingReturnsItToTheTrigger()
    {
        var cut = RenderMainLayout();
        await SwitchToCompactModeAsync(cut);

        var navigationToggle = cut.Find("button[aria-controls='shell-navigation']");
        navigationToggle.Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 1);
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 1);

        cut.Find("#shell-navigation .shell-panel-close").Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 0);
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
    }

    [Fact]
    public async Task EscapeClosesOnlyTheLastOpenedRegion()
    {
        var cut = RenderMainLayoutWithAttachPage();
        await SwitchToCompactModeAsync(cut);

        cut.Find("button[aria-controls='shell-navigation']").Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 1);
        cut.Find("button[aria-controls='shell-context']").Click();
        cut.WaitForState(() => cut.FindAll("aside").Count == 1);

        cut.Find("aside#shell-context").KeyDown("Escape");
        cut.WaitForState(() => cut.FindAll("aside").Count == 0);
        Assert.Single(cut.FindAll("nav[aria-label='Hauptnavigation']"));
        Assert.Equal("true", cut.Find("button[aria-controls='shell-navigation']")
            .Attributes["aria-expanded"]?.Value);
        Assert.Equal("false", cut.Find("button[aria-controls='shell-context']")
            .Attributes["aria-expanded"]?.Value);

        cut.Find("nav#shell-navigation").KeyDown("Escape");
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 0);
        Assert.Empty(cut.FindAll("aside"));
    }

    private IRenderedComponent<MainLayout> RenderMainLayout(string body = "<p>Inhalt</p>") =>
        Render<MainLayout>(parameters => parameters.Add(parameter => parameter.Body, body));

    private IRenderedComponent<MainLayout> RenderMainLayoutWithAttachPage() =>
        Render<MainLayout>(parameters => parameters.Add(
            parameter => parameter.Body,
            builder =>
            {
                builder.OpenComponent<RegionAttachingPage>(0);
                builder.CloseComponent();
            }));

    private static async Task SwitchToCompactModeAsync(IRenderedComponent<MainLayout> cut) =>
        await cut.Instance.NotifyCompactModeChangedAsync(isCompact: true);

    private sealed class RegionAttachingPage : ComponentBase
    {
        [Inject]
        private PageRegionState PageRegions { get; set; } = default!;

        public bool WasActionTriggered { get; private set; }

        protected override void OnInitialized()
        {
            PageRegions.SetBreadcrumbs(builder => builder.AddContent(0, "Start"));
            PageRegions.SetContext(builder => builder.AddContent(0, "Kontextinhalt"));
            PageRegions.SetKnowledgeContext(
                new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));
            PageRegions.SetActions(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "onclick",
                    EventCallback.Factory.Create(this, () => WasActionTriggered = true));
                builder.AddContent(3, "Testaktion");
                builder.CloseElement();
            });
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddContent(0, "Seiteninhalt");
    }
}
