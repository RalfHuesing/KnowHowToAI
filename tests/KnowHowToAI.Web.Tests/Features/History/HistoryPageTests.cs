using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.History;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.History;

[Trait("Category", "Unit")]
public sealed class HistoryPageTests : BunitContext
{
    [Fact]
    public async Task HistoryPage_PagesImmutableSnapshotsAndReleases_AndTransfersTheSelectedContext()
    {
        ConfigureServices(pageSize: 1);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/history?roleId=Developer");

        var cut = Render<HistoryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid^='snapshot-select-']"));
            Assert.Single(cut.FindAll("[data-testid^='release-select-']"));
            Assert.Contains("Working Transactions gehören nicht", cut.Find("[data-testid='history-working-separation']").TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-list-next']").Click());
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='snapshot-select-2']")));

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-select-2']").Click());
        Assert.EndsWith("/knowledge?snapshotId=2&roleId=Developer", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);

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

    private void ConfigureServices(int pageSize, bool includeHistory = true, bool includeReleases = true)
    {
        var harness = new NavigationTestHarness(new SnapshotId(3));
        if (includeHistory)
        {
            harness.AddHistoricalSnapshot(new Snapshot(new SnapshotId(2), new SnapshotId(1), SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            harness.AddHistoricalSnapshot(new Snapshot(new SnapshotId(1), null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
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
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 25,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            });

        Services.AddSingleton(historyService);
        Services.AddSingleton(releaseService);
        Services.AddSingleton<PageRegionState>();
    }
}
