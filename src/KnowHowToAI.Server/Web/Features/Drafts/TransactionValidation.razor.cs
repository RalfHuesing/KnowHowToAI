using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Drafts;

/// <summary>Fordert den vollständigen serverseitigen Validierungsbefund einer offenen Transaction an.</summary>
public sealed partial class TransactionValidation : ComponentBase, IDisposable
{
    [Inject]
    private TransactionService TransactionService { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Parameter, EditorRequired]
    public KnowledgeTransaction Transaction { get; set; } = default!;

    private TransactionValidationViewModel? _result;
    private string? _errorMessage;
    private bool _isValidating;
    private Guid _resultTransactionId;

    private long CurrentChangeVersion => WorkspaceState.CurrentChangeVersion ?? Transaction.ChangeVersion;

    private bool ResultsAreStale => _result is not null
        && _result.ChangeVersion != CurrentChangeVersion;

    protected override void OnInitialized() => WorkspaceState.Changed += OnWorkspaceChanged;

    protected override void OnParametersSet()
    {
        if (_resultTransactionId == Transaction.TransactionId.Value)
            return;

        _result = null;
        _errorMessage = null;
        _resultTransactionId = Transaction.TransactionId.Value;
    }

    private async Task ValidateAsync()
    {
        _isValidating = true;
        _errorMessage = null;

        var result = await TransactionService.ValidateAsync(Transaction.TransactionId, CancellationToken.None);
        if (result.IsSuccess)
        {
            _result = TransactionValidationMapper.ToViewModel(result.Value!);
        }
        else
        {
            _result = null;
            _errorMessage = result.Error!.Message;
        }

        _isValidating = false;
    }

    private string GetNodeUrl(Guid nodeId)
    {
        var audienceQuery = string.IsNullOrWhiteSpace(WorkspaceState.CurrentAudienceId)
            ? string.Empty
            : $"&audienceId={Uri.EscapeDataString(WorkspaceState.CurrentAudienceId)}";

        return $"/knowledge/{nodeId:D}?transactionId={Transaction.TransactionId.Value:D}{audienceQuery}";
    }

    private void OnWorkspaceChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => WorkspaceState.Changed -= OnWorkspaceChanged;
}
