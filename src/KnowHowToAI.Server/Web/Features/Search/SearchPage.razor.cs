using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>
/// Paginierte, rollen- und kontextabhängige Wissenssuche. Der Cursor bleibt
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
    private IWebReadContextResolver ReadContextResolver { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private IRoleStorageService RoleStorage { get; set; } = default!;

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

    [SupplyParameterFromQuery(Name = "roleId")]
    private string? QueryRoleId { get; set; }

    private readonly object _searchLock = new();
    private CancellationTokenSource? _searchCts;
    private SearchPageViewModel? _page;
    private SearchBreadcrumbLoader? _breadcrumbLoader;
    private ReadContext? _readContext;
    private string? _roleId;
    private string? _activeText;
    private string? _contextErrorMessage;
    private string? _searchErrorMessage;
    private SearchFilterViewModel _filter = SearchFilterViewModel.Empty;
    private IReadOnlyList<SearchFilterOptionViewModel> _filterRoles = [];
    private bool _hasNoRoles;
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
        _hasNoRoles = false;
        _isReady = false;
        _readContext = null;
        _roleId = null;
        _filter = SearchFilterViewModel.Empty;
        _filterRoles = [];
    }

    private async Task ApplyResolvedContextAsync(WebReadContextResolution resolution)
    {
        var readContext = resolution.ReadContext;
        var contextViewModel = resolution.ContextViewModel;
        var rolesResult = await NavigationService.ListRolesAsync(
            new ListRolesQuery(readContext, Limit: 100),
            CancellationToken.None);

        if (!rolesResult.IsSuccess)
        {
            _contextErrorMessage = rolesResult.Error!.Message;
            PageRegions.SetKnowledgeContext(contextViewModel with
            {
                DisplayName = contextViewModel.DisplayName ?? "Fehlerhafter Kontext"
            });
            return;
        }

        var roles = rolesResult.Value?.Items ?? [];
        if (roles.Count == 0)
        {
            _hasNoRoles = true;
            var emptyContext = contextViewModel with { RoleName = null };
            PageRegions.SetKnowledgeContext(emptyContext);
            WorkspaceState.SetContext(emptyContext, readContext);
            WorkspaceState.SetRole(null);
            return;
        }

        var roleId = await ResolveEffectiveRoleAsync(roles, QueryRoleId, NavigationManager.ToAbsoluteUri(NavigationManager.Uri));
        if (roleId is null)
        {
            PageRegions.SetKnowledgeContext(contextViewModel with { RoleName = null });
            ContextSelector.Open(ContextSelectorMode.MandatoryRole, readContext, null);
            return;
        }

        var selectedRole = roles.First(role => role.RoleId.Value == roleId);
        _filterRoles = roles.Select(role => new SearchFilterOptionViewModel(role.RoleId.Value, role.Name)).ToArray();
        var selectedContext = contextViewModel with { RoleName = selectedRole.Name };
        PageRegions.SetKnowledgeContext(selectedContext);
        WorkspaceState.SetContext(selectedContext, readContext);
        WorkspaceState.SetRole(roleId);
        _readContext = readContext;
        _roleId = roleId;
        _breadcrumbLoader = new SearchBreadcrumbLoader(NavigationService);
        _isReady = true;
    }

    private async Task<string?> ResolveEffectiveRoleAsync(
        IReadOnlyList<Role> roles,
        string? queryRoleId,
        Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(queryRoleId) && roles.Any(role => role.RoleId.Value == queryRoleId))
        {
            await RoleStorage.SetLastRoleIdAsync(queryRoleId);
            return queryRoleId;
        }

        var storedRoleId = await RoleStorage.GetLastRoleIdAsync();
        if (string.IsNullOrWhiteSpace(storedRoleId) || !roles.Any(role => role.RoleId.Value == storedRoleId))
        {
            return null;
        }

        UpdateUrlWithRole(uri, storedRoleId);
        return storedRoleId;
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
        if (_readContext is null || string.IsNullOrWhiteSpace(_roleId) || _activeText is null)
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
                RoleId: new RoleId(_roleId),
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

            _page = await _breadcrumbLoader!.LoadAsync(
                mapped.Value!,
                _readContext,
                _roleId,
                requestCts.Token);
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

    private void UpdateUrlWithRole(Uri currentUri, string roleId)
    {
        var query = QueryHelpers.ParseQuery(currentUri.Query)
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value[0]);
        query["roleId"] = roleId;
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
            _filter.ResolvedRoleIds.Select(value => new RoleId(value)).ToArray(),
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
