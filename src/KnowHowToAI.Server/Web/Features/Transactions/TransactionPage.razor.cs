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

public sealed partial class TransactionPage : ComponentBase
{
    [Inject]
    private TransactionService TransactionService { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    [Inject]
    private ToastState ToastState { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public Guid TransactionId { get; set; }

    private KnowledgeTransaction? _transaction;
    private string? _errorMessage;
    private string? _completionError;
    private bool _isLoading = true;
    private bool _isSubmitting;
    private ConfirmationDialog? _commitDialog;
    private ConfirmationDialog? _discardDialog;

    private bool CanComplete => _transaction?.State == TransactionState.Open && !_isSubmitting;

    private string KnowledgeUrl => string.IsNullOrWhiteSpace(WorkspaceState.CurrentRoleId)
        ? $"/knowledge?transactionId={TransactionId}"
        : $"/knowledge?transactionId={TransactionId}&roleId={WorkspaceState.CurrentRoleId}";

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        _completionError = null;

        var result = await TransactionService.GetAsync(
            new TransactionId(TransactionId),
            CancellationToken.None);

        if (!result.IsSuccess)
        {
            _errorMessage = result.Error!.Message;
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Fehlerhafter Kontext"));
        }
        else
        {
            _transaction = result.Value!;

            var contextVm = new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Transaction,
                ContextId: _transaction.TransactionId.Value.ToString("D"),
                DisplayName: string.IsNullOrWhiteSpace(_transaction.Purpose)
                    ? $"Transaktion {_transaction.TransactionId.Value:D}"
                    : _transaction.Purpose,
                RoleName: WorkspaceState.CurrentRoleId,
                BaseSnapshotId: _transaction.BaseSnapshotId.Value);

            PageRegions.SetKnowledgeContext(contextVm);
            WorkspaceState.SetContext(
                contextVm,
                new ReadContext(TransactionId: _transaction.TransactionId));
            WorkspaceState.SetChangeVersion(_transaction.ChangeVersion);
        }

        _isLoading = false;
    }

    private async Task OpenCommitDialogAsync()
    {
        if (!EnsureCompletionCanStart())
            return;

        if (_commitDialog is not null)
        {
            await _commitDialog.OpenAsync();
        }
    }

    private async Task OpenDiscardDialogAsync()
    {
        if (!EnsureCompletionCanStart())
            return;

        if (_discardDialog is not null)
        {
            await _discardDialog.OpenAsync();
        }
    }

    private async Task CommitAsync(string? commitMessage)
    {
        if (!EnsureCompletionCanStart())
            return;

        _isSubmitting = true;
        _completionError = null;

        var result = await TransactionService.CommitAsync(
            _transaction!.TransactionId,
            string.IsNullOrWhiteSpace(commitMessage) ? null : commitMessage,
            CancellationToken.None);

        _isSubmitting = false;
        if (result.IsCommitted)
        {
            await SwitchToCurrentSnapshotAsync(_commitDialog, "Transaction wurde committed.");
            return;
        }

        _completionError = CreateCommitErrorMessage(result);
    }

    private async Task DiscardAsync()
    {
        if (!EnsureCompletionCanStart())
            return;

        _isSubmitting = true;
        _completionError = null;

        var result = await TransactionService.DiscardAsync(_transaction!.TransactionId, CancellationToken.None);

        _isSubmitting = false;
        if (result.IsSuccess)
        {
            await SwitchToCurrentSnapshotAsync(_discardDialog, "Transaction wurde verworfen.");
            return;
        }

        _completionError = result.Error!.Message;
    }

    private bool EnsureCompletionCanStart()
    {
        if (!CanComplete)
        {
            _completionError = "Diese Transaction ist nicht mehr offen. Laden Sie die Seite neu.";
            return false;
        }

        if (WorkspaceState.CurrentChangeVersion is { } changeVersion
            && changeVersion != _transaction!.ChangeVersion)
        {
            _completionError = "Die Transaction wurde zwischenzeitlich geändert. Laden Sie Validierung und Diff neu, bevor Sie sie abschließen.";
            return false;
        }

        return true;
    }

    private async Task SwitchToCurrentSnapshotAsync(ConfirmationDialog? dialog, string successMessage)
    {
        if (dialog is not null)
        {
            await dialog.CloseAsync();
        }

        var currentContext = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Current,
            RoleName: WorkspaceState.CurrentRoleId);
        PageRegions.SetKnowledgeContext(currentContext);
        WorkspaceState.SetContext(currentContext, new ReadContext());
        WorkspaceState.SetChangeVersion(null);
        ToastState.Show(successMessage);
        NavigationManager.NavigateTo(CurrentKnowledgeUrl);
    }

    private string CurrentKnowledgeUrl => string.IsNullOrWhiteSpace(WorkspaceState.CurrentRoleId)
        ? "/knowledge"
        : $"/knowledge?roleId={Uri.EscapeDataString(WorkspaceState.CurrentRoleId)}";

    private static string CreateCommitErrorMessage(CommitTransactionResult result)
    {
        if (result.ValidationReport is { IsValid: false } report && report.Errors.Count > 0)
        {
            var error = report.Errors[0];
            return $"Commit wurde nicht ausgeführt: {error.Code}: {error.Message}";
        }

        return result.Error?.Message ?? "Commit konnte nicht ausgeführt werden.";
    }

    private static Task DismissDialogAsync() => Task.CompletedTask;
}
