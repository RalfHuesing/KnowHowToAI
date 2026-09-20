using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Routable Wissenscockpit-Seite (/knowledge und /knowledge/{NodeId:guid}).
/// Rekonstruiert den Arbeitskontext aus Route und Query und orchestriert
/// Tree, Breadcrumbs, Workspace-State und Node-Detailansicht.
/// Setzt O-008 verbindlich um: keine stille Standardrolle; wenn kein Eintrag
/// in Query oder localStorage vorhanden ist, erscheint der modale Pflichtauswahl-Selektor.
/// </summary>
public sealed partial class KnowledgePage : IDisposable
{
    [Inject]
    private IKnowledgeTreeWorkspace TreeWorkspace { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private WebReadContextResolver ReadContextResolver { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private IContextSelectionRoleCatalog RoleCatalog { get; set; } = default!;

    [Inject]
    private IRoleStorageService RoleStorage { get; set; } = default!;

    [Inject]
    private ContextSelectorState ContextSelector { get; set; } = default!;

    [Parameter]
    public Guid? NodeId { get; set; }

    [SupplyParameterFromQuery(Name = "transactionId")]
    private string? QueryTransactionId { get; set; }

    [SupplyParameterFromQuery(Name = "snapshotId")]
    private string? QuerySnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "releaseId")]
    private string? QueryReleaseId { get; set; }

    [SupplyParameterFromQuery(Name = "roleId")]
    private string? QueryRoleId { get; set; }

    private string? _errorMessage;
    private bool _hasNoRoles;
    private bool _isAwaitingRoleSelection;
    private bool _isDisposed;

    protected override async Task OnParametersSetAsync()
    {
        _errorMessage = null;
        _hasNoRoles = false;
        _isAwaitingRoleSelection = false;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

        var contextResolution = await ReadContextResolver.ResolveAsync(
            QueryTransactionId,
            QuerySnapshotId,
            QueryReleaseId,
            CancellationToken.None);

        if (!contextResolution.IsSuccess)
        {
            _errorMessage = contextResolution.Error!.Message;
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Ungültiger Kontext"));
            return;
        }

        var readContext = contextResolution.Value!.ReadContext;
        var contextVm = contextResolution.Value.ContextViewModel;

        var rolesResult = await RoleCatalog.LoadAsync(readContext, CancellationToken.None);
        if (!rolesResult.IsSuccess)
        {
            _errorMessage = rolesResult.ErrorMessage!;
            PageRegions.SetKnowledgeContext(contextVm with
            {
                DisplayName = contextVm.DisplayName ?? "Fehlerhafter Kontext"
            });
            return;
        }

        var availableRoles = rolesResult.Roles;
        if (availableRoles.Count == 0)
        {
            ApplyEmptyRolesState(contextVm, readContext);
            return;
        }

        var roleId = await ResolveEffectiveRoleAsync(availableRoles, QueryRoleId, uri);

        if (string.IsNullOrWhiteSpace(roleId))
        {
            _isAwaitingRoleSelection = true;
            PageRegions.SetKnowledgeContext(contextVm with { RoleName = null });
            ContextSelector.Open(ContextSelectorMode.MandatoryRole, readContext, null);
            return;
        }

        await ApplySelectedRoleAndInitializeAsync(contextVm, readContext, availableRoles, roleId, contextResolution.Value!.ChangeVersion);
    }

    protected override void OnInitialized() => WorkspaceState.Changed += HandleWorkspaceChanged;

    private void HandleWorkspaceChanged()
    {
        if (PageRegions.KnowledgeContext is { } context
            && context.IsDirty != WorkspaceState.IsDirty)
        {
            PageRegions.SetKnowledgeContext(context with { IsDirty = WorkspaceState.IsDirty });
        }
    }

    private void ApplyEmptyRolesState(KnowledgeContextViewModel contextVm, ReadContext readContext)
    {
        _hasNoRoles = true;
        var emptyRolesContextVm = contextVm with { RoleName = null };
        PageRegions.SetKnowledgeContext(emptyRolesContextVm);
        WorkspaceState.SetContext(emptyRolesContextVm, readContext);
        WorkspaceState.SetRole(null);
    }

    private async Task<string?> ResolveEffectiveRoleAsync(
        IReadOnlyList<ContextSelectionRoleOptionViewModel> availableRoles,
        string? queryRoleId,
        Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(queryRoleId))
        {
            if (availableRoles.Any(r => r.Id == queryRoleId))
            {
                await RoleStorage.SetLastRoleIdAsync(queryRoleId);
                return queryRoleId;
            }

            _errorMessage = $"[RequestedRoleNotFound] Die angefragte Rolle '{queryRoleId}' ist im gewählten Kontext nicht verfügbar.";
            return null;
        }

        var lastRoleId = await RoleStorage.GetLastRoleIdAsync();
        if (!string.IsNullOrWhiteSpace(lastRoleId) && availableRoles.Any(r => r.Id == lastRoleId))
        {
            UpdateUrlWithRole(uri, lastRoleId);
            return lastRoleId;
        }

        return null;
    }

    private async Task ApplySelectedRoleAndInitializeAsync(
        KnowledgeContextViewModel contextVm,
        ReadContext readContext,
        IReadOnlyList<ContextSelectionRoleOptionViewModel> availableRoles,
        string roleId,
        long? changeVersion)
    {
        var matchedRole = availableRoles.First(r => r.Id == roleId);
        var effectiveContextVm = contextVm with { RoleName = matchedRole.Name, ChangeVersion = changeVersion };
        PageRegions.SetKnowledgeContext(effectiveContextVm);

        WorkspaceState.SetContext(effectiveContextVm, readContext);
        WorkspaceState.SetChangeVersion(changeVersion);
        WorkspaceState.SetRole(roleId);

        if (!TreeWorkspace.HasContext(readContext, roleId))
        {
            await TreeWorkspace.InitializeAsync(readContext, roleId, CancellationToken.None);
        }

        await ApplyNodeSelectionAsync(readContext, roleId);
    }

    private async Task ApplyNodeSelectionAsync(ReadContext readContext, string roleId)
    {
        if (NodeId.HasValue)
        {
            await TreeWorkspace.SelectNodeAsync(NodeId.Value, CancellationToken.None);
            WorkspaceState.SetNode(NodeId.Value);
        }
        else if (TreeWorkspace.SelectedNodeId.HasValue)
        {
            await TreeWorkspace.SelectNodeAsync(null, CancellationToken.None);
            WorkspaceState.SetNode(null);
        }
    }

    private async Task HandleNodeMutationSucceededAsync(NodeMutationResult mutation)
    {
        if (WorkspaceState.CurrentRoleId is not { } roleId || !WorkspaceState.ActiveTransactionId.HasValue)
            return;

        WorkspaceState.SetChangeVersion(mutation.ChangeVersion);
        var updatedContext = WorkspaceState.CurrentContext with { ChangeVersion = mutation.ChangeVersion };
        WorkspaceState.SetContext(updatedContext, WorkspaceState.CurrentReadContext);
        PageRegions.SetKnowledgeContext(updatedContext);

        await TreeWorkspace.InitializeAsync(WorkspaceState.CurrentReadContext, roleId, CancellationToken.None);
        var selectedNodeId = mutation.Node.IsDeleted ? mutation.Node.ParentNodeId?.Value : mutation.Node.NodeId.Value;
        await TreeWorkspace.SelectNodeAsync(selectedNodeId, CancellationToken.None);
        WorkspaceState.SetNode(selectedNodeId);
    }

    private void UpdateUrlWithRole(Uri currentUri, string roleId)
    {
        var query = QueryHelpers.ParseQuery(currentUri.Query);
        if (query.TryGetValue("roleId", out var existing) && existing == roleId)
        {
            return;
        }

        var dict = new Dictionary<string, string?>();
        foreach (var kvp in query)
        {
            dict[kvp.Key] = kvp.Value[0];
        }
        dict["roleId"] = roleId;

        var newUrl = QueryHelpers.AddQueryString(currentUri.AbsolutePath, dict);
        NavigationManager.NavigateTo(newUrl);
    }

    private void NavigateToNode(Guid nodeId)
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = uri.Query;
        NavigationManager.NavigateTo($"/knowledge/{nodeId}{query}");
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        WorkspaceState.Changed -= HandleWorkspaceChanged;
    }
}
