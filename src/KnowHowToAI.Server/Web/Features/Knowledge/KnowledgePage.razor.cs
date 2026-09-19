using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
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
    private NavigationService NavigationService { get; set; } = default!;

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

    // Node-Detailansicht
    private NodeDetailsViewModel? _nodeDetailsViewModel;
    private bool _isLoadingNodeDetails;
    private string? _nodeDetailsErrorMessage;
    private bool _nodeDetailsNotFound;

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

        var rolesResult = await NavigationService.ListRolesAsync(
            new ListRolesQuery(readContext, Limit: 100),
            CancellationToken.None);

        if (!rolesResult.IsSuccess)
        {
            _errorMessage = rolesResult.Error!.Message;
            PageRegions.SetKnowledgeContext(contextVm with
            {
                DisplayName = contextVm.DisplayName ?? "Fehlerhafter Kontext"
            });
            return;
        }

        var availableRoles = rolesResult.Value?.Items ?? [];
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

        await ApplySelectedRoleAndInitializeAsync(contextVm, readContext, availableRoles, roleId);
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
        IReadOnlyList<Role> availableRoles,
        string? queryRoleId,
        Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(queryRoleId) && availableRoles.Any(r => r.RoleId.Value == queryRoleId))
        {
            await RoleStorage.SetLastRoleIdAsync(queryRoleId);
            return queryRoleId;
        }

        var lastRoleId = await RoleStorage.GetLastRoleIdAsync();
        if (!string.IsNullOrWhiteSpace(lastRoleId) && availableRoles.Any(r => r.RoleId.Value == lastRoleId))
        {
            UpdateUrlWithRole(uri, lastRoleId);
            return lastRoleId;
        }

        return null;
    }

    private async Task ApplySelectedRoleAndInitializeAsync(
        KnowledgeContextViewModel contextVm,
        ReadContext readContext,
        IReadOnlyList<Role> availableRoles,
        string roleId)
    {
        var matchedRole = availableRoles.First(r => r.RoleId.Value == roleId);
        var effectiveContextVm = contextVm with { RoleName = matchedRole.Name };
        PageRegions.SetKnowledgeContext(effectiveContextVm);

        WorkspaceState.SetContext(effectiveContextVm, readContext);
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
            await LoadNodeDetailsAsync(NodeId.Value, readContext, roleId);
        }
        else if (TreeWorkspace.SelectedNodeId.HasValue)
        {
            await TreeWorkspace.SelectNodeAsync(null, CancellationToken.None);
            WorkspaceState.SetNode(null);
            ClearNodeDetails();
        }
        else
        {
            ClearNodeDetails();
        }
    }

    private async Task LoadNodeDetailsAsync(Guid nodeId, ReadContext readContext, string roleId)
    {
        _isLoadingNodeDetails = true;
        _nodeDetailsErrorMessage = null;
        _nodeDetailsNotFound = false;
        _nodeDetailsViewModel = null;

        var result = await NavigationService.GetNodeAsync(
            new NodeId(nodeId),
            readContext,
            new RoleId(roleId),
            CancellationToken.None);

        _isLoadingNodeDetails = false;

        if (!result.IsSuccess)
        {
            var errorCode = result.Error!.Code;
            if (string.Equals(errorCode, "NodeNotFound", StringComparison.Ordinal))
                _nodeDetailsNotFound = true;
            else
                _nodeDetailsErrorMessage = result.Error.Message;
            return;
        }

        _nodeDetailsViewModel = KnowledgeNavigationMapper.ToNodeDetailsViewModel(
            result.Value,
            allDependencies: null,
            changeVersion: WorkspaceState.CurrentChangeVersion);
    }

    private void ClearNodeDetails()
    {
        _nodeDetailsViewModel = null;
        _isLoadingNodeDetails = false;
        _nodeDetailsErrorMessage = null;
        _nodeDetailsNotFound = false;
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
    }
}
