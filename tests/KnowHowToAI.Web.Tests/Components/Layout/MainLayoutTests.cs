using AngleSharp.Dom;
using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Layout.Shell;
using KnowHowToAI.Server.Web.Components.Shared;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class MainLayoutTests : ShellTestContext
{
    [Fact]
    public void ProvidesSkipLinkBrandTwoGlobalDestinationsAndExactlyOneMain()
    {
        var cut = RenderMainLayout();

        var skipLink = cut.Find("a.shell-skip-link");
        Assert.Equal("#shell-main", skipLink.Attributes["href"]?.Value);
        Assert.Equal("Zum Hauptinhalt springen", skipLink.TextContent);
        Assert.Equal("a", Assert.IsAssignableFrom<IElement>(cut.Find(".shell-root").ChildNodes[0]).TagName.ToLowerInvariant());
        Assert.Equal("KnowHowToAI", cut.Find("a.shell-brand").TextContent);

        var navigation = cut.Find("nav[aria-label='Hauptnavigation']");
        Assert.Equal(new[] { "/knowledge", "/drafts" },
            navigation.QuerySelectorAll("a").Select(link => link.GetAttribute("href")));
        Assert.Equal(new[] { "Wissen", "Entwürfe" },
            navigation.QuerySelectorAll("a").Select(link => link.TextContent.Trim()));
        Assert.Empty(cut.FindAll("aside, .knowledge-context, [data-testid='context-selector-dialog']"));

        var main = Assert.Single(cut.FindAll("main"));
        Assert.Equal("shell-main", main.Id);
        Assert.Equal("-1", main.Attributes["tabindex"]?.Value);
    }

    [Fact]
    public void HostsTheFeaturePageRootAndItsSingleHeadingInsideTheOnlyMain()
    {
        var cut = Render<MainLayout>(parameters => parameters.Add(parameter => parameter.Body, builder =>
        {
            builder.OpenComponent<PageFrame>(0);
            builder.AddAttribute(1, nameof(PageFrame.Title), "Wissensbasis");
            builder.AddAttribute(2, nameof(PageFrame.ChildContent), (RenderFragment)(content =>
                content.AddMarkupContent(0, "<p>Arbeitsinhalt</p>")));
            builder.CloseComponent();
        }));

        var main = Assert.Single(cut.FindAll("main"));
        Assert.Equal("shell-main", main.Id);
        Assert.Equal("Wissensbasis", Assert.Single(main.QuerySelectorAll("h1")).TextContent);
        Assert.Equal(0, main.QuerySelectorAll("main").Length);
        Assert.NotNull(main.QuerySelector(".page-frame"));
    }

    [Fact]
    public void DesktopNavigationCanBeClosedAndReopened()
    {
        var cut = RenderMainLayout();
        var toggle = cut.Find("button[aria-controls='shell-navigation']");

        Assert.Equal("true", toggle.Attributes["aria-expanded"]?.Value);
        Assert.Single(cut.FindAll("nav[aria-label='Hauptnavigation']"));

        toggle.Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 0);
        Assert.Equal("Navigation einblenden", toggle.Attributes["aria-label"]?.Value);

        toggle.Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 1);
        Assert.Equal("Navigation ausblenden", toggle.Attributes["aria-label"]?.Value);
    }

    [Fact]
    public async Task CompactNavigationOpensWithFocusAndEscapeReturnsFocusToTheToggle()
    {
        var cut = RenderMainLayout();
        await cut.Instance.NotifyCompactModeChangedAsync(isCompact: true);
        var toggle = cut.Find("button[aria-controls='shell-navigation']");

        Assert.Equal("Navigation einblenden", toggle.Attributes["aria-label"]?.Value);
        Assert.Empty(cut.FindAll("nav[aria-label='Hauptnavigation']"));

        toggle.Click();
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 1);
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 1);

        cut.Find("nav#shell-navigation").KeyDown("Escape");
        cut.WaitForState(() => cut.FindAll("nav[aria-label='Hauptnavigation']").Count == 0);
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
        Assert.Equal("Navigation einblenden", toggle.Attributes["aria-label"]?.Value);
    }

    [Fact]
    public void SelectedTransactionIsVisibleAndLinksDirectlyToItsDraftDetail()
    {
        var transactionId = new TransactionId(Guid.NewGuid());
        Services.GetRequiredService<WorkspaceState>().SetContext(
            new KnowHowToAI.Server.Web.State.KnowledgeContextViewModel(
                KnowHowToAI.Server.Web.State.KnowledgeReadContextKind.Current),
            new ReadContext(TransactionId: transactionId));

        var cut = RenderMainLayout();
        var link = cut.Find("a[data-testid='active-draft-link']");

        Assert.Equal($"/drafts/{transactionId.Value:D}", link.GetAttribute("href"));
        Assert.Equal("Entwurf öffnen", link.TextContent.Trim());
    }

    [Fact]
    public void BreadcrumbAndActionsRemainAvailableWithoutAddingAnotherMain()
    {
        var cut = RenderMainLayoutWithAttachPage();

        Assert.Equal("Start", cut.Find("nav[aria-label='Breadcrumbs']").TextContent.Trim());
        Assert.Equal("Testaktion", cut.Find(".shell-page-actions button").TextContent.Trim());
        Assert.Single(cut.FindAll("main"));
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

    private sealed class RegionAttachingPage : ComponentBase
    {
        [Inject]
        private PageRegionState PageRegions { get; set; } = default!;

        protected override void OnInitialized()
        {
            PageRegions.SetBreadcrumbs(builder => builder.AddContent(0, "Start"));
            PageRegions.SetActions(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddContent(2, "Testaktion");
                builder.CloseElement();
            });
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddContent(0, "Seiteninhalt");
    }
}
