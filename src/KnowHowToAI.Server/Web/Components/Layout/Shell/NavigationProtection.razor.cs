using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Components.Layout.Shell;

/// <summary>
/// Schützt ungespeicherte Seiteneingaben bei interner und externer Navigation.
/// </summary>
public sealed partial class NavigationProtection : ComponentBase
{
    private ConfirmationDialog? _confirmationDialog;
    private bool _isConfirmingNavigation;
    private string? _targetNavigationLocation;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private WorkspaceState? WorkspaceState { get; set; }

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private bool IsDirty => WorkspaceEditState.IsDirty;

    private async Task HandleBeforeInternalNavigation(LocationChangingContext context)
    {
        if (IsDirty
            && !_isConfirmingNavigation
            && !IsDraftContextSynchronization(context.TargetLocation))
        {
            context.PreventNavigation();
            _targetNavigationLocation = context.TargetLocation;
            if (_confirmationDialog is not null)
            {
                await _confirmationDialog.OpenAsync();
            }
        }
    }

    private bool IsDraftContextSynchronization(string targetLocation)
    {
        if (WorkspaceState?.ActiveTransactionId is not { } transactionId)
            return false;

        var currentUri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var targetUri = NavigationManager.ToAbsoluteUri(targetLocation);
        if (!string.Equals(currentUri.GetLeftPart(UriPartial.Authority), targetUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(currentUri.AbsolutePath, targetUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
            return false;

        var targetQuery = QueryHelpers.ParseQuery(targetUri.Query)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        if (!targetQuery.Remove("transactionId", out var selectedTransactions)
            || selectedTransactions.Length != 1
            || !string.Equals(selectedTransactions[0], transactionId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase))
            return false;

        var currentQuery = QueryHelpers.ParseQuery(currentUri.Query);
        var allowed = currentQuery.Count == targetQuery.Count
            && currentQuery.All(pair => targetQuery.TryGetValue(pair.Key, out var values)
                && pair.Value.SequenceEqual(values, StringComparer.Ordinal));
        return allowed;
    }

    private async Task ConfirmLeavePageAsync()
    {
        _isConfirmingNavigation = true;
        if (_confirmationDialog is not null)
        {
            await _confirmationDialog.CloseAsync();
        }

        WorkspaceEditState.SetDirty(false, null);

        if (PageRegions.KnowledgeContext is not null)
        {
            PageRegions.SetKnowledgeContext(PageRegions.KnowledgeContext with { IsDirty = false });
        }

        if (!string.IsNullOrEmpty(_targetNavigationLocation))
        {
            NavigationManager.NavigateTo(_targetNavigationLocation);
        }

        _isConfirmingNavigation = false;
        _targetNavigationLocation = null;
    }

    private async Task CancelLeavePageAsync()
    {
        _targetNavigationLocation = null;
        if (_confirmationDialog is not null)
        {
            await _confirmationDialog.CloseAsync();
        }
    }
}
