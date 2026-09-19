using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

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
    private NavigationManager NavigationManager { get; set; } = default!;

    private async Task HandleBeforeInternalNavigation(LocationChangingContext context)
    {
        if (PageRegions.KnowledgeContext?.IsDirty == true && !_isConfirmingNavigation)
        {
            context.PreventNavigation();
            _targetNavigationLocation = context.TargetLocation;
            if (_confirmationDialog is not null)
            {
                await _confirmationDialog.OpenAsync();
            }
        }
    }

    private async Task ConfirmLeavePageAsync()
    {
        _isConfirmingNavigation = true;
        if (_confirmationDialog is not null)
        {
            await _confirmationDialog.CloseAsync();
        }

        WorkspaceState?.SetDirty(false);

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
