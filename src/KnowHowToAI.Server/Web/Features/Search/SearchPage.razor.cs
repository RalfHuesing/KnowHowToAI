using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>
/// Paginierte, Zielgruppen- und kontextabhängige Wissenssuche. Der Cursor bleibt
/// featurelokal und wird ausschließlich als opaker Wert an den Search-Use-Case
/// zurückgegeben. Bei einem Kontextwechsel oder einer neuen Suche wird ein
/// noch laufender Request abgebrochen und kann das aktuelle Ergebnis nicht
/// überschreiben.
/// </summary>
public sealed partial class SearchPage : IDisposable
{
    [Inject]
    private SearchService SearchService { get; set; } = default!;

    [Inject]
    private NavigationService NavigationService { get; set; } = default!;

    [Inject]
    private IContextSelectionAudienceCatalog AudienceCatalog { get; set; } = default!;

    [Inject]
    private IWebReadContextResolver ReadContextResolver { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private IAudienceStorageService AudienceStorage { get; set; } = default!;

    [Inject]
    private ContextSelectorState ContextSelector { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "transactionId")]
    private string? QueryTransactionId { get; set; }

    [SupplyParameterFromQuery(Name = "snapshotId")]
    private string? QuerySnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "releaseId")]
    private string? QueryReleaseId { get; set; }

    [SupplyParameterFromQuery(Name = "audienceId")]
    private string? QueryAudienceId { get; set; }

    private readonly object _searchLock = new();
    private CancellationTokenSource? _searchCts;
    private SearchPageViewModel? _page;
    private SearchBreadcrumbLoader? _breadcrumbLoader;
    private ReadContext? _readContext;
    private string? _audienceId;
    private string? _activeText;
    private string? _contextErrorMessage;
    private string? _searchErrorMessage;
    private SearchFilterViewModel _filter = SearchFilterViewModel.Empty;
    private IReadOnlyList<SearchFilterOptionViewModel> _filterAudiences = [];
    private bool _hasNoAudiences;
    private bool _isReady;
    private bool _isSearching;
    private bool _isDisposed;
    private int _searchGeneration;

    protected override async Task OnParametersSetAsync()
    {
        ResetForContextChange();

        var resolution = await ReadContextResolver.ResolveAsync(
            QueryTransactionId,
            QuerySnapshotId,
            QueryReleaseId,
            CancellationToken.None);

        if (!resolution.IsSuccess)
        {
            _contextErrorMessage = resolution.Error!.Message;
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Ungültiger Kontext"));
            return;
        }

        await ApplyResolvedContextAsync(resolution.Value!);
    }

    private void ResetForContextChange()
    {
        CancelSearch(clearResults: true);
        _contextErrorMessage = null;
        _hasNoAudiences = false;
        _isReady = false;
        _readContext = null;
        _audienceId = null;
        _filter = SearchFilterViewModel.Empty;
        _filterAudiences = [];
    }

    private async Task ApplyResolvedContextAsync(WebReadContextResolution resolution)
    {
        var readContext = resolution.ReadContext;
        var contextViewModel = resolution.ContextViewModel;
        var audiencesResult = await AudienceCatalog.LoadAsync(readContext, CancellationToken.None);
        if (!audiencesResult.IsSuccess)
        {
            _contextErrorMessage = audiencesResult.ErrorMessage!;
            PageRegions.SetKnowledgeContext(contextViewModel with
            {
                DisplayName = contextViewModel.DisplayName ?? "Fehlerhafter Kontext"
            });
            return;
        }

        var audiences = audiencesResult.Audiences;
        if (audiences.Count == 0)
        {
            _hasNoAudiences = true;
            var emptyContext = contextViewModel with { AudienceName = null };
            PageRegions.SetKnowledgeContext(emptyContext);
            WorkspaceState.SetContext(emptyContext, readContext);
            WorkspaceState.SetAudience(null);
            return;
        }

        var audienceId = await ResolveEffectiveAudienceAsync(audiences, QueryAudienceId, NavigationManager.ToAbsoluteUri(NavigationManager.Uri));
        if (audienceId is null)
        {
            PageRegions.SetKnowledgeContext(contextViewModel with { AudienceName = null });
            ContextSelector.Open(ContextSelectorMode.MandatoryAudience, readContext, null);
            return;
        }

        var selectedAudience = audiences.First(audience => audience.Id == audienceId);
        _filterAudiences = audiences.Select(audience => new SearchFilterOptionViewModel(audience.Id, audience.Name)).ToArray();
        var selectedContext = contextViewModel with { AudienceName = selectedAudience.Name, ChangeVersion = resolution.ChangeVersion };
        PageRegions.SetKnowledgeContext(selectedContext);
        WorkspaceState.SetContext(selectedContext, readContext);
        WorkspaceState.SetChangeVersion(resolution.ChangeVersion);
        WorkspaceState.SetAudience(audienceId);
        _readContext = readContext;
        _audienceId = audienceId;
        _breadcrumbLoader = new SearchBreadcrumbLoader(NavigationService);
        _isReady = true;
    }

    private async Task<string?> ResolveEffectiveAudienceAsync(
        IReadOnlyList<ContextSelectionAudienceOptionViewModel> audiences,
        string? queryAudienceId,
        Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(queryAudienceId))
        {
            if (audiences.Any(audience => audience.Id == queryAudienceId))
            {
                await AudienceStorage.SetLastAudienceIdAsync(queryAudienceId);
                return queryAudienceId;
            }

            _contextErrorMessage = $"[RequestedAudienceNotFound] Die angefragte Zielgruppe '{queryAudienceId}' ist im gewählten Kontext nicht verfügbar.";
            return null;
        }

        var storedAudienceId = await AudienceStorage.GetLastAudienceIdAsync();
        if (string.IsNullOrWhiteSpace(storedAudienceId) || !audiences.Any(audience => audience.Id == storedAudienceId))
        {
            return null;
        }

        UpdateUrlWithAudience(uri, storedAudienceId);
        return storedAudienceId;
    }

    private Task StartSearchAsync(string text)
    {
        _activeText = text;
        return ExecuteSearchAsync(cursor: null);
    }

    private Task LoadNextPageAsync() => ExecuteSearchAsync(_page?.NextCursor);

    private Task ApplyFilterAsync(SearchFilterViewModel filter)
    {
        _filter = filter;
        CancelSearch(clearResults: false);
        _page = null;
        return _activeText is null ? Task.CompletedTask : ExecuteSearchAsync(cursor: null);
    }

    private async Task ExecuteSearchAsync(string? cursor)
    {
        if (_readContext is null || string.IsNullOrWhiteSpace(_audienceId) || _activeText is null)
        {
            return;
        }

        CancellationTokenSource requestCts;
        int generation;
        lock (_searchLock)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            requestCts = _searchCts;
            generation = ++_searchGeneration;
        }

        _isSearching = true;
        _searchErrorMessage = null;

        try
        {
            var query = new SearchQuery(
                _activeText,
                Cursor: cursor,
                AudienceId: new AudienceId(_audienceId),
                Filter: ToSearchFilter());
            var result = await SearchService.SearchAsync(query, _readContext, requestCts.Token);
            if (generation != _searchGeneration || requestCts.IsCancellationRequested)
            {
                return;
            }

            var mapped = SearchMapper.ToSearchPageResult(result);
            if (!mapped.IsSuccess)
            {
                _searchErrorMessage = mapped.Error!.Message;
                return;
            }

            var breadcrumbResult = await _breadcrumbLoader!.LoadAsync(
                mapped.Value!,
                _readContext,
                _audienceId,
                requestCts.Token);
            if (!breadcrumbResult.IsSuccess)
            {
                _searchErrorMessage = $"[{breadcrumbResult.Error!.Code}] {breadcrumbResult.Error.Message}";
                return;
            }

            _page = breadcrumbResult.Value;
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested)
        {
            // Eine überholte Suche ist kein Benutzerfehler und bleibt unsichtbar.
        }
        finally
        {
            if (generation == _searchGeneration)
            {
                _isSearching = false;
            }
        }
    }

    private void UpdateUrlWithAudience(Uri currentUri, string audienceId)
    {
        var query = QueryHelpers.ParseQuery(currentUri.Query)
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value[0]);
        query["audienceId"] = audienceId;
        NavigationManager.NavigateTo(QueryHelpers.AddQueryString(currentUri.AbsolutePath, query));
    }

    private void NavigateToNode(Guid nodeId)
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        NavigationManager.NavigateTo($"/knowledge/{nodeId}{uri.Query}");
    }

    private KnowHowToAI.Core.Application.Retrieval.Search.SearchFilter? ToSearchFilter()
    {
        if (_filter.IsEmpty)
            return null;

        return new KnowHowToAI.Core.Application.Retrieval.Search.SearchFilter(
            _filter.ResolvedAudienceIds.Select(value => new AudienceId(value)).ToArray(),
            _filter.Availabilities.Select(Enum.Parse<Availability>).ToArray(),
            _filter.Freshnesses.Select(Enum.Parse<Freshness>).ToArray(),
            _filter.FindingCodes);
    }

    private void CancelSearch(bool clearResults)
    {
        lock (_searchLock)
        {
            _searchGeneration++;
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;
        }

        _isSearching = false;
        _searchErrorMessage = null;
        if (clearResults)
        {
            _page = null;
            _activeText = null;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        CancelSearch(clearResults: false);
    }
}
