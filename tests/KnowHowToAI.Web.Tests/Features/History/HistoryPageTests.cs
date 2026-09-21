using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.History;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.History;

[Trait("Category", "Unit")]
public sealed class HistoryPageTests : BunitContext
{
    private static readonly Guid ChangedNodeId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task HistoryPage_PagesImmutableSnapshotsAndReleases_AndTransfersTheSelectedContext()
    {
        ConfigureServices(pageSize: 1);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/history?audienceId=Developer");

        var cut = Render<HistoryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid^='snapshot-select-']"));
            Assert.Single(cut.FindAll("[data-testid^='release-select-']"));
            Assert.Contains("Working Transactions gehören nicht", cut.Find("[data-testid='history-working-separation']").TextContent);
            Assert.Contains("Ausgang", cut.Find("[data-testid='history-selection-status']").TextContent);
            Assert.Equal(2, cut.FindAll(".history-page__selection").Count);
            Assert.Equal(2, cut.FindAll(".history-page__selection strong").Count(element => element.TextContent.Contains("Noch nicht gewählt", StringComparison.Ordinal)));
            Assert.NotEmpty(cut.FindAll(".history-page__technical"));
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-diff-target-3']").Click());
        cut.WaitForAssertion(() => Assert.Contains("Snapshot 3", cut.Find("[data-testid='history-selection-status']").TextContent));
        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-list-next']").Click());
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='snapshot-select-2']"));
            Assert.Contains("Historie prüfen", cut.Find("[data-testid='snapshot-transaction-2']").TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-diff-base-2']").Click());
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Snapshot 2", cut.Find("[data-testid='history-selection-status']").TextContent);
            Assert.Contains("Snapshot 2", cut.Find("[data-testid='snapshot-diff-summary']").TextContent);
            Assert.Contains("Knoten", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
            Assert.Contains("Vorher", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
            Assert.Contains("Nachher", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-diff-next']").Click());
        cut.WaitForAssertion(() => Assert.Contains("Inhalt", cut.Find("[data-testid='snapshot-diff-list']").TextContent));

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-select-2']").Click());
        Assert.EndsWith("/knowledge?snapshotId=2&audienceId=Developer", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);

        await cut.InvokeAsync(() => cut.Find("[data-testid='release-list-next']").Click());
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid^='release-select-']")));
    }

    [Fact]
    public void HistoryPage_ExplainsEmptyReleaseList()
    {
        ConfigureServices(pageSize: 10, includeHistory: false, includeReleases: false);

        var cut = Render<HistoryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("keine Releases", cut.Find("[data-testid='release-list-empty']").TextContent);
        });
    }

    [Fact]
    public void HistoryPage_FiltersSnapshotDiffToNodeFromQuery()
    {
        ConfigureServices(pageSize: 10);
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            $"/history?baseSnapshotId=2&targetSnapshotId=3&nodeId={ChangedNodeId}");

        var cut = Render<HistoryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("gefiltert auf Knoten", cut.Find("[data-testid='snapshot-diff-summary']").TextContent);
            Assert.DoesNotContain("Zielgruppenauflösung", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
        });
    }

    [Fact]
    public void HistoryPage_ExplainsInvalidComparisonQuery()
    {
        ConfigureServices(pageSize: 10);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/history?baseSnapshotId=ungültig");

        var cut = Render<HistoryPage>();

        cut.WaitForAssertion(() => Assert.Contains("ungültig", cut.Find("[data-testid='snapshot-diff-error']").TextContent));
    }

    [Fact]
    public async Task HistoryPage_CreatesReleaseFromExplicitCommittedSnapshot_AndNavigatesToIt()
    {
        var releaseRepository = ConfigureServices(pageSize: 10);
        var cut = Render<HistoryPage>();

        await cut.InvokeAsync(() => cut.Find("[data-testid='create-release-button']").Click());
        var dialog = cut.FindComponent<CreateReleaseDialog>();
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-snapshot']").Change("3"));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-name']").Change("September"));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-description']").Change("Bereit zur Veröffentlichung"));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-submit']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("September", releaseRepository.LastRequest!.Name);
            Assert.Equal(new SnapshotId(3), releaseRepository.LastRequest.SnapshotId);
            Assert.Equal("Bereit zur Veröffentlichung", releaseRepository.LastRequest.Description);
            Assert.Contains("September", cut.Find("[data-testid='release-created']").TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='release-created-navigate']").Click());
        Assert.EndsWith("/knowledge?releaseId=3", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HistoryPage_CreateReleaseWithNameConflict_ShowsServiceError()
    {
        var releaseRepository = ConfigureServices(pageSize: 10);
        releaseRepository.CreateError = new DomainError(
            ReleaseErrorCodes.ReleaseNameConflict,
            "Der Release-Name ist bereits vergeben.");
        var cut = Render<HistoryPage>();

        await FillReleaseDialogAsync(cut, snapshotId: 3, name: "Doppelt");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("bereits vergeben", cut.Find("[data-testid='create-release-error']").TextContent);
            Assert.Equal(2, releaseRepository.ExistingReleases.Count);
        });
    }

    [Fact]
    public async Task HistoryPage_CreateRelease_OffersOnlyCommittedSnapshots_AndRejectsUnknownSelection()
    {
        var releaseRepository = ConfigureServices(pageSize: 10);
        var cut = Render<HistoryPage>();

        await cut.InvokeAsync(() => cut.Find("[data-testid='create-release-button']").Click());
        var dialog = cut.FindComponent<CreateReleaseDialog>();
        Assert.Empty(dialog.FindAll("[data-testid='create-release-snapshot'] option[value='4']"));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-snapshot']").Change("999"));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-name']").Change("Unbekannt"));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-submit']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("committed Snapshot", dialog.Find("[data-testid='create-release-error']").TextContent);
            Assert.Null(releaseRepository.LastRequest);
        });
    }

    [Fact]
    public async Task HistoryPage_CreateReleaseWithFindings_SucceedsAndDisplaysThem()
    {
        ConfigureServices(pageSize: 10, contentSizeWarningBytes: 1);
        var cut = Render<HistoryPage>();

        await FillReleaseDialogAsync(cut, snapshotId: 3, name: "Mit Befunden");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("trotz folgender Qualitätsbefunde", cut.Find("[data-testid='release-created']").TextContent);
            Assert.NotEmpty(cut.FindAll("[data-testid='release-created-findings'] li"));
        });
    }

    private static async Task FillReleaseDialogAsync(IRenderedComponent<HistoryPage> cut, long snapshotId, string name)
    {
        await cut.InvokeAsync(() => cut.Find("[data-testid='create-release-button']").Click());
        var dialog = cut.FindComponent<CreateReleaseDialog>();
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-snapshot']").Change(snapshotId.ToString()));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-name']").Change(name));
        await dialog.InvokeAsync(() => dialog.Find("[data-testid='create-release-submit']").Click());
    }

    private InMemoryReleaseMutationRepository ConfigureServices(
        int pageSize,
        bool includeHistory = true,
        bool includeReleases = true,
        int contentSizeWarningBytes = 4096)
    {
        var harness = new NavigationTestHarness(new SnapshotId(3));
        if (includeHistory)
        {
            harness.AddHistoricalSnapshot(new Snapshot(
                new SnapshotId(2),
                new SnapshotId(1),
                SnapshotState.Committed,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                new SnapshotCommitMetadata(
                    new TransactionId(Guid.Parse("20000000-0000-0000-0000-000000000001")),
                    "Tester",
                    "Web",
                    "Historie prüfen",
                    "History commit")));
            harness.AddHistoricalSnapshot(new Snapshot(new SnapshotId(1), null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            var nodeId = new NodeId(ChangedNodeId);
            harness.AddNode(new Node(new SnapshotId(2), nodeId, null, "Vorher", null, 1, false));
            harness.AddNode(new Node(new SnapshotId(3), nodeId, null, "Nachher", null, 1, false));
            harness.AddContent(new NodeContent(
                new SnapshotId(2),
                nodeId,
                new AudienceId("Developer"),
                new ContentRevisionId(Guid.Parse("20000000-0000-0000-0000-000000000001")),
                ContentMode.Independent,
                "Vorher",
                false));
            harness.AddContent(new NodeContent(
                new SnapshotId(3),
                nodeId,
                new AudienceId("Developer"),
                new ContentRevisionId(Guid.Parse("20000000-0000-0000-0000-000000000002")),
                ContentMode.Independent,
                "Nachher",
                false));
            harness.SetTransaction(new KnowledgeTransaction(
                new TransactionId(Guid.NewGuid()),
                new SnapshotId(3),
                new SnapshotId(4),
                TransactionState.Open,
                1,
                DateTimeOffset.UtcNow,
                null,
                "Test",
                "Tester",
                "Web",
                null));
        }

        var policy = new RetrievalPolicy
        {
            DefaultPageSize = pageSize,
            MaximumPageSize = 10,
            SearchPageSize = 10,
            SearchMaximumPageSize = 10,
            SnippetMaximumCharacters = 100
        };
        var historyService = new HistoryService(harness.CreateRepositories(), policy);
        var releaseRepository = new InMemoryReleaseMutationRepository();
        if (includeReleases)
        {
            releaseRepository.ExistingReleases.Add(new Release(new ReleaseId(1), new SnapshotId(3), "Version 1", null, DateTimeOffset.UtcNow));
            releaseRepository.ExistingReleases.Add(new Release(new ReleaseId(2), new SnapshotId(2), "Version 2", null, DateTimeOffset.UtcNow));
        }

        var releaseService = new ReleaseService(
            harness.CreateRepositories(),
            releaseRepository,
            new SystemClock(),
            policy,
            TestPolicies.DefaultValidation with
            {
                ContentSizeWarningBytes = contentSizeWarningBytes,
                ChildCountWarning = 25
            });

        Services.AddSingleton(historyService);
        Services.AddSingleton(releaseService);
        Services.AddSingleton<PageRegionState>();
        JSInterop.SetupAppDialog();
        return releaseRepository;
    }
}
