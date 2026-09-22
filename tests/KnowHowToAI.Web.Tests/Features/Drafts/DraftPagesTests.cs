using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Drafts;
using KnowHowToAI.Server.Web.Features.Transactions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Drafts;

[Trait("Category", "Unit")]
public sealed class DraftPagesTests : BunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly TransactionTestHarness _harness = new(now: Now);
    private readonly TrackingTransactionRepository _transactionRepository;
    private readonly InMemoryWorkingSnapshotValidationDataRepository _validationData = new();
    private readonly PageRegionState _pageRegions = new();
    private readonly WorkspaceState _workspace = new();

    public DraftPagesTests()
    {
        _transactionRepository = new TrackingTransactionRepository(_harness.Store);
        Services.AddWebPageStates(_pageRegions, _workspace);
        Services.AddSingleton<IClock>(new FixedClock(Now));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService());
        Services.AddSingleton(new TransactionService(
            _transactionRepository,
            _validationData,
            new GuidIdentifierGenerator(),
            TestPolicies.DefaultValidation));
        Services.AddSingleton(_harness.CreateHistoryService());
        JSInterop.SetupAppDialog();
    }

    [Fact]
    public void DraftsPage_EmptyListExplainsThereAreNoOpenDrafts()
    {
        var cut = Render<DraftsPage>();

        Assert.Equal("Entwürfe", cut.Find("h1").TextContent.Trim());
        Assert.Single(cut.FindAll("h1"));
        Assert.Contains("keine offenen Entwürfe", cut.Find("[data-testid='drafts-empty']").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("meine Entwürfe", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DraftsPage_ShowsAllOpenDraftsAndExplicitResumeAndReviewLinks()
    {
        var selected = AddOpenDraft("Glossar", "Alice");
        var other = AddOpenDraft("Handbuch", "Bob");

        var cut = Render<DraftsPage>();

        Assert.Equal(2, cut.FindAll("[data-testid^='draft-item-']").Count);
        Assert.Contains("Glossar", cut.Find($"[data-testid='draft-item-{selected.TransactionId.Value}']").TextContent);
        Assert.Equal($"/knowledge?transactionId={selected.TransactionId.Value}",
            cut.Find($"[data-testid='draft-resume-{selected.TransactionId.Value}']").GetAttribute("href"));
        Assert.Equal($"/drafts/{selected.TransactionId.Value}",
            cut.Find($"[data-testid='draft-open-{selected.TransactionId.Value}']").GetAttribute("href"));
        Assert.Contains("Handbuch", cut.Find($"[data-testid='draft-item-{other.TransactionId.Value}']").TextContent);
        Assert.DoesNotContain("meine Entwürfe", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DraftPage_DirectLoadUsesRequestedIdAndOffersOnlyItsCompletionActions()
    {
        var selected = AddOpenDraft("Glossar", "Alice");
        var other = AddOpenDraft("Handbuch", "Bob");

        var cut = Render<DraftPage>(parameters => parameters.Add(page => page.TransactionId, selected.TransactionId.Value));

        Assert.Contains(selected.TransactionId.Value.ToString("D"), cut.Find("[data-testid='draft-summary']").TextContent);
        Assert.Equal("Entwurf: Glossar", cut.Find("h1").TextContent.Trim());
        Assert.Equal(selected.TransactionId, _workspace.CurrentReadContext.TransactionId);
        Assert.Single(cut.FindAll("[data-testid='draft-review']"));
        Assert.Single(cut.FindAll("[data-testid='transaction-diff']"));
        Assert.Single(cut.FindAll("[data-testid='transaction-validation']"));
        Assert.NotEqual(other.TransactionId, _workspace.CurrentReadContext.TransactionId);
    }

    [Fact]
    public void DraftPage_MissingIdShowsNotFoundWithoutSelectingAnotherDraft()
    {
        AddOpenDraft("Vorhanden", "Alice");
        var missingId = Guid.NewGuid();

        var cut = Render<DraftPage>(parameters => parameters.Add(page => page.TransactionId, missingId));

        Assert.Single(cut.FindAll("[data-testid='draft-error']"));
        Assert.Contains("existiert nicht", cut.Find("[data-testid='draft-error']").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Null(_workspace.CurrentReadContext.TransactionId);
        Assert.Equal("/drafts", cut.Find("[data-testid='drafts-back']").GetAttribute("href"));
    }

    [Fact]
    public void DraftPage_DiscardAffectsOnlyTheVisibleDraft()
    {
        var selected = AddOpenDraft("Ausgewählt", "Alice");
        var other = AddOpenDraft("Unberührt", "Bob");
        var cut = Render<DraftPage>(parameters => parameters.Add(page => page.TransactionId, selected.TransactionId.Value));

        cut.Find("[data-testid='discard-transaction-button']").Click();
        cut.FindComponents<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>()[1]
            .Find(".confirmation-dialog__button--destructive").Click();

        Assert.Equal(selected.TransactionId, _transactionRepository.LastDiscardedId);
        Assert.Equal(TransactionState.Discarded, _harness.Store.Transactions[selected.TransactionId].State);
        Assert.Equal(TransactionState.Open, _harness.Store.Transactions[other.TransactionId].State);
    }

    [Fact]
    public void DraftPage_ValidationErrorsShowFindingAndKeepDraftOpen()
    {
        var selected = AddOpenDraft("Ausgewählt", "Alice");
        _validationData.SetValidationData(selected.TransactionId, InvalidContentValidationData());
        var cut = Render<DraftPage>(parameters => parameters.Add(page => page.TransactionId, selected.TransactionId.Value));

        cut.Find("[data-testid='validate-transaction-button']").Click();

        Assert.Contains("HeadingNotAllowed", cut.Find("[data-testid='validation-errors']").TextContent);
        Assert.Equal(TransactionState.Open, _harness.Store.Transactions[selected.TransactionId].State);
        Assert.Single(cut.FindAll("[data-testid='draft-review']"));
    }

    [Fact]
    public void DraftPage_ConflictKeepsSelectedDraftAndShowsSafeNextStep()
    {
        var selected = AddOpenDraft("Ausgewählt", "Alice");
        _transactionRepository.NextCommitResult = new CommitTransactionResult(
            null,
            null,
            new DomainError(
                "SnapshotConflict",
                "Der aktuelle Snapshot wurde zwischenzeitlich geändert.",
                new Dictionary<string, string> { ["baseSnapshotId"] = "1", ["currentSnapshotId"] = "3" }));
        var cut = Render<DraftPage>(parameters => parameters.Add(page => page.TransactionId, selected.TransactionId.Value));

        cut.Find("[data-testid='commit-transaction-button']").Click();
        cut.FindComponents<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>()[0]
            .Find(".confirmation-dialog__button--primary").Click();

        Assert.Single(cut.FindAll("[data-testid='snapshot-conflict']"));
        Assert.Single(cut.FindAll("[data-testid='transaction-diff']"));
        Assert.Equal(TransactionState.Open, _harness.Store.Transactions[selected.TransactionId].State);
        Assert.Contains("Neuen Entwurf für manuelles Reapply", cut.Find("[data-testid='snapshot-conflict']").TextContent);
    }

    [Fact]
    public void DraftPage_CommitAffectsOnlyTheVisibleDraft()
    {
        var selected = AddOpenDraft("Ausgewählt", "Alice");
        var other = AddOpenDraft("Unberührt", "Bob");
        _validationData.SetValidationData(selected.TransactionId, new WorkingSnapshotValidationData([], [], [], [], []));
        var cut = Render<DraftPage>(parameters => parameters.Add(page => page.TransactionId, selected.TransactionId.Value));

        cut.Find("[data-testid='commit-transaction-button']").Click();
        cut.FindComponents<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>()[0]
            .Find(".confirmation-dialog__button--primary").Click();

        Assert.Equal(selected.TransactionId, _transactionRepository.LastCommittedId);
        Assert.Equal(TransactionState.Committed, _harness.Store.Transactions[selected.TransactionId].State);
        Assert.Equal(TransactionState.Open, _harness.Store.Transactions[other.TransactionId].State);
        Assert.Null(_workspace.CurrentReadContext.TransactionId);
    }

    private KnowledgeTransaction AddOpenDraft(string purpose, string actor)
    {
        var transaction = new KnowledgeTransaction(
            new TransactionId(Guid.NewGuid()),
            new SnapshotId(1),
            new SnapshotId(10 + _harness.Store.Transactions.Count),
            TransactionState.Open,
            ChangeVersion: 1,
            CreatedAtUtc: Now,
            CommittedAtUtc: null,
            Purpose: purpose,
            Actor: actor,
            Client: "Web UI",
            CommitMessage: null);
        _harness.AddTransaction(transaction);
        return transaction;
    }

    private static WorkingSnapshotValidationData InvalidContentValidationData()
    {
        var snapshotId = new SnapshotId(2);
        var nodeId = new NodeId(Guid.NewGuid());
        var audienceId = new AudienceId("Default");
        return new WorkingSnapshotValidationData(
            [new Node(snapshotId, nodeId, null, "Root", null, 0, false)],
            [new Audience(snapshotId, audienceId, "Default", null, false)],
            [new AudienceResolution(snapshotId, audienceId, audienceId, 1)],
            [new NodeContent(snapshotId, nodeId, audienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "# Nicht erlaubt", false)],
            [],
            ChangeVersion: 1);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public CurrentUser GetCurrentUser() => new("test-user", "Test User");
    }

    private sealed class TrackingTransactionRepository(InMemoryKnowledgeStore store) : ITransactionRepository
    {
        private readonly InMemoryTransactionRepository _reads = new(store);

        public TransactionId? LastCommittedId { get; private set; }

        public TransactionId? LastDiscardedId { get; private set; }

        public CommitTransactionResult? NextCommitResult { get; set; }

        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            _reads.BeginAsync(request, cancellationToken);

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            _reads.FindAsync(transactionId, cancellationToken);

        public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(CancellationToken cancellationToken = default) =>
            _reads.ListOpenAsync(cancellationToken);

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default)
        {
            LastCommittedId = request.TransactionId;
            if (NextCommitResult is { } response)
                return Task.FromResult(response);

            var committed = store.Transactions[request.TransactionId] with
            {
                State = TransactionState.Committed,
                CommittedAtUtc = Now,
                CommitMessage = request.CommitMessage
            };
            store.Transactions[request.TransactionId] = committed;
            return Task.FromResult(new CommitTransactionResult(committed, null, null));
        }

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
        {
            LastDiscardedId = transactionId;
            var discarded = store.Transactions[transactionId] with { State = TransactionState.Discarded };
            store.Transactions[transactionId] = discarded;
            return Task.FromResult(Result<KnowledgeTransaction>.Success(discarded));
        }
    }
}
