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
        Services.GetRequiredService<NavigationManager>().NavigateTo("/history?roleId=Developer");

        var cut = Render<HistoryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid^='snapshot-select-']"));
            Assert.Single(cut.FindAll("[data-testid^='release-select-']"));
            Assert.Contains("Working Transactions gehören nicht", cut.Find("[data-testid='history-working-separation']").TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-diff-target-3']").Click());
        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-list-next']").Click());
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='snapshot-select-2']")));

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-diff-base-2']").Click());
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Knoten", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
            Assert.Contains("Vorher", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
            Assert.Contains("Nachher", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("[data-testid='snapshot-diff-next']").Click());
        cut.WaitForAssertion(() => Assert.Contains("Inhalt", cut.Find("[data-testid='snapshot-diff-list']").TextContent));

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
            Assert.DoesNotContain("Rollenauflösung", cut.Find("[data-testid='snapshot-diff-list']").TextContent);
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

    private void ConfigureServices(int pageSize, bool includeHistory = true, bool includeReleases = true)
    {
        var harness = new NavigationTestHarness(new SnapshotId(3));
        if (includeHistory)
        {
            harness.AddHistoricalSnapshot(new Snapshot(new SnapshotId(2), new SnapshotId(1), SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            harness.AddHistoricalSnapshot(new Snapshot(new SnapshotId(1), null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            var nodeId = new NodeId(ChangedNodeId);
            harness.AddNode(new Node(new SnapshotId(2), nodeId, null, "Vorher", null, 1, false));
            harness.AddNode(new Node(new SnapshotId(3), nodeId, null, "Nachher", null, 1, false));
            harness.AddContent(new NodeContent(
                new SnapshotId(2),
                nodeId,
                new RoleId("Developer"),
                new ContentRevisionId(Guid.Parse("20000000-0000-0000-0000-000000000001")),
                ContentMode.Independent,
                "Vorher",
                false));
            harness.AddContent(new NodeContent(
                new SnapshotId(3),
                nodeId,
                new RoleId("Developer"),
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
