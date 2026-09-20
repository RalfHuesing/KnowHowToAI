using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>Orchestriert Content-Mutationen auf einem offenen Working Snapshot.</summary>
public sealed class ContentMutationApplicationService(
    IContentMutationRepository repository,
    ContentMutationService mutationService,
    ValidationPolicy validationPolicy)
{
    private readonly IContentMutationRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ContentMutationService _mutationService = mutationService ?? throw new ArgumentNullException(nameof(mutationService));
    private readonly ValidationPolicy _validationPolicy = validationPolicy ?? throw new ArgumentNullException(nameof(validationPolicy));

    public async Task<Result<ContentMutationUseCaseResult>> ReplaceContentAsync(
        TransactionId transactionId,
        ReplaceContentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executionResult = await _repository.ExecuteAsync(
            transactionId,
            state => CreateReplaceContentDecision(state, request),
            request.ExpectedChangeVersion,
            cancellationToken).ConfigureAwait(false);
        return ToUseCaseResult(executionResult);
    }

    public async Task<Result<ContentMutationUseCaseResult>> ReplaceTextAsync(
        TransactionId transactionId,
        ReplaceTextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executionResult = await _repository.ExecuteAsync(
            transactionId,
            state => CreateReplaceTextDecision(state, request),
            request.ExpectedChangeVersion,
            cancellationToken).ConfigureAwait(false);
        return ToUseCaseResult(executionResult);
    }

    public async Task<Result<ContentMutationUseCaseResult>> DeleteContentAsync(
        TransactionId transactionId,
        NodeId nodeId,
        RoleId roleId,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        var executionResult = await _repository.ExecuteAsync(
            transactionId,
            state => CreateDeletionDecision(state, nodeId, roleId),
            expectedChangeVersion,
            cancellationToken).ConfigureAwait(false);
        return ToUseCaseResult(executionResult);
    }

    private Result<WorkingContentMutationDecision<ContentMutationOutcome>> CreateReplaceContentDecision(
        WorkingContentMutationState state,
        ReplaceContentRequest request)
    {
        var contextResult = ValidateTarget(state, request.NodeId, request.RoleId);
        if (!contextResult.IsSuccess)
            return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Failure(contextResult.Error!);

        var node = contextResult.Value!;
        var dependencies = request.Sources.Select(source => new ContentDependency(
            state.SnapshotId,
            request.NodeId,
            request.RoleId,
            source.NodeId,
            source.RoleId,
            source.ContentRevisionId)).ToArray();
        var mutationResult = _mutationService.ReplaceContent(
            state.Contents,
            state.Dependencies,
            new ReplaceContentCommand(
                state.SnapshotId,
                request.NodeId,
                request.RoleId,
                request.ContentMode,
                request.ContentMd,
                dependencies,
                node.Title,
                _validationPolicy.PossibleEmbeddedHeadingWarning));
        if (!mutationResult.IsSuccess)
            return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Failure(mutationResult.Error!, mutationResult.Warnings);

        var mutation = mutationResult.Value!;
        var updatedDependencies = state.Dependencies.Where(dependency =>
            dependency.SnapshotId != state.SnapshotId
            || dependency.TargetNodeId != request.NodeId
            || dependency.TargetRoleId != request.RoleId).Concat(dependencies).ToArray();
        return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Success(
            new WorkingContentMutationDecision<ContentMutationOutcome>(
                new ContentMutationOutcome(mutation.ChangedContent, mutationResult.Warnings),
                state with { Contents = mutation.Contents, Dependencies = updatedDependencies }));
    }

    private Result<WorkingContentMutationDecision<ContentMutationOutcome>> CreateReplaceTextDecision(
        WorkingContentMutationState state,
        ReplaceTextRequest request)
    {
        var contextResult = ValidateTarget(state, request.NodeId, request.RoleId);
        if (!contextResult.IsSuccess)
            return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Failure(contextResult.Error!);

        var mutationResult = _mutationService.ReplaceText(
            state.Contents,
            new ReplaceTextCommand(
                request.NodeId,
                request.RoleId,
                request.OldText,
                request.NewText,
                contextResult.Value!.Title,
                _validationPolicy.PossibleEmbeddedHeadingWarning));
        if (!mutationResult.IsSuccess)
            return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Failure(mutationResult.Error!, mutationResult.Warnings);

        var mutation = mutationResult.Value!;
        return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Success(
            new WorkingContentMutationDecision<ContentMutationOutcome>(
                new ContentMutationOutcome(mutation.ChangedContent, mutationResult.Warnings),
                state with { Contents = mutation.Contents }));
    }

    private Result<WorkingContentMutationDecision<ContentMutationOutcome>> CreateDeletionDecision(
        WorkingContentMutationState state,
        NodeId nodeId,
        RoleId roleId)
    {
        var contextResult = ValidateTarget(state, nodeId, roleId);
        if (!contextResult.IsSuccess)
            return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Failure(contextResult.Error!);

        var mutationResult = _mutationService.DeleteContent(
            state.Contents,
            state.Dependencies,
            new DeleteContentCommand(nodeId, roleId));
        if (!mutationResult.IsSuccess)
            return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Failure(mutationResult.Error!);

        var mutation = mutationResult.Value!;
        return Result<WorkingContentMutationDecision<ContentMutationOutcome>>.Success(
            new WorkingContentMutationDecision<ContentMutationOutcome>(
                new ContentMutationOutcome(mutation.ChangedContent, []),
                state with { Contents = mutation.Contents, Dependencies = mutation.Dependencies }));
    }

    private Result<Node> ValidateTarget(WorkingContentMutationState state, NodeId nodeId, RoleId roleId)
    {
        var node = state.Nodes.SingleOrDefault(candidate => !candidate.IsDeleted && candidate.NodeId == nodeId);
        if (node is null)
        {
            return Result<Node>.Failure(new DomainError(
                HierarchyErrorCodes.NodeNotFound,
                "Die angefragte aktive Node existiert nicht.",
                new Dictionary<string, string> { [HierarchyErrorCodes.NodeIdDetail] = nodeId.ToString() }));
        }

        if (!state.Roles.Any(role => !role.IsDeleted && role.RoleId == roleId))
        {
            return Result<Node>.Failure(new DomainError(
                RoleResolutionErrorCodes.RequestedRoleNotFound,
                "Die angefragte aktive Rolle existiert nicht.",
                new Dictionary<string, string> { [RoleResolutionErrorCodes.RequestedRoleIdDetail] = roleId.ToString() }));
        }

        return Result<Node>.Success(node);
    }

    private Result<ContentMutationUseCaseResult> ToUseCaseResult(
        Result<WorkingContentMutationExecution<ContentMutationOutcome>> executionResult)
    {
        if (!executionResult.IsSuccess)
            return Result<ContentMutationUseCaseResult>.Failure(executionResult.Error!, executionResult.Warnings);

        var execution = executionResult.Value!;
        var outcome = execution.Value;
        var freshness = outcome.Content.IsDeleted
            ? Freshness.Unknown
            : FreshnessEvaluator.Evaluate(outcome.Content, execution.CurrentState.Contents, execution.CurrentState.Dependencies);
        var warnings = outcome.Warnings.Concat(outcome.Content.IsDeleted
            ? []
            : QualityWarningEvaluator.EvaluateContentSize(
                outcome.Content.ContentMd,
                _validationPolicy.ToQualityWarningThresholds()));
        return Result<ContentMutationUseCaseResult>.Success(
            new ContentMutationUseCaseResult(outcome.Content, execution.SnapshotId, execution.ChangeVersion, freshness),
            warnings);
    }

    private sealed record ContentMutationOutcome(NodeContent Content, IReadOnlyList<DomainWarning> Warnings);
}
