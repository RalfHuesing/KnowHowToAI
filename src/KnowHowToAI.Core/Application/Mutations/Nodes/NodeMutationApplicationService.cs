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
    private readonly NodeMutationResultFactory _resultFactory;

    public NodeMutationApplicationService(
        INodeMutationRepository repository,
        NodeMutationService mutationService,
        ValidationPolicy validationPolicy)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mutationService = mutationService ?? throw new ArgumentNullException(nameof(mutationService));
        _resultFactory = new NodeMutationResultFactory(validationPolicy);
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
        return _resultFactory.Create(execution.Value, execution);
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

}
