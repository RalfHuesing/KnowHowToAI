using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Drafts;

public sealed partial class DraftPage : ComponentBase
{
    [Inject]
    private TransactionService TransactionService { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Parameter]
    public Guid TransactionId { get; set; }

    private KnowledgeTransaction? _transaction;
    private string? _errorMessage;
    private bool _isLoading = true;

    private string PageTitle => string.IsNullOrWhiteSpace(_transaction?.Purpose)
        ? "Entwurf prüfen"
        : $"Entwurf: {_transaction.Purpose}";

    private string WorkingUrl => string.IsNullOrWhiteSpace(WorkspaceState.CurrentAudienceId)
        ? $"/knowledge?transactionId={TransactionId:D}"
        : $"/knowledge?transactionId={TransactionId:D}&audienceId={Uri.EscapeDataString(WorkspaceState.CurrentAudienceId)}";

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _transaction = null;
        _errorMessage = null;

        var result = await TransactionService.GetAsync(new TransactionId(TransactionId), CancellationToken.None);
        if (!result.IsSuccess)
        {
            _errorMessage = result.Error!.Message;
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Entwurf nicht verfügbar"));
        }
        else
        {
            _transaction = result.Value!;
            if (_transaction.State == TransactionState.Open)
            {
                var context = new KnowledgeContextViewModel(
                    KnowledgeReadContextKind.Transaction,
                    ContextId: _transaction.TransactionId.Value.ToString("D"),
                    DisplayName: string.IsNullOrWhiteSpace(_transaction.Purpose)
                        ? $"Entwurf {_transaction.TransactionId.Value:D}"
                        : _transaction.Purpose,
                    AudienceName: WorkspaceState.CurrentAudienceId,
                    BaseSnapshotId: _transaction.BaseSnapshotId.Value);

                PageRegions.SetKnowledgeContext(context);
                WorkspaceState.SetContext(context, new ReadContext(TransactionId: _transaction.TransactionId));
                WorkspaceState.SetChangeVersion(_transaction.ChangeVersion);
            }
            else
            {
                var currentContext = new KnowledgeContextViewModel(KnowledgeReadContextKind.Current);
                PageRegions.SetKnowledgeContext(currentContext);
                WorkspaceState.SetContext(currentContext, new ReadContext());
                WorkspaceState.SetChangeVersion(null);
            }
        }

        _isLoading = false;
    }
}
