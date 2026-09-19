using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Lädt den aktuellen Working-Stand für eine kontrollierte globale Node-Löschung.</summary>
public sealed class NodeDeletionPreviewService(IWorkingSnapshotReadRepository repository)
{
    private readonly IWorkingSnapshotReadRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Result<NodeDeletionPreview>> PreviewAsync(
        TransactionId transactionId,
        NodeId nodeId,
        CancellationToken cancellationToken = default)
    {
        var stateResult = await _repository.ReadOpenWorkingAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!stateResult.IsSuccess)
            return Result<NodeDeletionPreview>.Failure(stateResult.Error!);

        var state = stateResult.Value!;
        var node = state.Nodes.SingleOrDefault(candidate => !candidate.IsDeleted && candidate.NodeId == nodeId);
        if (node is null)
        {
            return Result<NodeDeletionPreview>.Failure(new DomainError(
                HierarchyErrorCodes.NodeNotFound,
                "Die angefragte aktive Node existiert nicht.",
                new Dictionary<string, string> { [HierarchyErrorCodes.NodeIdDetail] = nodeId.ToString() }));
        }

        var subtreeNodeIds = FindActiveSubtreeNodeIds(state.Nodes, node.NodeId);
        var removedDependencyCount = state.Dependencies.Count(dependency =>
            dependency.SnapshotId == state.Transaction.WorkingSnapshotId
            && subtreeNodeIds.Contains(dependency.TargetNodeId));
        var retainedSourceDependencyCount = state.Dependencies.Count(dependency =>
            dependency.SnapshotId == state.Transaction.WorkingSnapshotId
            && subtreeNodeIds.Contains(dependency.SourceNodeId)
            && !subtreeNodeIds.Contains(dependency.TargetNodeId));

        return Result<NodeDeletionPreview>.Success(new NodeDeletionPreview(
            node.NodeId,
            node.Title,
            node.ParentNodeId is null,
            state.Nodes.Count(candidate => !candidate.IsDeleted && candidate.ParentNodeId == node.NodeId),
            subtreeNodeIds.Count,
            state.Contents.Count(content => !content.IsDeleted && subtreeNodeIds.Contains(content.NodeId)),
            removedDependencyCount,
            retainedSourceDependencyCount,
            state.ChangeVersion));
    }

    private static ISet<NodeId> FindActiveSubtreeNodeIds(IEnumerable<Node> nodes, NodeId rootNodeId)
    {
        var childrenByParent = nodes.Where(node => !node.IsDeleted && node.ParentNodeId is not null)
            .GroupBy(node => node.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(node => node.NodeId));
        var subtreeNodeIds = new HashSet<NodeId> { rootNodeId };
        var pendingNodeIds = new Queue<NodeId>();
        pendingNodeIds.Enqueue(rootNodeId);

        while (pendingNodeIds.TryDequeue(out var parentNodeId)
               && childrenByParent.TryGetValue(parentNodeId, out var childNodeIds))
        {
            foreach (var childNodeId in childNodeIds)
            {
                if (subtreeNodeIds.Add(childNodeId))
                    pendingNodeIds.Enqueue(childNodeId);
            }
        }

        return subtreeNodeIds;
    }
}
