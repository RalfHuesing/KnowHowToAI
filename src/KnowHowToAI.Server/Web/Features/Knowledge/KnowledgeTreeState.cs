using System.Collections.Concurrent;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Flüchtiger Circuit-State und Lazy-Loading-Datenadapter für den Wissensbaum.
/// Verwaltet Root, geladene Seiten, Auswahl, Paging, LRU-Cache-Eviction (maximal 10 Seiten)
/// und isolierte Request-Cancellation.
/// </summary>
public sealed class KnowledgeTreeState : IKnowledgeTreeWorkspace, IDisposable
{
    internal const int PageLimit = 100;

    private readonly NavigationService _navigationService;
    private readonly KnowledgeTreePageCache _cache = new();
    private readonly Dictionary<Guid, KnowledgeTreeNodeViewModel> _knownNodes = new();
    private readonly ConcurrentDictionary<Guid, ActiveNodeRequest> _activeNodeRequests = new();
    private long _nextRequestId;

    private int _contextGeneration;
    private CancellationTokenSource _globalCts = new();
    private bool _isDisposed;

    public KnowledgeTreeState(NavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public KnowledgeTreeNodeViewModel? RootNode { get; private set; }

    public KnowledgeTreeNodeViewModel? VisualRootNode =>
        VisualRootNodeId.HasValue && RootNode is not null
            ? FindNode(VisualRootNodeId.Value) ?? RootNode
            : RootNode;

    public Guid? SelectedNodeId { get; private set; }

    public string? StatusMessage { get; private set; }

    public bool IsLoading { get; private set; }

    public string? RootError { get; private set; }

    public IReadOnlyList<KnowledgeTreeNodeViewModel> Breadcrumbs => GetBreadcrumbPath();

    public event Action? Changed;

    internal ReadContext CurrentReadContext { get; private set; } = new();

    internal string? CurrentRoleId { get; private set; }

    internal Guid? VisualRootNodeId { get; private set; }

    internal int LoadedPageCount => _cache.LoadedPageCount;

    internal int KnownNodeCount => _knownNodes.Count;

    bool IKnowledgeTreeWorkspace.HasContext(ReadContext readContext, string roleId) =>
        CurrentReadContext == readContext && CurrentRoleId == roleId;

    public async Task InitializeAsync(
        ReadContext context,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);

        ThrowIfDisposed();

        var generation = Interlocked.Increment(ref _contextGeneration);
        CancelAllActiveRequests();

        CurrentReadContext = context;
        CurrentRoleId = roleId;
        SelectedNodeId = null;
        VisualRootNodeId = null;
        RootNode = null;
        RootError = null;
        StatusMessage = null;

        _cache.Clear();
        _knownNodes.Clear();

        IsLoading = true;
        Changed?.Invoke();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token, cancellationToken);
        await LoadRootAsync(context, roleId, generation, linkedCts.Token).ConfigureAwait(false);
    }

    public async Task ExpandNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var node = FindNode(nodeId);
        if (node is null || !node.HasChildren || (node.IsExpanded && node.Children.Count > 0))
            return;

        var success = await LoadChildrenPageAsync(node, cursor: null, cancellationToken).ConfigureAwait(false);
        if (success)
        {
            node.IsExpanded = true;
            Changed?.Invoke();
        }
    }

    public void CollapseNode(Guid nodeId)
    {
        ThrowIfDisposed();

        var node = FindNode(nodeId);
        if (node is null)
            return;

        CancelActiveNodeRequest(nodeId);
        node.IsExpanded = false;
        Changed?.Invoke();
    }

    public async Task PageNextAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var node = FindNode(nodeId);
        if (node is null || string.IsNullOrEmpty(node.NextCursor))
            return;

        var nextCursor = node.NextCursor;
        _cache.PushCurrentCursor(nodeId);

        var success = await LoadChildrenPageAsync(node, nextCursor, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            _cache.TryPopPreviousCursor(nodeId, out _);
        }
    }

    public async Task PagePreviousAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var node = FindNode(nodeId);
        if (node is null || !_cache.TryPopPreviousCursor(nodeId, out var previousCursor))
            return;

        var success = await LoadChildrenPageAsync(node, previousCursor, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            _cache.RestoreCursor(nodeId, previousCursor);
        }
    }

    public async Task SelectNodeAsync(Guid? nodeId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (SelectedNodeId == nodeId && nodeId is not null && _knownNodes.ContainsKey(nodeId.Value))
            return;

        if (nodeId.HasValue && !_knownNodes.ContainsKey(nodeId.Value))
        {
            var loader = new KnowledgeTreePathLoader(
                _navigationService,
                (id, ct) => ExpandNodeAsync(id, ct),
                (id, ct) => PageNextAsync(id, ct),
                id => _knownNodes.ContainsKey(id),
                FindNode);

            await loader.EnsurePathLoadedAsync(nodeId.Value, CurrentReadContext, CurrentRoleId, cancellationToken).ConfigureAwait(false);
        }

        SelectedNodeId = nodeId;
        UpdateSelectionFlags(nodeId);

        if (nodeId.HasValue && _knownNodes.TryGetValue(nodeId.Value, out var selectedNode))
        {
            if (selectedNode.ParentNodeId.HasValue)
            {
                _cache.RecordAccess(selectedNode.ParentNodeId.Value);
            }

            await RecenterIfAboveVisualRootAsync(selectedNode, cancellationToken).ConfigureAwait(false);
        }

        PruneOffPathNodes();
        Changed?.Invoke();
    }

    private void UpdateSelectionFlags(Guid? selectedNodeId)
    {
        foreach (var node in _knownNodes.Values)
        {
            node.IsSelected = node.NodeId == selectedNodeId;
        }
    }

    private async Task RecenterIfAboveVisualRootAsync(
        KnowledgeTreeNodeViewModel selectedNode,
        CancellationToken cancellationToken)
    {
        if (VisualRootNode is null || selectedNode.Depth >= VisualRootNode.Depth)
            return;

        VisualRootNodeId = selectedNode.ParentNodeId ?? RootNode?.NodeId;

        if (selectedNode.ParentNodeId.HasValue && !_cache.IsLoaded(selectedNode.ParentNodeId.Value))
        {
            await ExpandNodeAsync(selectedNode.ParentNodeId.Value, cancellationToken).ConfigureAwait(false);
        }
        else if (RootNode is not null && !_cache.IsLoaded(RootNode.NodeId))
        {
            await ExpandNodeAsync(RootNode.NodeId, cancellationToken).ConfigureAwait(false);
        }

        StatusMessage = $"Der Baum wurde auf „{VisualRootNode?.Title ?? RootNode?.Title}“ zentriert.";
    }

    internal KnowledgeTreeNodeViewModel? FindNode(Guid nodeId) =>
        _knownNodes.TryGetValue(nodeId, out var node) ? node : null;

    private async Task LoadRootAsync(
        ReadContext context,
        string roleId,
        int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _navigationService.GetRootAsync(
                context,
                new RoleId(roleId),
                cancellationToken).ConfigureAwait(false);

            if (generation != _contextGeneration)
                return;

            if (!result.IsSuccess)
            {
                RootError = result.Error!.Message;
                RootNode = null;
            }
            else if (result.Value?.Node is not null)
            {
                var rootVm = KnowledgeTreeNodeViewModel.FromRoot(result.Value, childCount: 1);
                RootNode = rootVm;
                VisualRootNodeId = rootVm.NodeId;
                _knownNodes[rootVm.NodeId] = rootVm;
            }
        }
        catch (OperationCanceledException) when (generation != _contextGeneration || cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            if (generation == _contextGeneration)
            {
                RootError = ex.Message;
                RootNode = null;
            }
        }
        finally
        {
            if (generation == _contextGeneration)
            {
                IsLoading = false;
                Changed?.Invoke();
            }
        }
    }

    private bool IsCurrentRequest(Guid nodeId, long requestId, int generation) =>
        _contextGeneration == generation &&
        _activeNodeRequests.TryGetValue(nodeId, out var active) &&
        active.RequestId == requestId;

    private ActiveNodeRequest RegisterActiveRequest(
        Guid nodeId,
        int generation,
        CancellationToken cancellationToken)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token, cancellationToken);
        var request = new ActiveNodeRequest(requestId, generation, cts);

        _activeNodeRequests.AddOrUpdate(
            nodeId,
            request,
            (_, old) =>
            {
                try { old.Cts.Cancel(); } catch (ObjectDisposedException) { }
                old.Cts.Dispose();
                return request;
            });

        return request;
    }

    private void CompleteActiveRequest(
        Guid nodeId,
        ActiveNodeRequest request,
        KnowledgeTreeNodeViewModel node)
    {
        if (IsCurrentRequest(nodeId, request.RequestId, request.ContextGeneration))
        {
            node.IsLoading = false;
            Changed?.Invoke();
            _activeNodeRequests.TryRemove(new KeyValuePair<Guid, ActiveNodeRequest>(nodeId, request));
        }

        request.Cts.Dispose();
    }

    private async Task<bool> LoadChildrenPageAsync(
        KnowledgeTreeNodeViewModel node,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var generation = _contextGeneration;
        var nodeId = node.NodeId;
        var activeRequest = RegisterActiveRequest(nodeId, generation, cancellationToken);

        node.IsLoading = true;
        node.Error = null;
        Changed?.Invoke();

        try
        {
            var query = new ListChildrenQuery(
                new NodeId(nodeId),
                CurrentReadContext,
                new RoleId(CurrentRoleId ?? string.Empty),
                Limit: PageLimit,
                Cursor: cursor);

            var result = await _navigationService.ListChildrenAsync(query, activeRequest.Cts.Token).ConfigureAwait(false);

            if (!IsCurrentRequest(nodeId, activeRequest.RequestId, generation))
                return false;

            if (!result.IsSuccess)
            {
                node.Error = result.Error!.Message;
                return false;
            }

            if (!_cache.IsLoaded(nodeId))
            {
                ApplyEvictionIfNecessary();
            }

            ApplyPageResult(node, result.Value!, cursor);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            if (IsCurrentRequest(nodeId, activeRequest.RequestId, generation))
            {
                node.Error = ex.Message;
            }
            return false;
        }
        finally
        {
            CompleteActiveRequest(nodeId, activeRequest, node);
        }
    }

    private void ApplyPageResult(KnowledgeTreeNodeViewModel node, ChildrenPage page, string? cursor)
    {
        var childViewModels = new List<KnowledgeTreeNodeViewModel>(page.Items.Count);

        foreach (var item in page.Items)
        {
            var childVm = KnowledgeTreeNodeViewModel.FromChildSummary(item, node.NodeId, node.Depth + 1);
            if (childVm.NodeId == SelectedNodeId)
            {
                childVm.IsSelected = true;
            }

            childViewModels.Add(childVm);
            _knownNodes[childVm.NodeId] = childVm;
        }

        if (node.NodeId == RootNode?.NodeId)
        {
            node.ChildCount = childViewModels.Count;
        }

        node.Children = childViewModels;
        node.NextCursor = page.NextCursor;
        node.HasPreviousPage = _cache.HasPreviousCursor(node.NodeId);

        _cache.RecordPageLoaded(node.NodeId, cursor);
        PruneOffPathNodes();
    }

    private void PruneOffPathNodes()
    {
        var context = new CircuitPruneContext(RootNode, _cache, _knownNodes);
        var (selectedId, visualRootId) = KnowledgeTreeCircuitPruner.Prune(context, SelectedNodeId, VisualRootNodeId);
        SelectedNodeId = selectedId;
        VisualRootNodeId = visualRootId;
    }

    private void ApplyEvictionIfNecessary()
    {
        var eviction = _cache.EvictIfNecessary(FindNode, SelectedNodeId);
        if (eviction is null)
            return;

        StatusMessage = eviction.StatusMessage;
        if (eviction.NewVisualRoot is not null)
        {
            VisualRootNodeId = eviction.NewVisualRoot.NodeId;
        }
    }

    private IReadOnlyList<KnowledgeTreeNodeViewModel> GetBreadcrumbPath()
    {
        if (SelectedNodeId is not { } selectedId || !_knownNodes.TryGetValue(selectedId, out var current))
            return RootNode is not null ? new[] { RootNode } : Array.Empty<KnowledgeTreeNodeViewModel>();

        var path = new List<KnowledgeTreeNodeViewModel>();
        for (var node = current; node is not null; node = node.ParentNodeId is { } parentId && _knownNodes.TryGetValue(parentId, out var parent) ? parent : null)
        {
            path.Insert(0, node);
        }

        return path;
    }

    private void CancelActiveNodeRequest(Guid nodeId)
    {
        if (_activeNodeRequests.TryRemove(nodeId, out var existing))
        {
            try { existing.Cts.Cancel(); } catch (ObjectDisposedException) { }
            existing.Cts.Dispose();
        }
    }

    private void CancelAllActiveRequests()
    {
        try { _globalCts.Cancel(); } catch (ObjectDisposedException) { }
        _globalCts.Dispose();
        _globalCts = new CancellationTokenSource();

        foreach (var kvp in _activeNodeRequests)
        {
            if (_activeNodeRequests.TryRemove(kvp.Key, out var req))
            {
                try { req.Cts.Cancel(); } catch (ObjectDisposedException) { }
                req.Cts.Dispose();
            }
        }

        _activeNodeRequests.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        CancelAllActiveRequests();
        _globalCts.Dispose();
    }
}
