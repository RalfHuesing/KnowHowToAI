using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Routable Wissenscockpit-Seite (/knowledge und /knowledge/{NodeId:guid}).
/// Rekonstruiert den Arbeitskontext aus Route und Query und orchestriert
/// Tree, Breadcrumbs und Workspace-State.
/// </summary>
public sealed partial class KnowledgePage : IDisposable
{
    [Inject]
    private KnowledgeTreeState TreeState { get; set; } = default!;

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

    [Parameter]
    public Guid? NodeId { get; set; }

    private bool _isDisposed;

    protected override async Task OnParametersSetAsync()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var queryParams = QueryHelpers.ParseQuery(uri.Query);

        queryParams.TryGetValue("transactionId", out var txIdRaw);
        queryParams.TryGetValue("snapshotId", out var snapIdRaw);
        queryParams.TryGetValue("releaseId", out var relIdRaw);
        queryParams.TryGetValue("roleId", out var roleIdRaw);

        var contextResolution = await ReadContextResolver.ResolveAsync(
            txIdRaw.FirstOrDefault(),
            snapIdRaw.FirstOrDefault(),
            relIdRaw.FirstOrDefault(),
            CancellationToken.None);

        if (!contextResolution.IsSuccess)
        {
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Ungültiger Kontext"));
            return;
        }

        var readContext = contextResolution.Value!.ReadContext;
        var contextVm = contextResolution.Value.ContextViewModel;
        PageRegions.SetKnowledgeContext(contextVm);

        var roleId = roleIdRaw.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(roleId))
        {
            roleId = WorkspaceState.CurrentRoleId;
        }

        if (string.IsNullOrWhiteSpace(roleId))
        {
            var rolesResult = await NavigationService.ListRolesAsync(
                new ListRolesQuery(readContext, Limit: 1),
                CancellationToken.None);

            if (rolesResult.IsSuccess && rolesResult.Value?.Items.Count > 0)
            {
                roleId = rolesResult.Value.Items[0].RoleId.Value;
            }
            else
            {
                roleId = "Default";
            }
        }

        WorkspaceState.SetContext(contextVm, readContext);
        WorkspaceState.SetRole(roleId);

        if (TreeState.CurrentReadContext != readContext || TreeState.CurrentRoleId != roleId)
        {
            await TreeState.InitializeAsync(readContext, roleId, CancellationToken.None);
        }

        if (NodeId.HasValue)
        {
            await TreeState.SelectNodeAsync(NodeId.Value, CancellationToken.None);
            WorkspaceState.SetNode(NodeId.Value);
        }
        else if (TreeState.SelectedNodeId.HasValue)
        {
            await TreeState.SelectNodeAsync(null, CancellationToken.None);
            WorkspaceState.SetNode(null);
        }
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
