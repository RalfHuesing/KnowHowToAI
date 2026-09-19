using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Transactions;

public sealed partial class TransactionsPage : ComponentBase
{
    [Inject]
    private TransactionService TransactionService { get; set; } = default!;

    [Inject]
    private ICurrentUserService CurrentUserService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    private IReadOnlyList<TransactionItemViewModel> _transactions = [];
    private string _currentUserName = "System";
    internal string? NewPurpose { get; set; }
    internal string? NewClient { get; set; } = "Web UI";
    private string? _beginErrorMessage;
    private bool _isLoading = true;
    private bool _isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Current,
            DisplayName: "Transactions"));

        _currentUserName = CurrentUserService.GetCurrentUserName();
        await LoadOpenTransactionsAsync();
        _isLoading = false;
    }

    private async Task LoadOpenTransactionsAsync()
    {
        var list = await TransactionService.ListOpenAsync(CancellationToken.None);
        var now = Clock.UtcNow;

        _transactions = list.Select(tx => new TransactionItemViewModel(
            tx.TransactionId.Value,
            tx.BaseSnapshotId.Value,
            tx.WorkingSnapshotId.Value,
            tx.Purpose,
            tx.Actor,
            tx.Client,
            tx.CreatedAtUtc,
            tx.ChangeVersion,
            tx.CreatedAtUtc <= now.AddDays(-7))).ToList();
    }

    private async Task BeginTransactionAsync()
    {
        _isSubmitting = true;
        _beginErrorMessage = null;

        var options = new BeginTransactionOptions(
            NewPurpose,
            _currentUserName,
            NewClient);

        var result = await TransactionService.BeginAsync(options, CancellationToken.None);
        if (result.IsSuccess)
        {
            NavigationManager.NavigateTo($"/transactions/{result.Value!.TransactionId.Value}");
        }
        else
        {
            _beginErrorMessage = result.Error!.Message;
            _isSubmitting = false;
        }
    }
}
