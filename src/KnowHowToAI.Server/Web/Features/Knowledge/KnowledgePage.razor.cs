using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Content;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Routable Wissenscockpit-Seite (/knowledge und /knowledge/{NodeId:guid}).
/// Rekonstruiert den Arbeitskontext aus Route und Query und orchestriert
/// Tree, Breadcrumbs, Workspace-State und Node-Detailansicht.
/// Setzt O-008 verbindlich um: keine stille Standardzielgruppe; wenn kein Eintrag
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
    private IContextSelectionAudienceCatalog AudienceCatalog { get; set; } = default!;

    [Inject]
    private IAudienceStorageService AudienceStorage { get; set; } = default!;

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

    [SupplyParameterFromQuery(Name = "audienceId")]
    private string? QueryAudienceId { get; set; }

    private string? _errorMessage;
    private bool _hasNoAudiences;
    private bool _isAwaitingAudienceSelection;
    private bool _isDisposed;

    protected override async Task OnParametersSetAsync()
    {
        _errorMessage = null;
        _hasNoAudiences = false;
        _isAwaitingAudienceSelection = false;

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

        var audiencesResult = await AudienceCatalog.LoadAsync(readContext, CancellationToken.None);
        if (!audiencesResult.IsSuccess)
        {
            _errorMessage = audiencesResult.ErrorMessage!;
            PageRegions.SetKnowledgeContext(contextVm with
            {
                DisplayName = contextVm.DisplayName ?? "Fehlerhafter Kontext"
            });
            return;
        }

        var availableAudiences = audiencesResult.Audiences;
        if (availableAudiences.Count == 0)
        {
            ApplyEmptyAudiencesState(contextVm, readContext);
            return;
        }

        var audienceId = await ResolveEffectiveAudienceAsync(availableAudiences, QueryAudienceId, uri);

        if (string.IsNullOrWhiteSpace(audienceId))
        {
            _isAwaitingAudienceSelection = true;
            PageRegions.SetKnowledgeContext(contextVm with { AudienceName = null });
            ContextSelector.Open(ContextSelectorMode.MandatoryAudience, readContext, null);
            return;
        }

        await ApplySelectedAudienceAndInitializeAsync(contextVm, readContext, availableAudiences, audienceId, contextResolution.Value!.ChangeVersion);
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

    private void ApplyEmptyAudiencesState(KnowledgeContextViewModel contextVm, ReadContext readContext)
    {
        _hasNoAudiences = true;
        var emptyAudiencesContextVm = contextVm with { AudienceName = null };
        PageRegions.SetKnowledgeContext(emptyAudiencesContextVm);
        WorkspaceState.SetContext(emptyAudiencesContextVm, readContext);
        WorkspaceState.SetAudience(null);
    }

    private async Task<string?> ResolveEffectiveAudienceAsync(
        IReadOnlyList<ContextSelectionAudienceOptionViewModel> availableAudiences,
        string? queryAudienceId,
        Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(queryAudienceId))
        {
            if (availableAudiences.Any(r => r.Id == queryAudienceId))
            {
                await AudienceStorage.SetLastAudienceIdAsync(queryAudienceId);
                return queryAudienceId;
            }

            _errorMessage = $"[RequestedAudienceNotFound] Die angefragte Zielgruppe '{queryAudienceId}' ist im gewählten Kontext nicht verfügbar.";
            return null;
        }

        var lastAudienceId = await AudienceStorage.GetLastAudienceIdAsync();
        if (!string.IsNullOrWhiteSpace(lastAudienceId) && availableAudiences.Any(r => r.Id == lastAudienceId))
        {
            UpdateUrlWithAudience(uri, lastAudienceId);
            return lastAudienceId;
        }

        return null;
    }

    private async Task ApplySelectedAudienceAndInitializeAsync(
        KnowledgeContextViewModel contextVm,
        ReadContext readContext,
        IReadOnlyList<ContextSelectionAudienceOptionViewModel> availableAudiences,
        string audienceId,
        long? changeVersion)
    {
        var matchedAudience = availableAudiences.First(r => r.Id == audienceId);
        var effectiveContextVm = contextVm with { AudienceName = matchedAudience.Name, ChangeVersion = changeVersion };
        PageRegions.SetKnowledgeContext(effectiveContextVm);

        WorkspaceState.SetContext(effectiveContextVm, readContext);
        WorkspaceState.SetChangeVersion(changeVersion);
        WorkspaceState.SetAudience(audienceId);

        if (!TreeWorkspace.HasContext(readContext, audienceId))
        {
            await TreeWorkspace.InitializeAsync(readContext, audienceId, CancellationToken.None);
        }

        await ApplyNodeSelectionAsync(readContext, audienceId);
    }

    private async Task ApplyNodeSelectionAsync(ReadContext readContext, string audienceId)
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
        if (WorkspaceState.CurrentAudienceId is not { } audienceId || !WorkspaceState.ActiveTransactionId.HasValue)
            return;

        WorkspaceState.SetChangeVersion(mutation.ChangeVersion);
        var updatedContext = WorkspaceState.CurrentContext with { ChangeVersion = mutation.ChangeVersion };
        WorkspaceState.SetContext(updatedContext, WorkspaceState.CurrentReadContext);
        PageRegions.SetKnowledgeContext(updatedContext);

        await TreeWorkspace.InitializeAsync(WorkspaceState.CurrentReadContext, audienceId, CancellationToken.None);
        var selectedNodeId = mutation.Node.IsDeleted ? mutation.Node.ParentNodeId?.Value : mutation.Node.NodeId.Value;
        await TreeWorkspace.SelectNodeAsync(selectedNodeId, CancellationToken.None);
        WorkspaceState.SetNode(selectedNodeId);
        NavigateToSelection(selectedNodeId);
    }

    private async Task HandleContentMutationSucceededAsync(ContentMutationUseCaseResult mutation)
    {
        WorkspaceState.SetChangeVersion(mutation.ChangeVersion);
        var updatedContext = WorkspaceState.CurrentContext with { ChangeVersion = mutation.ChangeVersion };
        WorkspaceState.SetContext(updatedContext, WorkspaceState.CurrentReadContext);
        PageRegions.SetKnowledgeContext(updatedContext);

        if (WorkspaceState.CurrentAudienceId is { } audienceId)
        {
            await TreeWorkspace.InitializeAsync(WorkspaceState.CurrentReadContext, audienceId, CancellationToken.None);
            await TreeWorkspace.SelectNodeAsync(NodeId, CancellationToken.None);
        }
    }

    private void UpdateUrlWithAudience(Uri currentUri, string audienceId)
    {
        var query = QueryHelpers.ParseQuery(currentUri.Query);
        if (query.TryGetValue("audienceId", out var existing) && existing == audienceId)
        {
            return;
        }

        var dict = new Dictionary<string, string?>();
        foreach (var kvp in query)
        {
            dict[kvp.Key] = kvp.Value[0];
        }
        dict["audienceId"] = audienceId;

        var newUrl = QueryHelpers.AddQueryString(currentUri.AbsolutePath, dict);
        NavigationManager.NavigateTo(newUrl);
    }

    private void NavigateToNode(Guid nodeId)
    {
        NavigateToSelection(nodeId);
    }

    private void NavigateToSelection(Guid? nodeId)
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = uri.Query;
        var path = nodeId.HasValue ? $"/knowledge/{nodeId.Value:D}" : "/knowledge";
        NavigationManager.NavigateTo($"{path}{query}");
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        WorkspaceState.Changed -= HandleWorkspaceChanged;
    }
}
