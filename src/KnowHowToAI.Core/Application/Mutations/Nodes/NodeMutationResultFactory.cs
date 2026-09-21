using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Bildet einheitliche Ergebnisse und Qualitätswarnungen aus ausgeführten Node-Mutationen.</summary>
internal sealed class NodeMutationResultFactory(ValidationPolicy validationPolicy)
{
    private readonly ValidationPolicy _validationPolicy = validationPolicy ?? throw new ArgumentNullException(nameof(validationPolicy));

    public Result<NodeMutationResult> Create(
        HierarchyMutationResult mutation,
        WorkingNodeMutationExecution<HierarchyMutationResult> execution) =>
        Create(
            mutation.ChangedNode,
            execution.PreviousState.Nodes,
            execution.CurrentState.Nodes,
            execution.SnapshotId,
            execution.ChangeVersion);

    public Result<NodeMutationResult> Create(
        NodeDeletionResult mutation,
        NodeId deletedNodeId,
        WorkingNodeMutationExecution<NodeDeletionResult> execution)
    {
        var deletedNode = mutation.Nodes.Single(node => node.NodeId == deletedNodeId);
        return Create(
            deletedNode,
            execution.PreviousState.Nodes,
            execution.CurrentState.Nodes,
            execution.SnapshotId,
            execution.ChangeVersion);
    }

    private Result<NodeMutationResult> Create(
        Node changedNode,
        IReadOnlyList<Node> previousNodes,
        IReadOnlyList<Node> currentNodes,
        SnapshotId snapshotId,
        long changeVersion)
    {
        var affectedNodeIds = DetermineAffectedNodeIds(previousNodes, currentNodes, changedNode.NodeId);
        var warnings = EvaluateWarnings(currentNodes, affectedNodeIds);
        return Result<NodeMutationResult>.Success(
            new NodeMutationResult(changedNode, snapshotId, changeVersion, affectedNodeIds, AppliesToAllAudiences: true),
            warnings);
    }

    private IReadOnlyList<DomainWarning> EvaluateWarnings(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<NodeId> affectedNodeIds)
    {
        var thresholds = _validationPolicy.ToQualityWarningThresholds();
        var activeNodes = nodes.Where(node => !node.IsDeleted).ToArray();
        var warnings = new List<DomainWarning>();

        foreach (var nodeId in affectedNodeIds)
        {
            var node = activeNodes.SingleOrDefault(candidate => candidate.NodeId == nodeId);
            if (node is not null)
                warnings.AddRange(QualityWarningEvaluator.EvaluateHierarchyDepth(FindDepth(node, activeNodes), thresholds));
        }

        var affectedParentIds = activeNodes
            .Where(node => affectedNodeIds.Contains(node.NodeId) && node.ParentNodeId is not null)
            .Select(node => node.ParentNodeId!.Value)
            .Distinct()
            .ToArray();
        foreach (var parentNodeId in affectedParentIds)
        {
            warnings.AddRange(QualityWarningEvaluator.EvaluateChildCount(
                activeNodes.Where(node => node.ParentNodeId == parentNodeId), thresholds));
        }

        return warnings
            .GroupBy(warning => (warning.Code, string.Join("|", warning.Details.OrderBy(detail => detail.Key).Select(detail => detail.Key + "=" + detail.Value))))
            .Select(group => group.First())
            .ToArray();
    }

    private static int FindDepth(Node node, IReadOnlyList<Node> nodes)
    {
        var nodesById = nodes.ToDictionary(candidate => candidate.NodeId);
        var depth = 1;
        var current = node;
        while (current.ParentNodeId is { } parentNodeId)
        {
            depth++;
            current = nodesById[parentNodeId];
        }

        return depth;
    }

    private static IReadOnlyList<NodeId> DetermineAffectedNodeIds(
        IReadOnlyList<Node> previousNodes,
        IReadOnlyList<Node> currentNodes,
        NodeId changedNodeId)
    {
        var previousById = previousNodes.ToDictionary(node => node.NodeId);
        var currentById = currentNodes.ToDictionary(node => node.NodeId);
        var changedNodeIds = previousById.Keys.Union(currentById.Keys)
            .Where(nodeId => !previousById.TryGetValue(nodeId, out var previous)
                || !currentById.TryGetValue(nodeId, out var current)
                || previous != current)
            .Append(changedNodeId)
            .Distinct()
            .ToHashSet();
        var affectedNodeIds = new HashSet<NodeId>(changedNodeIds);

        foreach (var nodeId in changedNodeIds)
        {
            if (!previousById.TryGetValue(nodeId, out var previous)
                || !currentById.TryGetValue(nodeId, out var current)
                || previous.ParentNodeId != current.ParentNodeId)
            {
                AddActiveDescendants(nodeId, currentNodes, affectedNodeIds);
            }
        }

        return affectedNodeIds.OrderBy(nodeId => nodeId.Value).ToArray();
    }

    private static void AddActiveDescendants(NodeId rootNodeId, IReadOnlyList<Node> nodes, ISet<NodeId> nodeIds)
    {
        var childNodeIdsByParent = nodes.Where(node => !node.IsDeleted && node.ParentNodeId is not null)
            .GroupBy(node => node.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(node => node.NodeId));
        var pendingNodeIds = new Queue<NodeId>();
        pendingNodeIds.Enqueue(rootNodeId);
        while (pendingNodeIds.TryDequeue(out var parentNodeId)
               && childNodeIdsByParent.TryGetValue(parentNodeId, out var childNodeIds))
        {
            foreach (var childNodeId in childNodeIds)
            {
                if (nodeIds.Add(childNodeId))
                    pendingNodeIds.Enqueue(childNodeId);
            }
        }
    }
}
