using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Orchestriert den unpersistierten Auswahlvorgang des Kontextselektors.
/// Der Dialoghost bleibt dadurch auf Lifecycle und Zugänglichkeit begrenzt.
/// </summary>
public sealed partial class ContextSelectionForm : ComponentBase
{
    private readonly ContextSelectionDraft _draft = new();
    private ContextSelectionOptionsViewModel _options = ContextSelectionOptionsViewModel.Empty;
    private string? _selectedRoleId;
    private string? _errorMessage;
    private bool _isLoadingRoles;

    [Inject]
    private ContextSelectorState State { get; set; } = default!;

    [Inject]
    private IContextSelectionCatalog Catalog { get; set; } = default!;

    [Inject]
    private IContextSelectionRoleCatalog RoleCatalog { get; set; } = default!;

    [Inject]
    private IRoleStorageService RoleStorage { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    private bool _isConfirmingDirtySwitch;

    protected override async Task OnInitializedAsync()
    {
        _draft.ApplyInitialContext(State.InitialReadContext);
        _selectedRoleId = State.InitialRoleId;

        if (State.Mode == ContextSelectorMode.Full)
        {
            _options = await Catalog.LoadAsync(CancellationToken.None);
        }

        await RefreshRolesAsync();
    }

    private async Task SelectKindAsync(KnowledgeReadContextKind kind)
    {
        if (_draft.SelectedKind == kind)
            return;

        _draft.SelectedKind = kind;
        _errorMessage = null;
        await RefreshRolesAsync();
    }

    private async Task OnContextParameterChangedAsync()
    {
        _errorMessage = null;
        await RefreshRolesAsync();
    }

    private async Task RefreshRolesAsync()
    {
        _isLoadingRoles = true;
        _options = _options.WithRoles([]);

        var context = _draft.BuildReadContext(_options);
        if (!context.IsSuccess)
        {
            _isLoadingRoles = false;
            return;
        }

        var result = await RoleCatalog.LoadAsync(context.Value!, CancellationToken.None);
        _options = _options.WithRoles(result.Roles);
        _errorMessage = result.ErrorMessage;

        if (_selectedRoleId is not null && !_options.Roles.Any(role => role.Id == _selectedRoleId))
        {
            _selectedRoleId = null;
        }

        _isLoadingRoles = false;
    }

    private async Task ApplyAsync()
    {
        _errorMessage = ValidateSelection();
        if (_errorMessage is not null)
            return;

        if (WorkspaceState.IsDirty && !_isConfirmingDirtySwitch)
        {
            _isConfirmingDirtySwitch = true;
            _errorMessage = "Sie haben ungespeicherte Änderungen. Wenn Sie den Kontext wechseln, gehen diese verloren. Klicken Sie erneut auf 'Übernehmen', um trotzdem zu wechseln.";
            return;
        }

        if (WorkspaceState.IsDirty)
        {
            WorkspaceState.SetDirty(false);
        }

        if (!string.IsNullOrWhiteSpace(_selectedRoleId))
        {
            await RoleStorage.SetLastRoleIdAsync(_selectedRoleId);
        }

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var targetUrl = _draft.BuildTargetUrl(uri, State.Mode, _selectedRoleId);

        _isConfirmingDirtySwitch = false;
        State.Close();
        NavigationManager.NavigateTo(targetUrl);
    }

    private string? ValidateSelection()
    {
        var context = _draft.BuildReadContext(_options);
        if (!context.IsSuccess)
            return context.Error!.Message;

        return _options.Roles.Count > 0 && string.IsNullOrWhiteSpace(_selectedRoleId)
            ? "Bitte wählen Sie eine Rolle aus."
            : null;
    }

    private void Cancel()
    {
        _isConfirmingDirtySwitch = false;
        if (State.Mode == ContextSelectorMode.Full)
        {
            State.Close();
        }
    }
}
