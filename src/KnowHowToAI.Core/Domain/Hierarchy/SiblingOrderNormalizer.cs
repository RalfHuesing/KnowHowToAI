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
