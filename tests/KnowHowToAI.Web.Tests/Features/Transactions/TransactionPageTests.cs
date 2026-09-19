using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Transactions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Transactions;

[Trait("Category", "Unit")]
public sealed class TransactionPageTests : BunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly TransactionTestHarness _harness;
    private readonly PageRegionState _pageRegionState = new();
    private readonly WorkspaceState _workspaceState = new();

    public TransactionPageTests()
    {
        _harness = new TransactionTestHarness(now: Now);
        Services.AddSingleton(_pageRegionState);
        Services.AddSingleton(_workspaceState);
        Services.AddSingleton<IClock>(new FixedClock(Now));
        Services.AddSingleton(_harness.CreateService());
    }

    [Fact]
    public void TransactionPage_ValidTransaction_RendersDetailsAndSetsWorkingContext()
    {
        var txId = new TransactionId(Guid.NewGuid());
        var tx = new KnowledgeTransaction(
            txId,
            new SnapshotId(1),
            new SnapshotId(10),
            TransactionState.Open,
            ChangeVersion: 3,
            CreatedAtUtc: Now.AddHours(-1),
            CommittedAtUtc: null,
            Purpose: "Glossar Überarbeitung",
            Actor: "Alice",
            Client: "Web UI",
            CommitMessage: null);

        _harness.AddTransaction(tx);
        _workspaceState.SetRole("Architekt");

        var cut = Render<TransactionPage>(parameters => parameters
            .Add(p => p.TransactionId, txId.Value));

        // Header gerendert
        var header = cut.Find("[data-testid='transaction-header']");
        Assert.NotNull(header);
        Assert.Equal("Glossar Überarbeitung", cut.Find("[data-testid='tx-title']").TextContent.Trim());
        Assert.Equal("Open", cut.Find("[data-testid='tx-state']").TextContent.Trim());
        Assert.Equal("Alice", cut.Find("[data-testid='tx-actor']").TextContent.Trim());
        Assert.Equal("Web UI", cut.Find("[data-testid='tx-client']").TextContent.Trim());
        Assert.Equal("1", cut.Find("[data-testid='tx-base-snapshot']").TextContent.Trim());
        Assert.Equal("10", cut.Find("[data-testid='tx-working-snapshot']").TextContent.Trim());
        Assert.Equal("3", cut.Find("[data-testid='tx-change-version']").TextContent.Trim());

        // Arbeitskontext in PageRegions und WorkspaceState gesetzt
        Assert.Equal(KnowledgeReadContextKind.Transaction, _pageRegionState.KnowledgeContext?.ReadContext);
        Assert.Equal(txId.Value.ToString("D"), _pageRegionState.KnowledgeContext?.ContextId);
        Assert.Equal("Glossar Überarbeitung", _pageRegionState.KnowledgeContext?.DisplayName);

        Assert.Equal(KnowledgeReadContextKind.Transaction, _workspaceState.CurrentContext.ReadContext);
        Assert.Equal(txId, _workspaceState.CurrentReadContext.TransactionId);

        // Link in den Wissensbaum mit Rolle
        var openKnowledgeLink = cut.Find("[data-testid='tx-open-knowledge-link']");
        Assert.Equal($"/knowledge?transactionId={txId.Value}&roleId=Architekt", openKnowledgeLink.GetAttribute("href"));
    }

    [Fact]
    public void TransactionPage_MissingTransaction_RendersErrorMessage()
    {
        var nonExistentId = Guid.NewGuid();

        var cut = Render<TransactionPage>(parameters => parameters
            .Add(p => p.TransactionId, nonExistentId));

        var error = cut.Find("[data-testid='transaction-error']");
        Assert.NotNull(error);
        Assert.Contains("existiert nicht", error.TextContent);

        Assert.Equal("Fehlerhafter Kontext", _pageRegionState.KnowledgeContext?.DisplayName);
        Assert.Equal(KnowledgeReadContextKind.Current, _pageRegionState.KnowledgeContext?.ReadContext);

        var backLink = cut.Find("[data-testid='tx-back-link']");
        Assert.Equal("/transactions", backLink.GetAttribute("href"));
    }

    [Fact]
    public void TransactionPage_AgeWarningBadge_ShownWhenOlderThan7Days()
    {
        var oldTxId = new TransactionId(Guid.NewGuid());
        var oldTx = new KnowledgeTransaction(
            oldTxId,
            new SnapshotId(1),
            new SnapshotId(11),
            TransactionState.Open,
            ChangeVersion: 5,
            CreatedAtUtc: Now.AddDays(-8),
            CommittedAtUtc: null,
            Purpose: "Alte Transaction",
            Actor: "Bob",
            Client: "Desktop",
            CommitMessage: null);

        _harness.AddTransaction(oldTx);

        var cut = Render<TransactionPage>(parameters => parameters
            .Add(p => p.TransactionId, oldTxId.Value));

        Assert.Single(cut.FindAll("[data-testid='tx-age-warning']"));
    }
}
