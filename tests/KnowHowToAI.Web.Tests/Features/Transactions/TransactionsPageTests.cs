using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Transactions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Transactions;

[Trait("Category", "Unit")]
public sealed class TransactionsPageTests : BunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly TransactionTestHarness _harness;
    private readonly PageRegionState _pageRegionState = new();
    private readonly WorkspaceState _workspaceState = new();

    public TransactionsPageTests()
    {
        _harness = new TransactionTestHarness(now: Now);
        Services.AddWebPageStates(_pageRegionState, _workspaceState);
        Services.AddSingleton<IClock>(new FixedClock(Now));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService("TestUser"));
        Services.AddSingleton(_harness.CreateService());
    }

    [Fact]
    public void TransactionsPage_EmptyList_DisplaysEmptyStateMessage()
    {
        var cut = Render<TransactionsPage>();

        cut.Find("[data-testid='empty-transactions-message']");
        Assert.Equal("Transactions", _pageRegionState.KnowledgeContext?.DisplayName);
        Assert.Equal(KnowledgeReadContextKind.Current, _pageRegionState.KnowledgeContext?.ReadContext);
    }

    [Fact]
    public void TransactionsPage_RendersOpenTransactionsWithAllMetadata()
    {
        var txId = new TransactionId(Guid.NewGuid());
        var tx = new KnowledgeTransaction(
            txId,
            new SnapshotId(1),
            new SnapshotId(10),
            TransactionState.Open,
            ChangeVersion: 3,
            CreatedAtUtc: Now.AddHours(-2),
            CommittedAtUtc: null,
            Purpose: "Glossar Überarbeitung",
            Actor: "Alice",
            Client: "Web UI",
            CommitMessage: null);

        _harness.AddTransaction(tx);

        var cut = Render<TransactionsPage>();

        var item = cut.Find($"[data-testid='transaction-item-{txId.Value}']");
        Assert.NotNull(item);
        Assert.Contains("Glossar Überarbeitung", item.TextContent);
        Assert.Contains("Alice", item.TextContent);
        Assert.Contains("Web UI", item.TextContent);
        Assert.Contains("1", item.TextContent);
        Assert.Contains("10", item.TextContent);

        var detailsLink = cut.Find($"[data-testid='tx-details-link-{txId.Value}']");
        Assert.Equal($"/transactions/{txId.Value}", detailsLink.GetAttribute("href"));

        var resumeLink = cut.Find($"[data-testid='tx-resume-link-{txId.Value}']");
        Assert.Equal($"/knowledge?transactionId={txId.Value}", resumeLink.GetAttribute("href"));
    }

    [Fact]
    public void TransactionsPage_PrimaryResumeActionPrecedesDetailsAndTechnicalMetadataIsProgressive()
    {
        var txId = new TransactionId(Guid.NewGuid());
        _harness.AddTransaction(new KnowledgeTransaction(
            txId,
            new SnapshotId(1),
            new SnapshotId(10),
            TransactionState.Open,
            ChangeVersion: 3,
            CreatedAtUtc: Now,
            CommittedAtUtc: null,
            Purpose: null,
            Actor: null,
            Client: null,
            CommitMessage: null));

        var cut = Render<TransactionsPage>();

        var item = cut.Find($"[data-testid='transaction-item-{txId.Value}']");
        var technicalDetails = item.QuerySelector($"[data-testid='tx-technical-details-{txId.Value}']");
        Assert.NotNull(technicalDetails);
        Assert.False(technicalDetails!.HasAttribute("open"));
        Assert.Equal("Technische Details", technicalDetails.QuerySelector("summary")?.TextContent.Trim());
        Assert.Contains("Nicht angegeben", item.TextContent);

        var actions = item.QuerySelectorAll(".transaction-card__actions a");
        Assert.Equal(2, actions.Length);
        Assert.Equal($"tx-resume-link-{txId.Value}", actions[0].GetAttribute("data-testid"));
        Assert.Contains("btn-primary", actions[0].GetAttribute("class"));
        Assert.Equal($"tx-details-link-{txId.Value}", actions[1].GetAttribute("data-testid"));
        Assert.Contains("btn-secondary", actions[1].GetAttribute("class"));
    }

    [Fact]
    public void TransactionsPage_AgeWarningBadge_ShownWhenOlderThan7Days()
    {
        var recentTxId = new TransactionId(Guid.NewGuid());
        var oldTxId = new TransactionId(Guid.NewGuid());

        var recentTx = new KnowledgeTransaction(
            recentTxId,
            new SnapshotId(1),
            new SnapshotId(10),
            TransactionState.Open,
            ChangeVersion: 1,
            CreatedAtUtc: Now.AddDays(-2),
            CommittedAtUtc: null,
            Purpose: "Aktuelle Transaction",
            Actor: "Alice",
            Client: "Web UI",
            CommitMessage: null);

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

        _harness.AddTransaction(recentTx);
        _harness.AddTransaction(oldTx);

        var cut = Render<TransactionsPage>();

        var recentItem = cut.Find($"[data-testid='transaction-item-{recentTxId.Value}']");
        Assert.Empty(recentItem.QuerySelectorAll("[data-testid='tx-age-warning']"));

        var oldItem = cut.Find($"[data-testid='transaction-item-{oldTxId.Value}']");
        Assert.Single(oldItem.QuerySelectorAll("[data-testid='tx-age-warning']"));
    }

    [Fact]
    public void TransactionsPage_ActorIsReadOnly_FromCurrentUserService()
    {
        var cut = Render<TransactionsPage>();

        var actorDisplay = cut.Find("[data-testid='tx-actor-display']");
        Assert.Equal("TestUser", actorDisplay.GetAttribute("value"));
        Assert.True(actorDisplay.HasAttribute("readonly"));
    }

    [Fact]
    public void TransactionsPage_BeginTransaction_UsesCurrentUserServiceAndNavigates()
    {
        var cut = Render<TransactionsPage>();

        var purposeInput = cut.Find("[data-testid='tx-purpose-input']");
        purposeInput.Change("Neues Feature");

        var clientInput = cut.Find("[data-testid='tx-client-input']");
        clientInput.Change("CustomClient");

        var submitButton = cut.Find("[data-testid='begin-transaction-button']");
        submitButton.Click();

        var navManager = Services.GetRequiredService<NavigationManager>();
        Assert.StartsWith("http://localhost/transactions/", navManager.Uri);

        // Die erstellte Transaction existiert im Store
        var createdTx = _harness.Store.Transactions.Values.FirstOrDefault(t => t.Purpose == "Neues Feature");
        Assert.NotNull(createdTx);
        Assert.Equal("TestUser", createdTx.Actor);
        Assert.Equal("CustomClient", createdTx.Client);
        Assert.Equal(TransactionState.Open, createdTx.State);
    }

    private sealed class TestCurrentUserService(string userName) : ICurrentUserService
    {
        public CurrentUser GetCurrentUser() => new("test-id", userName);
    }
}
