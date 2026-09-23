using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>
/// Flüchtiger Circuit-State und Lazy-Loading-Datenadapter für den Wissensbaum.
/// Verwaltet Root, geladene Seiten, Auswahl, Paging, LRU-Cache-Eviction (maximal 10 Seiten)
/// und isolierte Request-Cancellation.
/// </summary>
public sealed partial class KnowledgeTreeState : IKnowledgeTreeWorkspace, IDisposable
{
    internal const int PageLimit = 100;

    private readonly NavigationService _navigationService;
    private readonly KnowledgeTreePageCache _cache = new();
    private readonly Dictionary<Guid, KnowledgeTreeNodeViewModel> _knownNodes = new();
    private readonly HashSet<Guid> _expandedNodeIds = new();
    private readonly KnowledgeTreeRequestCoordinator _requestCoordinator = new();
    private HashSet<Guid>? _activeTargetPath;

    private int _contextGeneration;
    private bool _isDisposed;

    public KnowledgeTreeState(NavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public KnowledgeTreeNodeViewModel? RootNode { get; private set; }

    public KnowledgeTreeNodeViewModel? VisualRootNode =>
        VisualRootNodeId.HasValue && RootNode is not null ? FindNode(VisualRootNodeId.Value) ?? RootNode : RootNode;

    public Guid? SelectedNodeId { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool IsLoading { get; private set; }
    public string? RootError { get; private set; }
    public IReadOnlyList<KnowledgeTreeNodeViewModel> Breadcrumbs => GetBreadcrumbPath();
    public event Action? Changed;

    internal ReadContext CurrentReadContext { get; private set; } = new();
    internal string? CurrentAudienceId { get; private set; }
    internal Guid? VisualRootNodeId { get; private set; }
    internal int LoadedPageCount => _cache.LoadedPageCount;
    internal int KnownNodeCount => _knownNodes.Count;

    bool IKnowledgeTreeWorkspace.HasContext(ReadContext readContext, string audienceId) =>
        CurrentReadContext == readContext && CurrentAudienceId == audienceId;

    public async Task ExpandNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var node = FindNode(nodeId);
        if (node is null || !node.HasChildren)
        {
            return;
        }

        _expandedNodeIds.Add(nodeId);
        node.IsExpanded = true;
        if (node.IsChildrenPageLoaded)
        {
            Changed?.Invoke();
            return;
        }

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
        {
            return;
        }

        _requestCoordinator.CancelRequest(nodeId);
        _expandedNodeIds.Remove(nodeId);
        node.IsExpanded = false;
        Changed?.Invoke();
    }

    public async Task PageNextAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var node = FindNode(nodeId);
        if (node is null || string.IsNullOrEmpty(node.NextCursor))
        {
            return;
        }

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
        {
            return;
        }

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
        {
            return;
        }

        if (nodeId.HasValue && !_knownNodes.ContainsKey(nodeId.Value))
        {
            var loaded = await TryLoadNodePathAsync(nodeId.Value, cancellationToken).ConfigureAwait(false);
            if (!loaded)
            {
                return;
            }
        }

        SelectedNodeId = nodeId;
        UpdateSelectionFlags(nodeId);

        if (nodeId.HasValue && _knownNodes.TryGetValue(nodeId.Value, out var selectedNode))
        {
            ExpandAncestors(selectedNode);
            if (selectedNode.ParentNodeId.HasValue)
            {
                _cache.RecordAccess(selectedNode.ParentNodeId.Value);
            }
            await RecenterIfAboveVisualRootAsync(selectedNode, cancellationToken).ConfigureAwait(false);
        }

        PruneOffPathNodes();
        Changed?.Invoke();
    }

    private async Task<bool> TryLoadNodePathAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        var callbacks = new TreePathLoaderCallbacks(
            (id, ct) => ExpandNodeAsync(id, ct),
            (id, ct) => PageNextAsync(id, ct),
            id => _knownNodes.ContainsKey(id),
            FindNode,
            (id, ct) => PagePreviousAsync(id, ct),
            path => _activeTargetPath = new HashSet<Guid>(path));

        var loader = new KnowledgeTreePathLoader(_navigationService, callbacks);
        try
        {
            var result = await loader.EnsurePathLoadedAsync(nodeId, CurrentReadContext, CurrentAudienceId, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                if (result.FailedNodeId.HasValue && FindNode(result.FailedNodeId.Value) is { } failedNode)
                {
                    failedNode.Error = result.ErrorMessage;
                }

                StatusMessage = result.ErrorMessage;
                SelectedNodeId = null;
                UpdateSelectionFlags(null);
                PruneOffPathNodes();
                Changed?.Invoke();
                return false;
            }
            return true;
        }
        finally
        {
            _activeTargetPath = null;
        }
    }

    private void ExpandAncestors(KnowledgeTreeNodeViewModel selectedNode)
    {
        var parentId = selectedNode.ParentNodeId;
        while (parentId.HasValue && _knownNodes.TryGetValue(parentId.Value, out var parentNode))
        {
            parentNode.IsExpanded = true;
            _expandedNodeIds.Add(parentNode.NodeId);
            parentId = parentNode.ParentNodeId;
        }
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
        if (!IsUnderVisualRoot(selectedNode))
        {
            VisualRootNodeId = RootNode?.NodeId;
        }

        if (VisualRootNode is null || selectedNode.Depth >= VisualRootNode.Depth)
        {
            return;
        }

        VisualRootNodeId = selectedNode.ParentNodeId ?? RootNode?.NodeId;
        await EnsureVisualRootLoadedAsync(selectedNode, cancellationToken).ConfigureAwait(false);
        StatusMessage = $"Der Baum wurde auf „{VisualRootNode?.Title ?? RootNode?.Title}“ zentriert.";
    }

    private bool IsUnderVisualRoot(KnowledgeTreeNodeViewModel selectedNode)
    {
        for (var n = selectedNode; n is not null; n = n.ParentNodeId.HasValue && _knownNodes.TryGetValue(n.ParentNodeId.Value, out var p) ? p : null)
        {
            if (n.NodeId == VisualRootNodeId)
            {
                return true;
            }
        }
        return false;
    }

    private async Task EnsureVisualRootLoadedAsync(KnowledgeTreeNodeViewModel selectedNode, CancellationToken cancellationToken)
    {
        if (selectedNode.ParentNodeId.HasValue && !_cache.IsLoaded(selectedNode.ParentNodeId.Value))
        {
            await ExpandNodeAsync(selectedNode.ParentNodeId.Value, cancellationToken).ConfigureAwait(false);
        }
        else if (RootNode is not null && !_cache.IsLoaded(RootNode.NodeId))
        {
            await ExpandNodeAsync(RootNode.NodeId, cancellationToken).ConfigureAwait(false);
        }
    }

    internal KnowledgeTreeNodeViewModel? FindNode(Guid nodeId) =>
        _knownNodes.TryGetValue(nodeId, out var node) ? node : null;

    private async Task LoadRootAsync(
        ReadContext context,
        string audienceId,
        int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _navigationService.GetRootAsync(
                context,
                new AudienceId(audienceId),
                cancellationToken).ConfigureAwait(false);

            if (generation != _contextGeneration)
            {
                return;
            }

            if (!result.IsSuccess)
            {
                RootError = result.Error!.Message;
                RootNode = null;
            }
            else if (result.Value?.Node is not null)
            {
                var rootVm = KnowledgeTreeNodeViewModel.FromRoot(result.Value, childCount: 1);
                rootVm.IsExpanded = _expandedNodeIds.Contains(rootVm.NodeId);
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

    private void CompleteActiveRequest(
        Guid nodeId,
        ActiveNodeRequest request,
        KnowledgeTreeNodeViewModel node)
    {
        if (_requestCoordinator.TryCompleteRequest(nodeId, request, _contextGeneration))
        {
            node.IsLoading = false;
            Changed?.Invoke();
        }
    }

    private async Task<bool> LoadChildrenPageAsync(
        KnowledgeTreeNodeViewModel node,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var generation = _contextGeneration;
        var nodeId = node.NodeId;
        var activeRequest = _requestCoordinator.RegisterRequest(nodeId, generation, cancellationToken);

        node.IsLoading = true;
        node.Error = null;
        Changed?.Invoke();

        try
        {
            var query = new ListChildrenQuery(
                new NodeId(nodeId),
                CurrentReadContext,
                new AudienceId(CurrentAudienceId ?? string.Empty),
                Limit: PageLimit,
                Cursor: cursor);

            var result = await _navigationService.ListChildrenAsync(query, activeRequest.Cts.Token).ConfigureAwait(false);

            if (!_requestCoordinator.IsCurrentRequest(nodeId, activeRequest.RequestId, generation, _contextGeneration))
            {
                return false;
            }

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
            if (_requestCoordinator.IsCurrentRequest(nodeId, activeRequest.RequestId, generation, _contextGeneration))
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
            childVm.IsExpanded = _expandedNodeIds.Contains(childVm.NodeId);
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
        node.IsChildrenPageLoaded = true;
        node.NextCursor = page.NextCursor;
        node.HasPreviousPage = _cache.HasPreviousCursor(node.NodeId);

        _cache.RecordPageLoaded(node.NodeId, cursor);
        PruneOffPathNodes();
    }

    private void PruneOffPathNodes()
    {
        var context = new CircuitPruneContext(RootNode, _cache, _knownNodes, _activeTargetPath);
        var (selectedId, visualRootId) = KnowledgeTreeCircuitPruner.Prune(context, SelectedNodeId, VisualRootNodeId);
        SelectedNodeId = selectedId;
        VisualRootNodeId = visualRootId;
    }

    private void ApplyEvictionIfNecessary()
    {
        var eviction = _cache.EvictIfNecessary(FindNode, SelectedNodeId, _activeTargetPath);
        if (eviction is null)
        {
            return;
        }

        _expandedNodeIds.Remove(eviction.EvictedNode.NodeId);
        StatusMessage = eviction.StatusMessage;
        if (eviction.NewVisualRoot is not null)
        {
            VisualRootNodeId = eviction.NewVisualRoot.NodeId;
        }
    }

    private IReadOnlyList<KnowledgeTreeNodeViewModel> GetBreadcrumbPath()
    {
        if (SelectedNodeId is not { } selectedId || !_knownNodes.TryGetValue(selectedId, out var current))
        {
            return RootNode is not null ? new[] { RootNode } : Array.Empty<KnowledgeTreeNodeViewModel>();
        }

        var path = new List<KnowledgeTreeNodeViewModel>();
        for (var node = current; node is not null; node = node.ParentNodeId is { } parentId && _knownNodes.TryGetValue(parentId, out var parent) ? parent : null)
        {
            path.Insert(0, node);
        }

        return path;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _requestCoordinator.Dispose();
    }
}
