using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
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

    [Parameter]
    public Guid TransactionId { get; set; }

    private KnowledgeTransaction? _transaction;
    private string? _errorMessage;
    private bool _isLoading = true;

    private string TransactionPageTitle => string.IsNullOrWhiteSpace(_transaction?.Purpose)
        ? "Transaction"
        : _transaction.Purpose;

    private string KnowledgeUrl => string.IsNullOrWhiteSpace(WorkspaceState.CurrentAudienceId)
        ? $"/knowledge?transactionId={TransactionId}"
        : $"/knowledge?transactionId={TransactionId}&audienceId={Uri.EscapeDataString(WorkspaceState.CurrentAudienceId)}";

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _errorMessage = null;

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
                AudienceName: WorkspaceState.CurrentAudienceId,
                BaseSnapshotId: _transaction.BaseSnapshotId.Value);

            PageRegions.SetKnowledgeContext(contextVm);
            WorkspaceState.SetContext(
                contextVm,
                new ReadContext(TransactionId: _transaction.TransactionId));
            WorkspaceState.SetChangeVersion(_transaction.ChangeVersion);
        }

        _isLoading = false;
    }
}
