using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Transactions;

public sealed partial class TransactionCompletionPanel : ComponentBase
{
    [Inject]
    private TransactionService TransactionService { get; set; } = default!;

    [Inject]
    private ICurrentUserService CurrentUserService { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private ToastState ToastState { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter, EditorRequired]
    public KnowledgeTransaction Transaction { get; set; } = default!;

    private string? _completionError;
    private bool _isSubmitting;
    private bool _isStartingManualReapply;
    private SnapshotConflict? _snapshotConflict;
    private ConfirmationDialog? _commitDialog;
    private ConfirmationDialog? _discardDialog;

    private bool CanCommit => CanDiscard && _snapshotConflict is null;

    private bool CanDiscard => Transaction.State == TransactionState.Open
        && !_isSubmitting
        && !_isStartingManualReapply;

    private bool CanStartManualReapply => _snapshotConflict is not null
        && !_isStartingManualReapply
        && !_isSubmitting;

    private async Task OpenCommitDialogAsync()
    {
        if (!EnsureCompletionCanStart(isCommit: true))
            return;

        if (_commitDialog is not null)
            await _commitDialog.OpenAsync();
    }

    private async Task OpenDiscardDialogAsync()
    {
        if (!EnsureCompletionCanStart(isCommit: false))
            return;

        if (_discardDialog is not null)
            await _discardDialog.OpenAsync();
    }

    private async Task CommitAsync(string? commitMessage)
    {
        if (!EnsureCompletionCanStart(isCommit: true))
            return;

        _isSubmitting = true;
        _completionError = null;
        var result = await TransactionService.CommitAsync(
            Transaction.TransactionId,
            string.IsNullOrWhiteSpace(commitMessage) ? null : commitMessage,
            CancellationToken.None);
        _isSubmitting = false;

        if (result.IsCommitted)
        {
            await SwitchToCurrentSnapshotAsync(_commitDialog, "Transaction wurde committed.");
            return;
        }

        _snapshotConflict = CreateSnapshotConflict(result);
        if (_snapshotConflict is not null)
        {
            await _commitDialog!.CloseAsync();
            return;
        }

        _completionError = CreateCommitErrorMessage(result);
    }

    private async Task DiscardAsync()
    {
        if (!EnsureCompletionCanStart(isCommit: false))
            return;

        _isSubmitting = true;
        _completionError = null;
        var result = await TransactionService.DiscardAsync(Transaction.TransactionId, CancellationToken.None);
        _isSubmitting = false;

        if (result.IsSuccess)
        {
            await SwitchToCurrentSnapshotAsync(_discardDialog, "Transaction wurde verworfen.");
            return;
        }

        _completionError = result.Error!.Message;
    }

    private async Task StartManualReapplyAsync()
    {
        if (!CanStartManualReapply)
            return;

        _isStartingManualReapply = true;
        _completionError = null;
        var result = await TransactionService.BeginAsync(
            new BeginTransactionOptions(
                Transaction.Purpose,
                CurrentUserService.GetCurrentUserName(),
                Transaction.Client),
            CancellationToken.None);
        _isStartingManualReapply = false;

        NavigationManager.NavigateTo($"/transactions/{result.Value!.TransactionId.Value}");
    }

    private bool EnsureCompletionCanStart(bool isCommit)
    {
        if (isCommit && _snapshotConflict is not null)
        {
            _completionError = "Dieser Commit kann wegen des Snapshot-Konflikts nicht wiederholt werden. Starten Sie eine neue Transaction für das manuelle Reapply.";
            return false;
        }

        if (!CanDiscard)
        {
            _completionError = "Diese Transaction ist nicht mehr offen. Laden Sie die Seite neu.";
            return false;
        }

        if (WorkspaceState.CurrentChangeVersion is { } changeVersion
            && changeVersion != Transaction.ChangeVersion)
        {
            _completionError = "Die Transaction wurde zwischenzeitlich geändert. Laden Sie Validierung und Diff neu, bevor Sie sie abschließen.";
            return false;
        }

        return true;
    }

    private async Task SwitchToCurrentSnapshotAsync(ConfirmationDialog? dialog, string successMessage)
    {
        if (dialog is not null)
            await dialog.CloseAsync();

        var currentContext = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Current,
            AudienceName: WorkspaceState.CurrentAudienceId);
        PageRegions.SetKnowledgeContext(currentContext);
        WorkspaceState.SetContext(currentContext, new ReadContext());
        WorkspaceState.SetChangeVersion(null);
        ToastState.Show(successMessage);
        NavigationManager.NavigateTo(CurrentKnowledgeUrl);
    }

    private string CurrentKnowledgeUrl => string.IsNullOrWhiteSpace(WorkspaceState.CurrentAudienceId)
        ? "/knowledge"
        : $"/knowledge?audienceId={Uri.EscapeDataString(WorkspaceState.CurrentAudienceId)}";

    private static string CreateCommitErrorMessage(CommitTransactionResult result)
    {
        if (result.ValidationReport is { IsValid: false } report && report.Errors.Count > 0)
        {
            var error = report.Errors[0];
            return $"Commit wurde nicht ausgeführt: {error.Code}: {error.Message}";
        }

        return result.Error?.Message ?? "Commit konnte nicht ausgeführt werden.";
    }

    private static SnapshotConflict? CreateSnapshotConflict(CommitTransactionResult result)
    {
        var error = result.Error;
        if (error is null
            || !string.Equals(error.Code, TransactionValidationErrorCodes.SnapshotConflict, StringComparison.Ordinal)
            || error.Details is null
            || !error.Details.TryGetValue("baseSnapshotId", out var baseSnapshot)
            || !error.Details.TryGetValue("currentSnapshotId", out var currentSnapshot)
            || !long.TryParse(baseSnapshot, out var baseSnapshotId)
            || !long.TryParse(currentSnapshot, out var currentSnapshotId))
        {
            return null;
        }

        return new SnapshotConflict(baseSnapshotId, currentSnapshotId);
    }

    private static Task DismissDialogAsync() => Task.CompletedTask;

    private sealed record SnapshotConflict(long BaseSnapshotId, long CurrentSnapshotId);
}
