using Bunit;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeDetailsTests : BunitContext
{
    private static NodeDetailsViewModel MakeViewModel(
        string? contentMd = null,
        string availability = "Explicit",
        string freshness = "Current",
        string? contentMode = "Independent",
        bool fallbackUsed = false,
        string? resolvedAudienceId = null)
    {
        var nodeId = Guid.NewGuid();
        return new NodeDetailsViewModel(
            NodeId: nodeId,
            ParentNodeId: null,
            Title: "Test-Knoten",
            Description: "Eine Beschreibung",
            SortOrder: 1,
            RequestedAudienceId: "Developer",
            ResolvedAudienceId: resolvedAudienceId ?? "Developer",
            FallbackUsed: fallbackUsed,
            Availability: availability,
            Freshness: freshness,
            ContentRevisionId: Guid.NewGuid(),
            ContentMode: contentMode,
            ContentMd: contentMd,
            SourceRevisions: [],
            ChangeVersion: null);
    }

    [Fact]
    public void NodeDetails_LoadingState_RendersLoadingIndicator()
    {
        var cut = Render<NodeDetails>(p => p
            .Add(x => x.IsLoading, true));

        // LoadingState-Komponente gerendert; kein Fehler, kein ViewModel
        cut.MarkupMatches(cut.Markup); // kein Crash
        Assert.DoesNotContain("node-details\"", cut.Markup);
    }

    [Fact]
    public void NodeDetails_ErrorMessage_RendersError()
    {
        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ErrorMessage, "Ladefehler"));

        Assert.Contains("node-details-error", cut.Markup);
        Assert.Contains("Ladefehler", cut.Markup);
    }

    [Fact]
    public void NodeDetails_NodeNotFound_RendersNotFound()
    {
        var cut = Render<NodeDetails>(p => p
            .Add(x => x.NodeNotFound, true));

        Assert.Contains("node-details-not-found", cut.Markup);
    }

    [Fact]
    public void NodeDetails_WithViewModel_LeavesPageHeadingToPageFrame()
    {
        var vm = MakeViewModel();

        var cut = Render<NodeDetails>(p => p.Add(x => x.ViewModel, vm));

        Assert.Empty(cut.FindAll("h1"));
        Assert.DoesNotContain("node-details-title", cut.Markup);
    }

    [Fact]
    public void NodeDetails_WithViewModel_RendersMetadata()
    {
        var vm = MakeViewModel();

        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm)
            .Add(x => x.ActiveView, "Technical"));

        var availability = cut.Find("[data-testid='node-details-availability']");
        Assert.Equal("Eigener Inhalt", availability.TextContent.Trim());

        var freshness = cut.Find("[data-testid='node-details-freshness']");
        Assert.Equal("Aktuell", freshness.TextContent.Trim());
        Assert.Empty(cut.FindAll("summary"));

        Assert.Empty(cut.FindAll("[data-testid='node-details-node-id']"));
        Assert.DoesNotContain(vm.NodeId.ToString(), cut.Find("[data-testid='node-details']").TextContent);
    }

    [Fact]
    public void NodeDetails_WithMarkdownContent_RendersRenderedHtml()
    {
        var vm = MakeViewModel(contentMd: "**fett** und _kursiv_");

        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm));

        var contentDiv = cut.Find("[data-testid='node-content-markdown']");
        Assert.Contains("<strong>fett</strong>", contentDiv.InnerHtml);
    }

    [Fact]
    public void NodeDetails_ReadViewShowsContentWithoutTechnicalDetails()
    {
        var vm = MakeViewModel(contentMd: "Lesbarer Inhalt");
        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm));

        Assert.Contains("Lesbarer Inhalt", cut.Find("[data-testid='node-details-content']").TextContent);
        Assert.True(cut.Find("[data-testid='node-view-panel-technical']").HasAttribute("hidden"));
    }

    [Fact]
    public void NodeDetails_WithEmptyContent_ShowsEmptyHint()
    {
        var vm = MakeViewModel(contentMd: null, availability: "None");

        var cut = Render<NodeDetails>(p => p.Add(x => x.ViewModel, vm));

        var emptyDiv = cut.Find("[data-testid='node-content-empty']");
        Assert.NotNull(emptyDiv);
    }

    [Fact]
    public void NodeDetails_WithFallback_ShowsFallbackInAudience()
    {
        var vm = MakeViewModel(
            fallbackUsed: true,
            resolvedAudienceId: "Architect",
            availability: "Fallback");

        var cut = Render<NodeDetails>(p => p.Add(x => x.ViewModel, vm));

        var fallbackContext = cut.Find("[data-testid='node-content-fallback-context']");
        Assert.Contains("kein eigener Inhalt hinterlegt", fallbackContext.TextContent);
        Assert.Contains("Fallback-Zielgruppe „Architect“", fallbackContext.TextContent);
        Assert.Contains("Wissenskontext bleiben unverändert", fallbackContext.TextContent);
        Assert.True(cut.Markup.IndexOf("data-testid=\"node-content-fallback-context\"", StringComparison.Ordinal)
            < cut.Markup.IndexOf("data-testid=\"node-details-content\"", StringComparison.Ordinal));
    }

    [Fact]
    public void NodeDetails_WithFallback_RendersFallbackContext()
    {
        var vm = MakeViewModel(
            contentMd: "Fallback-Inhalt",
            fallbackUsed: true,
            resolvedAudienceId: "Architect",
            availability: "Fallback");
        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm));

        Assert.NotNull(cut.Find("[data-testid='node-content-fallback-context']"));
    }

    [Fact]
    public void NodeDetails_WithoutFallback_DoesNotShowArrow()
    {
        var vm = MakeViewModel(fallbackUsed: false, resolvedAudienceId: "Developer");

        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm)
            .Add(x => x.ActiveView, "Technical"));

        var AudienceElement = cut.Find("[data-testid='node-details-Audience']");
        Assert.DoesNotContain("→", AudienceElement.TextContent);
    }

    [Fact]
    public void NodeDetails_WithSourceRevisions_RendersProvenanceSection()
    {
        var sourceRev = new SourceRevisionViewModel(
            Guid.NewGuid(),
            "Architect",
            Guid.NewGuid(),
            "Stale",
            "Quellknoten mit Titel");

        var vm = MakeViewModel(contentMode: "Derived") with
        {
            SourceRevisions = [sourceRev]
        };

        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm)
            .Add(x => x.ActiveView, "Technical"));

        var provenanceSection = cut.Find("[data-testid='node-details-provenance']");
        Assert.NotNull(provenanceSection);

        var items = cut.FindAll("[data-testid='node-provenance-item']");
        Assert.Single(items);
        Assert.Contains("Quellknoten mit Titel", items[0].TextContent);
        Assert.Contains("Architect", items[0].TextContent);
        Assert.Equal("Quelle: Veraltet", cut.Find("[data-testid='node-provenance-freshness']").TextContent.Trim());
        Assert.DoesNotContain(sourceRev.SourceNodeId.ToString(), items[0].TextContent);
        Assert.DoesNotContain(sourceRev.SourceNodeId.ToString("N")[..8], items[0].TextContent);
    }

    [Fact]
    public void NodeDetails_WithoutSourceRevisions_HidesProvenanceSection()
    {
        var vm = MakeViewModel(contentMode: "Independent");

        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm)
            .Add(x => x.ActiveView, "Technical"));

        var provenance = cut.FindAll("[data-testid='node-details-provenance']");
        Assert.Empty(provenance);
    }

    [Fact]
    public void NodeDetails_StaleFreshness_ShowsWarningBadge()
    {
        var vm = MakeViewModel(freshness: "Stale");

        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm)
            .Add(x => x.ActiveView, "Technical"));

        var freshnessEl = cut.Find("[data-testid='node-details-freshness']");
        Assert.Equal("Veraltet", freshnessEl.TextContent.Trim());
        Assert.Contains("node-meta-badge--stale", freshnessEl.GetAttribute("class") ?? "");
    }

    [Fact]
    public void NodeDetails_DerivedContentMode_ShowsAbgeleitetLabel()
    {
        var vm = MakeViewModel(contentMode: "Derived");

        var cut = Render<NodeDetails>(p => p
            .Add(x => x.ViewModel, vm)
            .Add(x => x.ActiveView, "Technical"));

        var modeEl = cut.Find("[data-testid='node-details-content-mode']");
        Assert.Equal("Abgeleitet", modeEl.TextContent.Trim());
        Assert.True(cut.Markup.IndexOf("data-testid=\"node-content-derived-context\"", StringComparison.Ordinal)
            < cut.Markup.IndexOf("data-testid=\"node-details-content\"", StringComparison.Ordinal));
    }

    [Fact]
    public void NodeDetails_NoViewModel_NoLoadingNoError_RenderNothing()
    {
        var cut = Render<NodeDetails>();

        // Keine sichtbaren Elemente außer der leeren Hülle
        Assert.DoesNotContain("node-details\"", cut.Markup);
        Assert.DoesNotContain("node-details-error", cut.Markup);
        Assert.DoesNotContain("node-details-not-found", cut.Markup);
    }
}
