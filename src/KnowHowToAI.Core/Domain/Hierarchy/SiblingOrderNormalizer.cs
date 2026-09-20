using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Hierarchy;

/// <summary>
/// Erzeugt eine neue Node-Liste mit kanonischen, lückenlosen Geschwisterreihenfolgen.
/// </summary>
public static class SiblingOrderNormalizer
{
    public static IReadOnlyList<Node> Normalize(IEnumerable<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        return Normalize(nodes, static _ => true);
    }

    public static IReadOnlyList<Node> Normalize(
        IEnumerable<Node> nodes,
        SnapshotId snapshotId,
        IEnumerable<NodeId?> parentNodeIds)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(parentNodeIds);

        var affectedGroups = parentNodeIds
            .Select(parentNodeId => (SnapshotId: snapshotId, ParentNodeId: parentNodeId))
            .ToHashSet();

        return Normalize(nodes, affectedGroups.Contains);
    }

    /// <summary>
    /// Verschiebt eine aktive Node an den angegebenen Einfügeindex der Zielgruppe.
    /// Der Index wird nach Entfernung der Quelle aus ihrer bisherigen Gruppe
    /// ausgewertet; dadurch bleibt die sichtbare Before-/After-Position auch bei
    /// einer Verschiebung innerhalb derselben Geschwistergruppe exakt erhalten.
    /// </summary>
    internal static IReadOnlyList<Node> Move(
        IEnumerable<Node> nodes,
        MoveInsertion insertion)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var movedNodes = nodes.ToArray();
        var sourceIndex = Array.FindIndex(
            movedNodes,
            node => !node.IsDeleted && node.SnapshotId == insertion.SnapshotId && node.NodeId == insertion.NodeId);
        if (sourceIndex < 0)
            return Array.AsReadOnly(movedNodes);

        var source = movedNodes[sourceIndex];
        var targetGroup = (insertion.SnapshotId, insertion.TargetParentNodeId);
        var targetSiblingIndexes = Enumerable.Range(0, movedNodes.Length)
            .Where(index => index != sourceIndex
                && !movedNodes[index].IsDeleted
                && (movedNodes[index].SnapshotId, movedNodes[index].ParentNodeId) == targetGroup)
            .ToList();
        targetSiblingIndexes.Sort((left, right) => Compare(movedNodes[left], movedNodes[right]));

        var insertionIndex = Math.Clamp(insertion.TargetIndex, 0, targetSiblingIndexes.Count);
        var orderedTargetIndexes = targetSiblingIndexes.ToArray().ToList();
        orderedTargetIndexes.Insert(insertionIndex, sourceIndex);

        var movedSource = source with { ParentNodeId = insertion.TargetParentNodeId };
        movedNodes[sourceIndex] = movedSource;
        for (var sortOrder = 0; sortOrder < orderedTargetIndexes.Count; sortOrder++)
        {
            var index = orderedTargetIndexes[sortOrder];
            movedNodes[index] = movedNodes[index] with { SortOrder = sortOrder };
        }

        var sourceGroup = (insertion.SnapshotId, source.ParentNodeId);
        if (sourceGroup != targetGroup)
        {
            var sourceSiblingIndexes = Enumerable.Range(0, movedNodes.Length)
                .Where(index => index != sourceIndex
                    && !movedNodes[index].IsDeleted
                    && (movedNodes[index].SnapshotId, movedNodes[index].ParentNodeId) == sourceGroup)
                .ToList();
            sourceSiblingIndexes.Sort((left, right) => Compare(movedNodes[left], movedNodes[right]));
            for (var sortOrder = 0; sortOrder < sourceSiblingIndexes.Count; sortOrder++)
            {
                var index = sourceSiblingIndexes[sortOrder];
                movedNodes[index] = movedNodes[index] with { SortOrder = sortOrder };
            }
        }

        return Array.AsReadOnly(movedNodes);
    }

    private static IReadOnlyList<Node> Normalize(
        IEnumerable<Node> nodes,
        Func<(SnapshotId SnapshotId, NodeId? ParentNodeId), bool> shouldNormalize)
    {
        ArgumentNullException.ThrowIfNull(shouldNormalize);

        var normalizedNodes = nodes.ToArray();
        var siblingIndexes = new Dictionary<(SnapshotId SnapshotId, NodeId? ParentNodeId), List<int>>();
        for (var index = 0; index < normalizedNodes.Length; index++)
        {
            var node = normalizedNodes[index];
            if (node.IsDeleted)
                continue;

            var group = (node.SnapshotId, node.ParentNodeId);
            if (!shouldNormalize(group))
                continue;

            if (!siblingIndexes.TryGetValue(group, out var indexes))
            {
                indexes = [];
                siblingIndexes.Add(group, indexes);
            }

            indexes.Add(index);
        }

        foreach (var indexes in siblingIndexes.Values)
            NormalizeGroup(normalizedNodes, indexes);

        return Array.AsReadOnly(normalizedNodes);
    }

    private static int Compare(Node left, Node right)
    {
        var sortOrderComparison = left.SortOrder.CompareTo(right.SortOrder);
        return sortOrderComparison != 0
            ? sortOrderComparison
            : left.NodeId.Value.CompareTo(right.NodeId.Value);
    }

    private static void NormalizeGroup(Node[] nodes, List<int> indexes)
    {
        indexes.Sort((left, right) => Compare(nodes[left], nodes[right]));
        for (var sortOrder = 0; sortOrder < indexes.Count; sortOrder++)
        {
            var index = indexes[sortOrder];
            nodes[index] = nodes[index] with { SortOrder = sortOrder };
        }
    }
}
