using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Orchestriert globale Node-Mutationen auf einem offenen Working Snapshot.</summary>
public sealed class NodeMutationApplicationService
{
    private readonly INodeMutationRepository _repository;
    private readonly NodeMutationService _mutationService;
    private readonly ValidationPolicy _validationPolicy;

    public NodeMutationApplicationService(
        INodeMutationRepository repository,
        NodeMutationService mutationService,
        ValidationPolicy validationPolicy)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mutationService = mutationService ?? throw new ArgumentNullException(nameof(mutationService));
        _validationPolicy = validationPolicy ?? throw new ArgumentNullException(nameof(validationPolicy));
    }

    public Task<Result<NodeMutationResult>> CreateAsync(
        TransactionId transactionId,
        CreateNodeRequest request,
        long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteHierarchyMutationAsync(
            transactionId,
            state => _mutationService.Create(
                state.Nodes,
                new CreateNodeCommand(
                    state.SnapshotId,
                    request.ParentNodeId,
                    request.Title,
                    request.Description,
                    request.SortOrder),
                state.KnownNodeIds),
            expectedChangeVersion,
            cancellationToken);
    }

    public Task<Result<NodeMutationResult>> UpdateAsync(
        TransactionId transactionId,
        UpdateNodeRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteHierarchyMutationAsync(
            transactionId,
            state => _mutationService.Update(
                state.Nodes,
                new UpdateNodeCommand { NodeId = request.NodeId, Title = request.Title, Description = request.Description }),
            request.ExpectedChangeVersion,
            cancellationToken);

    public Task<Result<NodeMutationResult>> MoveAsync(
        TransactionId transactionId,
        MoveNodeRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteHierarchyMutationAsync(
            transactionId,
            state => _mutationService.Move(
                state.Nodes,
                new MoveNodeCommand(request.NodeId, request.ParentNodeId, request.SortOrder)),
            request.ExpectedChangeVersion,
            cancellationToken: cancellationToken);

    public Task<Result<NodeMutationResult>> ReorderAsync(
        TransactionId transactionId,
        NodeId nodeId,
        int sortOrder,
        long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default) =>
        ExecuteHierarchyMutationAsync(
            transactionId,
            state => _mutationService.Reorder(
                state.Nodes,
                new ReorderNodeCommand(nodeId, sortOrder)),
            expectedChangeVersion,
            cancellationToken: cancellationToken);

    public async Task<Result<NodeMutationResult>> DeleteAsync(
        TransactionId transactionId,
        NodeId nodeId,
        bool deleteSubtree,
        long? expectedChangeVersion = null,
        CancellationToken cancellationToken = default)
    {
        var executionResult = await _repository.ExecuteAsync(
            transactionId,
            state => CreateDeletionDecision(state, nodeId, deleteSubtree),
            cancellationToken: cancellationToken,
            expectedChangeVersion: expectedChangeVersion).ConfigureAwait(false);
        if (!executionResult.IsSuccess)
            return Result<NodeMutationResult>.Failure(executionResult.Error!);

        var execution = executionResult.Value!;
        return CreateResult(execution.Value, nodeId, execution);
    }

    private async Task<Result<NodeMutationResult>> ExecuteHierarchyMutationAsync(
        TransactionId transactionId,
        Func<WorkingNodeMutationState, Result<HierarchyMutationResult>> mutate,
        long? expectedChangeVersion,
        CancellationToken cancellationToken)
    {
        var executionResult = await _repository.ExecuteAsync(
            transactionId,
            state => CreateHierarchyDecision(state, mutate),
            cancellationToken: cancellationToken,
            expectedChangeVersion: expectedChangeVersion).ConfigureAwait(false);
        if (!executionResult.IsSuccess)
            return Result<NodeMutationResult>.Failure(executionResult.Error!);

        var execution = executionResult.Value!;
        return CreateResult(execution.Value, execution);
    }

    private static Result<WorkingNodeMutationDecision<HierarchyMutationResult>> CreateHierarchyDecision(
        WorkingNodeMutationState state,
        Func<WorkingNodeMutationState, Result<HierarchyMutationResult>> mutate)
    {
        var mutationResult = mutate(state);
        if (!mutationResult.IsSuccess)
            return Result<WorkingNodeMutationDecision<HierarchyMutationResult>>.Failure(mutationResult.Error!);

        return Result<WorkingNodeMutationDecision<HierarchyMutationResult>>.Success(
            new WorkingNodeMutationDecision<HierarchyMutationResult>(
                mutationResult.Value!,
                state with { Nodes = mutationResult.Value!.Nodes }));
    }

    private Result<WorkingNodeMutationDecision<NodeDeletionResult>> CreateDeletionDecision(
        WorkingNodeMutationState state,
        NodeId nodeId,
        bool deleteSubtree)
    {
        var mutationResult = _mutationService.Delete(
            state.Nodes,
            state.Contents,
            state.Dependencies,
            new DeleteNodeCommand(nodeId, deleteSubtree));
        if (!mutationResult.IsSuccess)
            return Result<WorkingNodeMutationDecision<NodeDeletionResult>>.Failure(mutationResult.Error!);

        var mutation = mutationResult.Value!;
        return Result<WorkingNodeMutationDecision<NodeDeletionResult>>.Success(
            new WorkingNodeMutationDecision<NodeDeletionResult>(
                mutation,
                state with
                {
                    Nodes = mutation.Nodes,
                    Contents = mutation.Contents,
                    Dependencies = mutation.Dependencies
                }));
    }

    private Result<NodeMutationResult> CreateResult(
        HierarchyMutationResult mutation,
        WorkingNodeMutationExecution<HierarchyMutationResult> execution) =>
        CreateResult(
            mutation.ChangedNode,
            execution.PreviousState.Nodes,
            execution.CurrentState.Nodes,
            execution.SnapshotId,
            execution.ChangeVersion);

    private Result<NodeMutationResult> CreateResult(
        NodeDeletionResult mutation,
        NodeId deletedNodeId,
        WorkingNodeMutationExecution<NodeDeletionResult> execution)
    {
        var deletedNode = mutation.Nodes.Single(node => node.NodeId == deletedNodeId);
        return CreateResult(
            deletedNode,
            execution.PreviousState.Nodes,
            execution.CurrentState.Nodes,
            execution.SnapshotId,
            execution.ChangeVersion);
    }

    private Result<NodeMutationResult> CreateResult(
        Node changedNode,
        IReadOnlyList<Node> previousNodes,
        IReadOnlyList<Node> currentNodes,
        SnapshotId snapshotId,
        long changeVersion)
    {
        var affectedNodeIds = DetermineAffectedNodeIds(previousNodes, currentNodes, changedNode.NodeId);
        var warnings = EvaluateWarnings(currentNodes, affectedNodeIds);
        return Result<NodeMutationResult>.Success(
            new NodeMutationResult(changedNode, snapshotId, changeVersion, affectedNodeIds, AppliesToAllRoles: true),
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
