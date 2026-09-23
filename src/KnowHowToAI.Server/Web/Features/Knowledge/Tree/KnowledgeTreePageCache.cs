namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>
/// Ergebnis einer LRU-Cache-Eviction.
/// </summary>
internal sealed record EvictionResult(
    KnowledgeTreeNodeViewModel EvictedNode,
    KnowledgeTreeNodeViewModel? NewVisualRoot,
    string StatusMessage);

/// <summary>
/// Verwaltet die maximal 10 geladenen Seiten, Zugriffszeiten (LRU) und Cursor-Historien
/// des Wissensbaums im Blazor-Circuit.
/// </summary>
internal sealed class KnowledgeTreePageCache
{
    public const int MaxLoadedPages = 10;

    private readonly Dictionary<Guid, long> _loadedPages = new();
    private readonly Dictionary<Guid, Stack<string?>> _cursorHistories = new();
    private readonly Dictionary<Guid, string?> _currentCursors = new();
    private long _tickCounter;

    public int LoadedPageCount => _loadedPages.Count;

    public bool IsLoaded(Guid parentId) => _loadedPages.ContainsKey(parentId);

    public void RecordPageLoaded(Guid parentId, string? cursor)
    {
        _loadedPages[parentId] = ++_tickCounter;
        _currentCursors[parentId] = cursor;
        if (!_cursorHistories.ContainsKey(parentId))
        {
            _cursorHistories[parentId] = new Stack<string?>();
        }
    }

    public void RecordAccess(Guid parentId)
    {
        if (_loadedPages.ContainsKey(parentId))
        {
            _loadedPages[parentId] = ++_tickCounter;
        }
    }

    public void PushCurrentCursor(Guid parentId)
    {
        if (!_cursorHistories.TryGetValue(parentId, out var history))
        {
            history = new Stack<string?>();
            _cursorHistories[parentId] = history;
        }

        _currentCursors.TryGetValue(parentId, out var current);
        history.Push(current);
    }

    public bool TryPopPreviousCursor(Guid parentId, out string? cursor)
    {
        if (_cursorHistories.TryGetValue(parentId, out var history) && history.Count > 0)
        {
            cursor = history.Pop();
            return true;
        }

        cursor = null;
        return false;
    }

    public void RestoreCursor(Guid parentId, string? cursor)
    {
        if (!_cursorHistories.TryGetValue(parentId, out var history))
        {
            history = new Stack<string?>();
            _cursorHistories[parentId] = history;
        }

        history.Push(cursor);
    }

    public bool HasPreviousCursor(Guid parentId) =>
        _cursorHistories.TryGetValue(parentId, out var history) && history.Count > 0;

    public void Clear()
    {
        _loadedPages.Clear();
        _cursorHistories.Clear();
        _currentCursors.Clear();
    }

    public EvictionResult? EvictIfNecessary(
        Func<Guid, KnowledgeTreeNodeViewModel?> nodeResolver,
        Guid? selectedNodeId,
        IEnumerable<Guid>? protectedPath = null)
    {
        if (_loadedPages.Count < MaxLoadedPages)
            return null;

        var selectionPath = BuildSelectionPath(nodeResolver, selectedNodeId);
        if (protectedPath is not null)
        {
            selectionPath.UnionWith(protectedPath);
        }

        // 1. Suche unselektierten Teilbaum
        var unselectedPages = _loadedPages
            .Where(kvp => !selectionPath.Contains(kvp.Key))
            .OrderBy(kvp => kvp.Value)
            .ToList();

        if (unselectedPages.Count > 0)
        {
            return EvictUnselectedPage(unselectedPages[0].Key, nodeResolver);
        }

        // 2. Alle Seiten liegen auf dem Auswahlpfad -> Rootnächste Seite evicten
        return EvictRootNearestPage(selectionPath, nodeResolver);
    }

    private static HashSet<Guid> BuildSelectionPath(
        Func<Guid, KnowledgeTreeNodeViewModel?> nodeResolver,
        Guid? selectedNodeId)
    {
        var path = new HashSet<Guid>();
        if (!selectedNodeId.HasValue)
            return path;

        path.Add(selectedNodeId.Value);
        var current = nodeResolver(selectedNodeId.Value);
        while (current?.ParentNodeId is not null)
        {
            path.Add(current.ParentNodeId.Value);
            current = nodeResolver(current.ParentNodeId.Value);
        }

        return path;
    }

    private EvictionResult? EvictUnselectedPage(
        Guid parentId,
        Func<Guid, KnowledgeTreeNodeViewModel?> nodeResolver)
    {
        var node = nodeResolver(parentId);
        RemovePage(parentId);

        if (node is null)
            return null;

        node.Children = Array.Empty<KnowledgeTreeNodeViewModel>();
        node.IsExpanded = false;
        node.IsChildrenPageLoaded = false;
        node.NextCursor = null;
        node.HasPreviousPage = false;

        return new EvictionResult(
            node,
            null,
            $"Die Unterknoten von „{node.Title}“ wurden aus dem Zwischenspeicher entfernt, um die Speichergrenze einzuhalten.");
    }

    private EvictionResult? EvictRootNearestPage(
        HashSet<Guid> selectionPath,
        Func<Guid, KnowledgeTreeNodeViewModel?> nodeResolver)
    {
        var loadedSelectionNodes = _loadedPages.Keys
            .Select(nodeResolver)
            .Where(n => n is not null)
            .Cast<KnowledgeTreeNodeViewModel>()
            .OrderBy(n => n.Depth)
            .ToList();

        if (loadedSelectionNodes.Count == 0)
            return null;

        var rootNearest = loadedSelectionNodes[0];
        var childOnPath = rootNearest.Children.FirstOrDefault(c => selectionPath.Contains(c.NodeId));

        RemovePage(rootNearest.NodeId);

        rootNearest.Children = Array.Empty<KnowledgeTreeNodeViewModel>();
        rootNearest.IsExpanded = false;
        rootNearest.IsChildrenPageLoaded = false;
        rootNearest.NextCursor = null;
        rootNearest.HasPreviousPage = false;

        var message = childOnPath is not null
            ? $"Der Baum wurde auf „{childOnPath.Title}“ zentriert, um die Speichergrenze einzuhalten."
            : "Der Baum wurde neu zentriert, um die Speichergrenze einzuhalten.";

        return new EvictionResult(rootNearest, childOnPath, message);
    }

    public IEnumerable<Guid> LoadedPageParentIds => _loadedPages.Keys;

    public void RemovePageAndCursors(Guid parentId)
    {
        _loadedPages.Remove(parentId);
        _cursorHistories.Remove(parentId);
        _currentCursors.Remove(parentId);
    }

    private void RemovePage(Guid parentId) => RemovePageAndCursors(parentId);
}
