using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Search;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Search;

[Trait("Category", "Unit")]
public sealed class SearchPageTests : BunitContext
{
    private static readonly SnapshotId SnapshotId = new(1);
    private static readonly AudienceId AudienceId = new("Developer");

    private sealed class CancellableRetrievalRepository : IRetrievalRepository
    {
        private readonly SearchHit _completedHit;

        public CancellableRetrievalRepository(SearchHit completedHit) => _completedHit = completedHit;

        public TaskCompletionSource<bool> FirstRequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool FirstRequestCanceled { get; private set; }

        public int CallCount { get; private set; }

        public async Task<Result<SearchRepositoryResult>> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (CallCount == 1)
            {
                FirstRequestStarted.TrySetResult(true);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    FirstRequestCanceled = true;
                    throw;
                }
            }

            var audiences = new[] { new Audience(SnapshotId, AudienceId, AudienceId.Value, null, false) };
            var resolutions = new[] { new AudienceResolution(SnapshotId, AudienceId, AudienceId, 1) };
            return Result<SearchRepositoryResult>.Success(new SearchRepositoryResult([_completedHit], Audiences: audiences, Resolutions: resolutions));
        }
    }

    [Fact]
    public async Task SearchPage_FindsTodoContent_ShowsSnippetAndBreadcrumb_AndNavigatesToNode()
    {
        var setup = ConfigureSearch(searchPageSize: 10);
        var rootId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var nodeId = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        setup.Harness.AddNode(new Node(SnapshotId, rootId, null, "Wissensbasis", null, 0, false));
        setup.Harness.AddNode(new Node(SnapshotId, nodeId, rootId, "Betrieb", null, 0, false));
        setup.Repository.ResultsToReturn =
        [
            new SearchHit(nodeId, "Betrieb", null, "...offenes TODO im Inhalt...", "Content",
                Availability.Explicit, AudienceId, Freshness.Current)
        ];

        var cut = Render<SearchPage>();
        Assert.Equal("Wissenssuche", cut.Find("h1").TextContent.Trim());
        Assert.Single(cut.FindAll("h1"));
        var input = cut.Find("[data-testid='search-text']");
        await cut.InvokeAsync(() => input.Change("TODO"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-submit']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("offenes TODO", cut.Find("[data-testid='search-results']").TextContent);
            Assert.Contains("Wissensbasis › Betrieb", cut.Find("[data-testid='search-results']").TextContent);
        });

        await cut.InvokeAsync(() => cut.Find($"[data-testid='search-result-{nodeId.Value}']").Click());

        Assert.Contains($"/knowledge/{nodeId.Value}", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Equal("TODO", setup.Repository.LastRequest!.Text);
        Assert.Equal(AudienceId, setup.Repository.LastRequest.AudienceId);
    }

    [Fact]
    public async Task SearchPage_LoadsNextPage_WithTheUnchangedOpaqueCursor()
    {
        var setup = ConfigureSearch(searchPageSize: 1);
        var rootId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000003"));
        setup.Harness.AddNode(new Node(SnapshotId, rootId, null, "Root", null, 0, false));
        setup.Repository.ResultsToReturn =
        [
            new SearchHit(rootId, "TODO eins", null, "TODO", "Content", Availability.Explicit, AudienceId, Freshness.Current, 1),
            new SearchHit(new NodeId(Guid.Parse("40000000-0000-0000-0000-000000000004")), "TODO zwei", null, "TODO", "Content", Availability.Explicit, AudienceId, Freshness.Current, 2)
        ];

        var cut = Render<SearchPage>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-text']").Change("TODO"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-submit']").Click());
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='search-next']")));

        await cut.InvokeAsync(() => cut.Find("[data-testid='search-next']").Click());

        cut.WaitForAssertion(() => Assert.Equal(2, setup.Repository.CallCount));
        Assert.False(string.IsNullOrWhiteSpace(setup.Repository.LastRequest!.Cursor));
    }

    [Fact]
    public void SearchResults_ExplainNoHitsAndInvalidCursorError()
    {
        var empty = Render<SearchResults>(parameters => parameters
            .Add(component => component.Page, new SearchPageViewModel("TODO", [], null)));
        Assert.Contains("Keine Treffer", empty.Find("[data-testid='search-empty']").TextContent);

        var failed = Render<SearchResults>(parameters => parameters
            .Add(component => component.ErrorMessage, "Der Cursor ist ungültig oder abgelaufen."));
        Assert.Contains("Cursor", failed.Find("[data-testid='search-error']").TextContent);
    }

    [Fact]
    public void KnowledgeFilter_UsesGermanGroupLabels_AndKeepsFilterContracts()
    {
        var cut = Render<KnowledgeFilter>();

        var labels = cut.FindAll(".knowledge-filter__label").Select(label => label.TextContent.Trim()).ToArray();

        Assert.Contains("Aktualität", labels);
        Assert.Contains("Befunde", labels);
        Assert.DoesNotContain("Freshness", labels);
        Assert.DoesNotContain("Findings", labels);
        Assert.NotEmpty(cut.FindAll("[data-testid='filter-freshness-current']"));
        Assert.NotEmpty(cut.FindAll("[data-testid='filter-finding-stale']"));
    }

    [Fact]
    public async Task SearchPage_FilterChange_ClearsOldCursorAndRequestsServerSideFilter()
    {
        var setup = ConfigureSearch(searchPageSize: 10);
        var rootId = new NodeId(Guid.Parse("60000000-0000-0000-0000-000000000006"));
        setup.Harness.AddNode(new Node(SnapshotId, rootId, null, "Root", null, 0, false));
        var fallbackNodeId = new NodeId(Guid.Parse("70000000-0000-0000-0000-000000000007"));
        setup.Harness.AddNode(new Node(SnapshotId, fallbackNodeId, rootId, "Fallback Treffer", null, 1, false));
        setup.Repository.ResultsToReturn =
        [
            new SearchHit(rootId, "Eigener Treffer", null, "TODO", "Content", Availability.Explicit, AudienceId, Freshness.Current, 1),
            new SearchHit(fallbackNodeId, "Fallback Treffer", null, "TODO", "Content", Availability.Fallback, AudienceId, Freshness.Current, 2)
        ];

        var cut = Render<SearchPage>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-text']").Change("TODO"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-submit']").Click());
        cut.WaitForAssertion(() => Assert.Contains("Eigener Treffer", cut.Find("[data-testid='search-results']").TextContent));

        await cut.InvokeAsync(() => cut.Find("[data-testid='filter-availability-fallback']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Fallback Treffer", cut.Find("[data-testid='search-results']").TextContent);
            Assert.DoesNotContain("Eigener Treffer", cut.Find("[data-testid='search-results']").TextContent);
            Assert.Null(setup.Repository.LastRequest!.Cursor);
            Assert.Equal([Availability.Fallback], setup.Repository.LastRequest.Filter!.Availabilities);
        });
    }

    [Fact]
    public void SearchPage_ContextSwitch_ClearsFeatureLocalResultsAndUsesTheNewReadContext()
    {
        var setup = ConfigureSearch(searchPageSize: 10);
        var historicalSnapshotId = new SnapshotId(2);
        setup.Harness.AddHistoricalSnapshot(new Snapshot(
            historicalSnapshotId,
            SnapshotId,
            SnapshotState.Committed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        Services.GetRequiredService<NavigationManager>().NavigateTo($"/search?snapshotId={historicalSnapshotId.Value}&audienceId={AudienceId.Value}");
        var cut = Render<SearchPage>();

        Assert.Equal(historicalSnapshotId, Services.GetRequiredService<WorkspaceState>().CurrentReadContext.SnapshotId);
        Assert.Empty(cut.FindAll("[data-testid='search-results']"));
        Assert.Empty(cut.FindAll("[data-testid='search-empty']"));
    }

    [Fact]
    public async Task SearchPage_AbortsAnOvertakenSearch_WithoutShowingAnError()
    {
        var harness = new NavigationTestHarness(SnapshotId);
        var rootId = new NodeId(Guid.Parse("50000000-0000-0000-0000-000000000005"));
        harness.AddNode(new Node(SnapshotId, rootId, null, "Root", null, 0, false));
        var repository = new CancellableRetrievalRepository(
            new SearchHit(rootId, "TODO aktuell", null, "TODO", "Content", Availability.Explicit, AudienceId, Freshness.Current));
        RegisterPageServices(harness.CreateService(), harness.CreateSearchService(repository));

        var cut = Render<SearchPage>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-text']").Change("erste TODO-Suche"));
        var firstSearch = cut.InvokeAsync(() => cut.Find("[data-testid='search-submit']").Click());
        await repository.FirstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => cut.Find("[data-testid='search-text']").Change("zweite TODO-Suche"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-submit']").Click());
        await firstSearch;

        cut.WaitForAssertion(() =>
        {
            Assert.True(repository.FirstRequestCanceled);
            Assert.Equal(2, repository.CallCount);
            Assert.Empty(cut.FindAll("[data-testid='search-error']"));
            Assert.Contains("TODO aktuell", cut.Find("[data-testid='search-results']").TextContent);
        });
    }

    private (NavigationTestHarness Harness, InMemoryRetrievalRepository Repository) ConfigureSearch(int searchPageSize)
    {
        var harness = new NavigationTestHarness(SnapshotId);
        var navigationService = harness.CreateService();
        var repository = new InMemoryRetrievalRepository(SnapshotId);
        repository.ConfigureActiveAudience(AudienceId);
        var searchService = harness.CreateSearchService(repository, searchPageSize);

        RegisterPageServices(navigationService, searchService);

        return (harness, repository);
    }

    private void RegisterPageServices(NavigationService navigationService, SearchService searchService)
    {
        Services.AddWebPageStates()
            .AddSearchPageServices(navigationService, searchService, defaultAudience: AudienceId.Value);
    }

    [Fact]
    public void ExplicitlyInvalidAudience_DoesNotFallBackToStoredAudience()
    {
        ConfigureSearch(searchPageSize: 10);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/search?audienceId=Missing");

        var cut = Render<SearchPage>();

        Assert.Contains("RequestedAudienceNotFound", cut.Find("[data-testid='search-context-error']").TextContent);
        Assert.True(Services.GetRequiredService<ContextSelectorState>().IsOpen);
    }
}
