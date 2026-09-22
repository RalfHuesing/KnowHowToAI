using KnowHowToAI.Core.Application.Transactions;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Drafts;

public sealed partial class DraftsPage : ComponentBase
{
    [Inject]
    private TransactionService TransactionService { get; set; } = default!;

    private IReadOnlyList<DraftItemViewModel> _drafts = [];
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var result = await TransactionService.ListOpenAsync(CancellationToken.None);
        _drafts = result.Select(transaction => new DraftItemViewModel(
            transaction.TransactionId.Value,
            transaction.Purpose,
            transaction.Actor,
            transaction.CreatedAtUtc,
            transaction.ChangeVersion)).ToArray();
        _isLoading = false;
    }
}
