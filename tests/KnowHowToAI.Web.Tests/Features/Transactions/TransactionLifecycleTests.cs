using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.Features.Transactions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Transactions;

[Trait("Category", "Unit")]
public sealed class TransactionLifecycleTests : BunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 9, 0, 0, TimeSpan.Zero);
    private readonly TransactionTestHarness _historyHarness = new(now: Now);
    private readonly LifecycleTransactionRepository _repository;
    private readonly WorkspaceState _workspaceState = new();
    private readonly ToastState _toastState = new();

    public TransactionLifecycleTests()
    {
        _repository = new LifecycleTransactionRepository(OpenTransaction());
        _historyHarness.AddTransaction(_repository.Transaction);
        _historyHarness.Store.Snapshots.Add(new Snapshot(
            _repository.Transaction.WorkingSnapshotId,
            _repository.Transaction.BaseSnapshotId,
            SnapshotState.Working,
            Now,
            null));
        _historyHarness.Store.Snapshots.Add(new Snapshot(
            new SnapshotId(3),
            _repository.Transaction.BaseSnapshotId,
            SnapshotState.Committed,
            Now,
            Now));
        Services.AddWebPageStates(workspaceState: _workspaceState, toastState: _toastState);
        Services.AddSingleton<IClock>(new FixedClock(Now));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService("ReapplyUser"));
        Services.AddSingleton(new TransactionService(
            _repository,
            new InMemoryWorkingSnapshotValidationDataRepository(),
            new FixedIdentifierGenerator
            {
                FixedTransactionId = new TransactionId(Guid.Parse("00000000-0000-0000-0000-000000000402"))
            },
            TestPolicies.DefaultValidation));
        Services.AddSingleton(_historyHarness.CreateHistoryService());
        JSInterop.SetupAppDialog();
    }

    [Fact]
    public void Commit_AfterConfirmation_CommitsWithMessageAndSwitchesToCurrentSnapshot()
    {
        _workspaceState.SetRole("Architekt");
        _repository.CommitResult = new CommitTransactionResult(CommittedTransaction(), null, null);

        var cut = RenderPage();

        cut.Find("[data-testid='commit-transaction-button']").Click();
        var dialog = CommitDialog(cut);
        dialog.Find("textarea").Change("Bereinigt");
        dialog.Find(".confirmation-dialog__button--primary").Click();

        Assert.Equal(1, _repository.CommitCount);
        Assert.Equal("Bereinigt", _repository.CommitRequest?.CommitMessage);
        Assert.False(_workspaceState.HasActiveTransaction);
        Assert.Equal(KnowHowToAI.Server.Web.Components.Layout.Context.KnowledgeReadContextKind.Current, _workspaceState.CurrentContext.ReadContext);
        Assert.Single(_toastState.Entries);
        Assert.Contains("committed", _toastState.Entries[0].Message, StringComparison.Ordinal);
        Assert.EndsWith("/knowledge?roleId=Architekt", BrowserNavigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Commit_WithValidationError_StaysInWorkingContextAndShowsFinding()
    {
        var validationError = new DomainError("HeadingNotAllowed", "Überschriften sind im Inhalt nicht erlaubt.");
        _repository.CommitResult = new CommitTransactionResult(
            null,
            new TransactionValidationReport([validationError], [], [], []),
            validationError);

        var cut = RenderPage();

        ConfirmCommit(cut);

        Assert.Equal(1, _repository.CommitCount);
        Assert.True(_workspaceState.HasActiveTransaction);
        Assert.Contains("HeadingNotAllowed", cut.Find("[data-testid='transaction-completion-error']").TextContent);
        Assert.Empty(_toastState.Entries);
    }

    [Fact]
    public void Commit_WithSnapshotConflict_ShowsBaseCurrentComparisonAndStartsManualReapply()
    {
        var conflict = new DomainError(
            "SnapshotConflict",
            "Der aktuelle Snapshot wurde zwischenzeitlich geändert.",
            new Dictionary<string, string>
            {
                ["baseSnapshotId"] = "1",
                ["currentSnapshotId"] = "3"
            });
        _repository.CommitResult = new CommitTransactionResult(null, null, conflict);
        _repository.BeginResult = OpenTransaction() with
        {
            TransactionId = new TransactionId(Guid.Parse("00000000-0000-0000-0000-000000000402")),
            BaseSnapshotId = new SnapshotId(3),
            WorkingSnapshotId = new SnapshotId(4)
        };

        var cut = RenderPage();

        ConfirmCommit(cut);

        Assert.Equal(1, _repository.CommitCount);
        Assert.True(_workspaceState.HasActiveTransaction);
        Assert.Contains("Base-Snapshot 1", cut.Find("[data-testid='snapshot-conflict-explanation']").TextContent);
        Assert.Contains("Current Snapshot 3", cut.Find("[data-testid='snapshot-conflict-explanation']").TextContent);
        Assert.Single(cut.FindAll("[data-testid='snapshot-diff']"));

        cut.Find("[data-testid='snapshot-conflict-start-reapply']").Click();

        Assert.Equal(1, _repository.BeginCount);
        Assert.Equal("Lebenszyklus", _repository.BeginRequest?.Purpose);
        Assert.Equal("ReapplyUser", _repository.BeginRequest?.Actor);
        Assert.Equal("Web UI", _repository.BeginRequest?.Client);
        Assert.EndsWith("/transactions/00000000-0000-0000-0000-000000000402", BrowserNavigation.Uri, StringComparison.Ordinal);
        Assert.Empty(_toastState.Entries);
    }

    [Fact]
    public void Commit_WhileRequestIsRunning_IsSubmittedOnlyOnce()
    {
        _repository.CommitResult = new CommitTransactionResult(CommittedTransaction(), null, null);
        var completion = new TaskCompletionSource<CommitTransactionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _repository.CommitTask = completion.Task;
        var cut = RenderPage();

        cut.Find("[data-testid='commit-transaction-button']").Click();
        var confirm = CommitDialog(cut).Find(".confirmation-dialog__button--primary");
        confirm.Click();
        confirm.Click();

        Assert.Equal(1, _repository.CommitCount);
        Assert.True(cut.Find("[data-testid='discard-transaction-button']").HasAttribute("disabled"));

        completion.SetResult(_repository.CommitResult);
        cut.WaitForAssertion(() => Assert.False(_workspaceState.HasActiveTransaction));
    }

    [Fact]
    public void Discard_AfterConfirmation_SwitchesToCurrentSnapshot()
    {
        _repository.DiscardResult = Result<KnowledgeTransaction>.Success(DiscardedTransaction());
        var cut = RenderPage();

        cut.Find("[data-testid='discard-transaction-button']").Click();
        DiscardDialog(cut).Find(".confirmation-dialog__button--destructive").Click();

        Assert.Equal(1, _repository.DiscardCount);
        Assert.False(_workspaceState.HasActiveTransaction);
        Assert.Single(_toastState.Entries);
        Assert.Contains("verworfen", _toastState.Entries[0].Message, StringComparison.Ordinal);
        Assert.EndsWith("/knowledge", BrowserNavigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Commit_WithStaleChangeVersion_IsRejectedBeforeTheServiceCall()
    {
        var cut = RenderPage();
        _workspaceState.SetChangeVersion(OpenTransaction().ChangeVersion + 1);

        cut.Find("[data-testid='commit-transaction-button']").Click();

        Assert.Equal(0, _repository.CommitCount);
        Assert.Contains("zwischenzeitlich geändert", cut.Find("[data-testid='transaction-completion-error']").TextContent);
    }

    [Fact]
    public void CompletedTransaction_DisablesCommitAndDiscard()
    {
        _repository.Transaction = CommittedTransaction();

        var cut = RenderPage();

        Assert.True(cut.Find("[data-testid='commit-transaction-button']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='discard-transaction-button']").HasAttribute("disabled"));
        Assert.Single(cut.FindAll("[data-testid='transaction-completion-unavailable']"));
    }

    private IRenderedComponent<TransactionPage> RenderPage() =>
        Render<TransactionPage>(parameters => parameters.Add(page => page.TransactionId, _repository.Transaction.TransactionId.Value));

    private NavigationManager BrowserNavigation => Services.GetRequiredService<NavigationManager>();

    private static void ConfirmCommit(IRenderedComponent<TransactionPage> cut) =>
        CommitDialog(cut).Find(".confirmation-dialog__button--primary").Click();

    private static IRenderedComponent<ConfirmationDialog> CommitDialog(IRenderedComponent<TransactionPage> cut) =>
        cut.FindComponents<ConfirmationDialog>()[0];

    private static IRenderedComponent<ConfirmationDialog> DiscardDialog(IRenderedComponent<TransactionPage> cut) =>
        cut.FindComponents<ConfirmationDialog>()[1];

    private static KnowledgeTransaction OpenTransaction() =>
        new(
            new TransactionId(Guid.Parse("00000000-0000-0000-0000-000000000401")),
            new SnapshotId(1),
            new SnapshotId(2),
            TransactionState.Open,
            ChangeVersion: 7,
            CreatedAtUtc: Now,
            CommittedAtUtc: null,
            Purpose: "Lebenszyklus",
            Actor: "Alice",
            Client: "Web UI",
            CommitMessage: null);

    private static KnowledgeTransaction CommittedTransaction() => OpenTransaction() with
    {
        State = TransactionState.Committed,
        CommittedAtUtc = Now,
        CommitMessage = "Bereinigt"
    };

    private static KnowledgeTransaction DiscardedTransaction() => OpenTransaction() with
    {
        State = TransactionState.Discarded
    };

    private sealed class LifecycleTransactionRepository(KnowledgeTransaction transaction) : ITransactionRepository
    {
        public KnowledgeTransaction Transaction { get; set; } = transaction;

        public CommitTransactionResult CommitResult { get; set; } = new(null, null, new DomainError("SnapshotConflict", "Nicht konfiguriert."));

        public Result<KnowledgeTransaction> DiscardResult { get; set; } = Result<KnowledgeTransaction>.Failure(
            new DomainError("TransactionClosed", "Nicht konfiguriert."));

        public Task<CommitTransactionResult>? CommitTask { get; set; }

        public CommitTransactionRequest? CommitRequest { get; private set; }

        public int CommitCount { get; private set; }

        public int DiscardCount { get; private set; }

        public KnowledgeTransaction? BeginResult { get; set; }

        public BeginTransactionRequest? BeginRequest { get; private set; }

        public int BeginCount { get; private set; }

        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default)
        {
            BeginCount++;
            BeginRequest = request;
            return Task.FromResult(BeginResult ?? throw new InvalidOperationException("Keine Reapply-Transaction konfiguriert."));
        }

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeTransaction?>(transactionId == Transaction.TransactionId ? Transaction : null);

        public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeTransaction>>([]);

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default)
        {
            CommitCount++;
            CommitRequest = request;
            return CommitTask ?? Task.FromResult(CommitResult);
        }

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
        {
            DiscardCount++;
            return Task.FromResult(DiscardResult);
        }
    }

    private sealed class TestCurrentUserService(string userName) : ICurrentUserService
    {
        public CurrentUser GetCurrentUser() => new("reapply-user", userName);
    }
}
