using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Domain.Hierarchy;

/// <summary>
/// Prüft die nicht konfigurierbaren Regeln der globalen Node-Hierarchie eines Snapshots.
/// </summary>
public static class HierarchyValidator
{
    public static ValidationReport Validate(IEnumerable<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var allNodes = nodes.ToArray();
        var activeNodes = allNodes.Where(node => !node.IsDeleted).ToArray();
        var errors = new List<DomainError>();
        var nodesById = CreateActiveNodeIndex(activeNodes, errors);

        ValidateSnapshotConsistency(activeNodes, errors);
        ValidateTitles(activeNodes, errors);
        ValidateRootCount(activeNodes, errors);
        ValidateParents(activeNodes, nodesById, errors);
        ValidateCycles(nodesById, errors);

        return new ValidationReport(errors, []);
    }

    private static Dictionary<NodeId, Node> CreateActiveNodeIndex(
        IEnumerable<Node> activeNodes,
        ICollection<DomainError> errors)
    {
        var nodesById = new Dictionary<NodeId, Node>();
        foreach (var node in activeNodes)
        {
            if (!nodesById.TryAdd(node.NodeId, node))
            {
                errors.Add(CreateError(
                    HierarchyErrorCodes.DuplicateNodeId,
                    "Eine NodeId darf innerhalb eines Snapshots nur einmal vorkommen.",
                    node.NodeId));
            }
        }

        return nodesById;
    }

    private static void ValidateSnapshotConsistency(
        IReadOnlyList<Node> activeNodes,
        ICollection<DomainError> errors)
    {
        if (activeNodes.Count == 0)
            return;

        var expectedSnapshotId = activeNodes[0].SnapshotId;
        foreach (var node in activeNodes)
        {
            if (node.SnapshotId == expectedSnapshotId)
                continue;

            errors.Add(new DomainError(
                HierarchyErrorCodes.SnapshotMismatch,
                "Alle aktiven Nodes einer Hierarchie müssen zum selben Snapshot gehören.",
                new Dictionary<string, string>
                {
                    [HierarchyErrorCodes.NodeIdDetail] = node.NodeId.ToString(),
                    [HierarchyErrorCodes.ExpectedSnapshotIdDetail] = expectedSnapshotId.ToString(),
                    [HierarchyErrorCodes.SnapshotIdDetail] = node.SnapshotId.ToString()
                }));
        }
    }

    private static void ValidateTitles(IEnumerable<Node> activeNodes, ICollection<DomainError> errors)
    {
        foreach (var node in activeNodes)
        {
            if (string.IsNullOrWhiteSpace(node.Title))
                errors.Add(CreateError(
                    HierarchyErrorCodes.TitleRequired,
                    "Der Titel einer aktiven Node darf nicht leer oder nur Whitespace sein.",
                    node.NodeId));
        }
    }

    private static void ValidateRootCount(IReadOnlyList<Node> activeNodes, ICollection<DomainError> errors)
    {
        var rootCount = activeNodes.Count(node => node.ParentNodeId is null);
        if (rootCount <= 1)
            return;

        errors.Add(new DomainError(
            HierarchyErrorCodes.RootAlreadyExists,
            "Ein Snapshot darf höchstens eine aktive Root-Node enthalten.",
            new Dictionary<string, string>
            {
                [HierarchyErrorCodes.RootCountDetail] = rootCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));
    }

    private static void ValidateParents(
        IEnumerable<Node> activeNodes,
        IReadOnlyDictionary<NodeId, Node> nodesById,
        ICollection<DomainError> errors)
    {
        foreach (var node in activeNodes)
        {
            if (node.ParentNodeId is not { } parentNodeId)
                continue;

            if (parentNodeId == node.NodeId)
            {
                errors.Add(CreateError(
                    HierarchyErrorCodes.SelfParentNotAllowed,
                    "Eine Node darf nicht ihr eigener Parent sein.",
                    node.NodeId));
                continue;
            }

            if (!nodesById.TryGetValue(parentNodeId, out var parent) || parent.SnapshotId != node.SnapshotId)
            {
                errors.Add(new DomainError(
                    HierarchyErrorCodes.ParentNotFound,
                    "Der Parent muss aktiv im selben Snapshot vorhanden sein.",
                    new Dictionary<string, string>
                    {
                        [HierarchyErrorCodes.NodeIdDetail] = node.NodeId.ToString(),
                        [HierarchyErrorCodes.ParentNodeIdDetail] = parentNodeId.ToString()
                    }));
            }
        }
    }

    private static void ValidateCycles(IReadOnlyDictionary<NodeId, Node> nodesById, ICollection<DomainError> errors)
    {
        var inspectedNodeIds = new HashSet<NodeId>();
        foreach (var node in nodesById.Values)
        {
            if (inspectedNodeIds.Contains(node.NodeId))
                continue;

            var pathNodeIds = new HashSet<NodeId>();
            var currentNode = node;
            while (true)
            {
                if (!pathNodeIds.Add(currentNode.NodeId))
                {
                    errors.Add(CreateError(
                        HierarchyErrorCodes.HierarchyCycle,
                        "Die Node-Hierarchie darf keine Zyklen enthalten.",
                        currentNode.NodeId));
                    break;
                }

                if (currentNode.ParentNodeId is not { } parentNodeId
                    || parentNodeId == currentNode.NodeId
                    || !nodesById.TryGetValue(parentNodeId, out currentNode))
                {
                    break;
                }
            }

            inspectedNodeIds.UnionWith(pathNodeIds);
        }
    }

    private static DomainError CreateError(string code, string message, NodeId nodeId) =>
        new(code, message, new Dictionary<string, string>
        {
            [HierarchyErrorCodes.NodeIdDetail] = nodeId.ToString()
        });
}
