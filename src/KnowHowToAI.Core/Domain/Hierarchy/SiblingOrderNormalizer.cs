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

        var normalizedNodes = nodes.ToArray();
        var siblingIndexes = new Dictionary<NodeId, List<int>>();
        var rootIndexes = new List<int>();
        for (var index = 0; index < normalizedNodes.Length; index++)
        {
            var node = normalizedNodes[index];
            if (node.IsDeleted)
                continue;

            if (node.ParentNodeId is not { } parentNodeId)
            {
                rootIndexes.Add(index);
                continue;
            }

            if (!siblingIndexes.TryGetValue(parentNodeId, out var indexes))
            {
                indexes = [];
                siblingIndexes.Add(parentNodeId, indexes);
            }

            indexes.Add(index);
        }

        NormalizeGroup(normalizedNodes, rootIndexes);
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
