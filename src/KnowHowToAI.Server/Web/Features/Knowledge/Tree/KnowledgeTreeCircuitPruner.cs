namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>
/// Kontext für das Bereinigen des Wissensbaum-Circuits.
/// </summary>
internal readonly record struct CircuitPruneContext(
    KnowledgeTreeNodeViewModel? RootNode,
    KnowledgeTreePageCache Cache,
    Dictionary<Guid, KnowledgeTreeNodeViewModel> KnownNodes,
    IEnumerable<Guid>? ProtectedPath = null);

/// <summary>
/// Bereinigt nicht mehr erreichbare Off-Path-Knoten und deren Cursor-Zustand aus dem Wissensbaum-Circuit.
/// </summary>
internal static class KnowledgeTreeCircuitPruner
{
    public static (Guid? SelectedNodeId, Guid? VisualRootNodeId) Prune(
        CircuitPruneContext context,
        Guid? selectedNodeId,
        Guid? visualRootNodeId)
    {
        var allowed = CollectAllowedNodeIds(context, selectedNodeId);
        var toRemove = context.KnownNodes.Keys.Where(id => !allowed.Contains(id)).ToArray();

        foreach (var nodeId in toRemove)
        {
            if (context.KnownNodes.Remove(nodeId, out var removedNode))
            {
                removedNode.Children = Array.Empty<KnowledgeTreeNodeViewModel>();
                removedNode.IsChildrenPageLoaded = false;
                removedNode.IsSelected = false;
            }

            if (selectedNodeId == nodeId)
            {
                selectedNodeId = null;
            }

            context.Cache.RemovePageAndCursors(nodeId);
        }

        if (visualRootNodeId.HasValue && !context.KnownNodes.ContainsKey(visualRootNodeId.Value))
        {
            visualRootNodeId = context.RootNode?.NodeId;
        }

        return (selectedNodeId, visualRootNodeId);
    }

    private static HashSet<Guid> CollectAllowedNodeIds(
        CircuitPruneContext context,
        Guid? selectedNodeId)
    {
        var allowed = new HashSet<Guid>();
        if (context.RootNode is not null)
        {
            allowed.Add(context.RootNode.NodeId);
        }

        if (context.ProtectedPath is not null)
        {
            allowed.UnionWith(context.ProtectedPath);
        }

        foreach (var parentId in context.Cache.LoadedPageParentIds)
        {
            allowed.Add(parentId);
            if (!context.KnownNodes.TryGetValue(parentId, out var parentNode))
            {
                continue;
            }

            foreach (var child in parentNode.Children)
            {
                allowed.Add(child.NodeId);
            }
        }

        AddSelectedAncestors(context.KnownNodes, selectedNodeId, allowed);
        return allowed;
    }

    private static void AddSelectedAncestors(
        Dictionary<Guid, KnowledgeTreeNodeViewModel> knownNodes,
        Guid? selectedNodeId,
        HashSet<Guid> allowed)
    {
        if (selectedNodeId is not { } selectedId || !knownNodes.TryGetValue(selectedId, out var current))
        {
            return;
        }

        var node = current;
        while (node is not null)
        {
            allowed.Add(node.NodeId);
            node = node.ParentNodeId is { } parentId && knownNodes.TryGetValue(parentId, out var parent)
                ? parent
                : null;
        }
    }
}
