using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Server.Web.Features.Knowledge.Audiences;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>Setzt URL, Zielgruppe, Wissensbaum und Node-Dokument zusammen.</summary>
public sealed partial class KnowledgePage : IDisposable
{
    private readonly SemaphoreSlim _treeInitializationGate = new(1, 1);

    [Inject]
    private IKnowledgeTreeWorkspace TreeWorkspace { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private KnowledgePageContextResolver ReadContextResolver { get; set; } = default!;

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

    [SupplyParameterFromQuery(Name = "audienceId")]
    private string? QueryAudienceId { get; set; }

    private string? _errorMessage;
    private bool _hasNoAudiences;
    private bool _isAwaitingAudienceSelection;
    private bool _isDisposed;

    private string CurrentPageTitle => TreeWorkspace.Breadcrumbs.LastOrDefault()?.Title ?? "Wissensbasis";
    private bool CanManageTree => WorkspaceState.CurrentContext.ReadContext is KnowledgeReadContextKind.Current or KnowledgeReadContextKind.Transaction;

    protected override async Task OnParametersSetAsync()
    {
        _errorMessage = null;
        _hasNoAudiences = false;
        _isAwaitingAudienceSelection = false;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var resolution = await ReadContextResolver.ResolveAsync(
            QueryTransactionId,
            CancellationToken.None);

        if (!resolution.IsSuccess)
        {
            WorkspaceState.Reset();
            _errorMessage = resolution.Error!.Message;
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Ungültiger Kontext"));
            return;
        }

        var context = resolution.Value!;
        WorkspaceState.SetLoadedSnapshotId(context.LoadedSnapshotId);
        var audiences = await AudienceCatalog.LoadAsync(context.ReadContext, CancellationToken.None);
        if (!audiences.IsSuccess)
        {
            _errorMessage = audiences.ErrorMessage!;
            PageRegions.SetKnowledgeContext(context.ContextViewModel with
            {
                DisplayName = context.ContextViewModel.DisplayName ?? "Fehlerhafter Kontext"
            });
            return;
        }

        if (audiences.Audiences.Count == 0)
        {
            ApplyEmptyAudiencesState(context.ContextViewModel, context.ReadContext);
            return;
        }

        var audienceId = await ResolveEffectiveAudienceAsync(audiences.Audiences, QueryAudienceId, uri);
        if (_errorMessage is not null)
        {
            PageRegions.SetKnowledgeContext(context.ContextViewModel with { AudienceName = null });
            return;
        }

        if (audienceId is null)
        {
            _isAwaitingAudienceSelection = true;
            PageRegions.SetKnowledgeContext(context.ContextViewModel with { AudienceName = null });
            ContextSelector.Open(context.ReadContext);
            return;
        }

        await ApplySelectedAudienceAndInitializeAsync(
            context.ContextViewModel,
            context.ReadContext,
            audiences.Audiences,
            audienceId,
            context.ChangeVersion);
    }

    protected override void OnInitialized() => WorkspaceState.Changed += HandleWorkspaceChanged;

    private void HandleWorkspaceChanged()
    {
        if (_isDisposed)
            return;

        _ = InvokeAsync(() =>
        {
            if (PageRegions.KnowledgeContext is not null
                && PageRegions.KnowledgeContext != WorkspaceState.CurrentContext)
            {
                PageRegions.SetKnowledgeContext(WorkspaceState.CurrentContext);
            }

            StateHasChanged();
        });
    }

    private void ApplyEmptyAudiencesState(KnowledgeContextViewModel context, ReadContext readContext)
    {
        _hasNoAudiences = true;
        var emptyContext = context with { AudienceName = null };
        PageRegions.SetKnowledgeContext(emptyContext);
        WorkspaceState.SetContext(emptyContext, readContext);
        WorkspaceState.SetAudience(null);
    }

    private async Task<string?> ResolveEffectiveAudienceAsync(
        IReadOnlyList<ContextSelectionAudienceOptionViewModel> availableAudiences,
        string? queryAudienceId,
        Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(queryAudienceId))
        {
            if (availableAudiences.Any(audience => audience.Id == queryAudienceId))
            {
                await AudienceStorage.SetLastAudienceIdAsync(queryAudienceId);
                return queryAudienceId;
            }

            _errorMessage = $"[RequestedAudienceNotFound] Die angefragte Zielgruppe '{queryAudienceId}' ist im gewählten Kontext nicht verfügbar.";
            return null;
        }

        var lastAudienceId = await AudienceStorage.GetLastAudienceIdAsync();
        if (!string.IsNullOrWhiteSpace(lastAudienceId)
            && availableAudiences.Any(audience => audience.Id == lastAudienceId))
        {
            UpdateUrlWithAudience(uri, lastAudienceId);
            return lastAudienceId;
        }

        if (availableAudiences.Count == 1)
        {
            var onlyAudienceId = availableAudiences[0].Id;
            await AudienceStorage.SetLastAudienceIdAsync(onlyAudienceId);
            UpdateUrlWithAudience(uri, onlyAudienceId);
            return onlyAudienceId;
        }

        return null;
    }

    private async Task ApplySelectedAudienceAndInitializeAsync(
        KnowledgeContextViewModel context,
        ReadContext readContext,
        IReadOnlyList<ContextSelectionAudienceOptionViewModel> availableAudiences,
        string audienceId,
        long? changeVersion)
    {
        await _treeInitializationGate.WaitAsync();
        try
        {
            changeVersion = KnowledgePageChangeVersion.Resolve(
                readContext,
                WorkspaceState.ActiveTransactionId,
                WorkspaceState.CurrentChangeVersion,
                changeVersion);
            var selectedAudience = availableAudiences.First(audience => audience.Id == audienceId);
            var currentContext = context with
            {
                AudienceName = selectedAudience.Name,
                ChangeVersion = changeVersion
            };
            PageRegions.SetKnowledgeContext(currentContext);
            WorkspaceState.SetContext(currentContext, readContext);
            WorkspaceState.SetChangeVersion(changeVersion);
            WorkspaceState.SetAudience(audienceId);

            if (!TreeWorkspace.HasContext(readContext, audienceId))
                await TreeWorkspace.InitializeAsync(readContext, audienceId, CancellationToken.None);

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
        finally
        {
            _treeInitializationGate.Release();
        }
    }

    private void UpdateUrlWithAudience(Uri currentUri, string audienceId)
    {
        var query = QueryHelpers.ParseQuery(currentUri.Query);
        if (query.TryGetValue("audienceId", out var existing) && existing == audienceId)
            return;

        var values = query.ToDictionary(pair => pair.Key, pair => (string?)pair.Value[0]);
        values["audienceId"] = audienceId;
        var newUrl = QueryHelpers.AddQueryString(currentUri.AbsolutePath, values);
        NavigationManager.NavigateTo(newUrl);
    }

    private void NavigateToNode(Guid nodeId) => NavigateToSelection(nodeId);

    private void NavigateToSelection(Guid? nodeId)
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var path = nodeId.HasValue ? $"/knowledge/{nodeId.Value:D}" : "/knowledge";
        var query = QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
        if (WorkspaceState.ActiveTransactionId is { } transactionId)
        {
            query.Remove("snapshotId");
            query.Remove("releaseId");
            query["transactionId"] = transactionId.Value.ToString("D");
        }

        var target = QueryHelpers.AddQueryString(path, query);
        if (!string.Equals(uri.PathAndQuery, target, StringComparison.OrdinalIgnoreCase))
            NavigationManager.NavigateTo(target);
    }

    private async Task RefreshKnowledgeTreeAfterMetadataChangeAsync(NodeMutationResult mutation)
    {
        await _treeInitializationGate.WaitAsync();
        try
        {
            if (WorkspaceState.CurrentAudienceId is not { } audienceId)
                return;

            var nodeId = NodeId ?? mutation.Node.NodeId.Value;
            var readContext = WorkspaceState.CurrentReadContext;
            await TreeWorkspace.InitializeAsync(readContext, audienceId, CancellationToken.None);
            await TreeWorkspace.SelectNodeAsync(nodeId, CancellationToken.None);
            WorkspaceState.SetNode(nodeId);
            NavigateToSelection(nodeId);
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            _treeInitializationGate.Release();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        WorkspaceState.Changed -= HandleWorkspaceChanged;
    }
}
