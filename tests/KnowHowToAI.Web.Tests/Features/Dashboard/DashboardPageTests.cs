using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Dashboard;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout;
using KnowHowToAI.Server.Web.Features.Dashboard;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Dashboard;

[Trait("Category", "Unit")]
public sealed class DashboardPageTests : Bunit.BunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    public DashboardPageTests()
    {
        Services.AddSingleton(new PageRegionState());
        Services.AddSingleton<IClock>(new FixedClock(Now));
    }

    [Fact]
    public void RendersSemanticHeadingAndKnowledgeContext()
    {
        var harness = new DashboardTestHarness(now: Now);
        Services.AddSingleton(harness.CreateService());

        var cut = Render<DashboardPage>();

        cut.Find("h1").MarkupMatches("<h1>KnowHowToAI</h1>");
        cut.Find(".dashboard-badge").MarkupMatches("<span class=\"dashboard-badge\">Wissensdashboard</span>");

        var state = Services.GetRequiredService<PageRegionState>();
        var context = state.KnowledgeContext;
        Assert.NotNull(context);
        Assert.Equal(KnowledgeReadContextKind.Current, context.ReadContext);
        Assert.Null(context.ContextId);
        Assert.Null(context.RoleName);
        Assert.False(context.IsDirty);
    }

    [Fact]
    public void RendersSnapshotSummaryWithCurrentSnapshotAndRelease()
    {
        var harness = new DashboardTestHarness(now: Now);
        harness.SetLatestRelease(new Release(new ReleaseId(1), new SnapshotId(1), "v1.0.0", "Erster Release", Now.AddDays(-1)));
        Services.AddSingleton(harness.CreateService());

        var cut = Render<DashboardPage>();

        var summary = cut.Find("[data-testid=snapshot-summary]");
        Assert.NotNull(summary);

        Assert.Equal("1", cut.Find("[data-testid=current-snapshot-id]").TextContent);
        Assert.Equal("v1.0.0", cut.Find("[data-testid=latest-release-name]").TextContent);

        var knowledgeLink = cut.Find("[data-testid=link-knowledge]");
        Assert.Equal("/knowledge", knowledgeLink.GetAttribute("href"));

        var historyLink = cut.Find("[data-testid=link-history]");
        Assert.Equal("/history", historyLink.GetAttribute("href"));
    }

    [Fact]
    public void RendersOpenTransactionsWithAgeWarningAndValidationErrors()
    {
        var harness = new DashboardTestHarness(now: Now);
        var txId = new TransactionId(Guid.NewGuid());
        var oldTx = new KnowledgeTransaction(
            txId,
            new SnapshotId(1),
            new SnapshotId(10),
            TransactionState.Open,
            ChangeVersion: 3,
            CreatedAtUtc: Now.AddDays(-10), // > 7 days -> warnbadge
            CommittedAtUtc: null,
            Purpose: "Großer Umbau",
            Actor: "Bob",
            Client: "Desktop",
            CommitMessage: null);

        harness.AddOpenTransaction(oldTx);
        // Set invalid data with a node that has empty title
        var invalidNode = new Node(new SnapshotId(10), new NodeId(Guid.NewGuid()), null, "", null, 1, false);
        harness.SetTransactionValidationData(txId, new WorkingSnapshotValidationData(
            Nodes: [invalidNode],
            Roles: [],
            RoleResolutions: [],
            Contents: [],
            Dependencies: []));

        Services.AddSingleton(harness.CreateService());

        var cut = Render<DashboardPage>();

        var txSection = cut.Find("[data-testid=open-transactions]");
        Assert.NotNull(txSection);

        // Warnbadge check
        var warnBadge = cut.Find("[data-testid=tx-age-warning]");
        Assert.Contains("Älter als 7 Tage", warnBadge.TextContent, StringComparison.Ordinal);

        // Validation errors check
        var errors = cut.Find("[data-testid=tx-validation-errors]");
        Assert.Contains("harte Validierungsfehler", errors.TextContent, StringComparison.Ordinal);

        // Link check
        var link = cut.Find($"[data-testid=tx-link-{txId.Value}]");
        Assert.Equal($"/transactions/{txId.Value}", link.GetAttribute("href"));
    }

    [Fact]
    public void RendersRecentNodeChangesAndDistinguishesDeletedNodes()
    {
        var harness = new DashboardTestHarness(now: Now);
        var baseSnapshotId = new SnapshotId(1);
        var currentSnapshotId = new SnapshotId(2);

        harness.SetCurrentSnapshot(new Snapshot(currentSnapshotId, baseSnapshotId, SnapshotState.Committed, Now, Now));

        var nodeActiveId = new NodeId(Guid.NewGuid());
        var nodeDeletedId = new NodeId(Guid.NewGuid());

        // Base has both nodes
        harness.SetSnapshotNodes(baseSnapshotId, [
            new Node(baseSnapshotId, nodeActiveId, null, "Aktiver Node", null, 1, false),
            new Node(baseSnapshotId, nodeDeletedId, null, "Gelöschter Node", null, 2, false)
        ]);

        // Current has modified active node and deleted node is omitted
        harness.SetSnapshotNodes(currentSnapshotId, [
            new Node(currentSnapshotId, nodeActiveId, null, "Aktiver Node Geändert", null, 1, false)
        ]);

        Services.AddSingleton(harness.CreateService());

        var cut = Render<DashboardPage>();

        var changesSection = cut.Find("[data-testid=recent-changes]");
        Assert.NotNull(changesSection);

        var activeNodeLink = cut.Find($"[data-testid=node-link-{nodeActiveId.Value}]");
        Assert.Equal($"/knowledge/{nodeActiveId.Value}", activeNodeLink.GetAttribute("href"));
        Assert.Equal("Aktiver Node Geändert", activeNodeLink.TextContent);

        var deletedNodeSpan = cut.Find($"[data-testid=node-deleted-{nodeDeletedId.Value}]");
        Assert.Equal("Gelöschter Node", deletedNodeSpan.TextContent);
    }

    [Fact]
    public void RendersEmptyStatesWhenNoReleaseNoTransactionsAndCleanQuality()
    {
        var harness = new DashboardTestHarness(now: Now);
        Services.AddSingleton(harness.CreateService());

        var cut = Render<DashboardPage>();

        Assert.NotNull(cut.Find("[data-testid=no-release-hint]"));
        Assert.NotNull(cut.Find("[data-testid=empty-transactions]"));
        Assert.NotNull(cut.Find("[data-testid=quality-clean]"));
        Assert.NotNull(cut.Find("[data-testid=empty-recent-changes]"));
    }
}
