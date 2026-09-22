using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>Setzt URL, Zielgruppe, Wissensbaum und Node-Dokument zusammen.</summary>
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

    [SupplyParameterFromQuery(Name = "audienceId")]
    private string? QueryAudienceId { get; set; }

    private string? _errorMessage;
    private bool _hasNoAudiences;
    private bool _isAwaitingAudienceSelection;
    private bool _isDisposed;

    private string CurrentPageTitle => TreeWorkspace.Breadcrumbs.LastOrDefault()?.Title ?? "Wissensbasis";

    protected override async Task OnParametersSetAsync()
    {
        _errorMessage = null;
        _hasNoAudiences = false;
        _isAwaitingAudienceSelection = false;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var resolution = await ReadContextResolver.ResolveAsync(
            QueryTransactionId,
            null,
            null,
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
            ContextSelector.Open(ContextSelectorMode.MandatoryAudience, context.ReadContext, null);
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
        var target = $"{path}{uri.Query}";
        if (!string.Equals(uri.PathAndQuery, target, StringComparison.OrdinalIgnoreCase))
            NavigationManager.NavigateTo(target);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        WorkspaceState.Changed -= HandleWorkspaceChanged;
    }
}
