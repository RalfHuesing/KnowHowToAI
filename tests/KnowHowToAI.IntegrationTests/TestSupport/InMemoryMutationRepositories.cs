using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// In-Memory-Node-Mutations-Port für MCP-Handler-Vertragstests ohne SQL-Infrastruktur:
/// wendet die fachliche Entscheidung atomar auf einen zustandsbehafteten Working
/// Snapshot an und kann deterministische Transaction-Rejectionen liefern.
/// </summary>
public sealed class InMemoryNodeMutationRepository(WorkingNodeMutationState state) : INodeMutationRepository
{
    public WorkingNodeMutationState State { get; private set; } = state;

    public long ChangeVersion { get; private set; }

    public DomainError? Rejection { get; set; }

    public Task<Result<WorkingNodeMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingNodeMutationState, Result<WorkingNodeMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default)
    {
        if (Rejection is not null)
            return Task.FromResult(Result<WorkingNodeMutationExecution<T>>.Failure(Rejection));

        var previousState = State;
        var decisionResult = mutate(previousState);
        if (!decisionResult.IsSuccess)
            return Task.FromResult(Result<WorkingNodeMutationExecution<T>>.Failure(decisionResult.Error!));

        var decision = decisionResult.Value!;
        State = decision.State;
        if (!previousState.Nodes.SequenceEqual(State.Nodes)
            || !previousState.Contents.SequenceEqual(State.Contents)
            || !previousState.Dependencies.SequenceEqual(State.Dependencies))
        {
            ChangeVersion++;
        }

        return Task.FromResult(Result<WorkingNodeMutationExecution<T>>.Success(
            new WorkingNodeMutationExecution<T>(decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
    }
}

/// <summary>In-Memory-Content-Mutations-Port für MCP-Handler-Vertragstests ohne SQL-Infrastruktur.</summary>
public sealed class InMemoryContentMutationRepository(WorkingContentMutationState state) : IContentMutationRepository
{
    public WorkingContentMutationState State { get; private set; } = state;

    public long ChangeVersion { get; private set; }

    public DomainError? Rejection { get; set; }

    public Task<Result<WorkingContentMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingContentMutationState, Result<WorkingContentMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default)
    {
        if (Rejection is not null)
            return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Failure(Rejection));

        var previousState = State;
        var decisionResult = mutate(previousState);
        if (!decisionResult.IsSuccess)
            return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Failure(decisionResult.Error!));

        var decision = decisionResult.Value!;
        State = decision.State;
        if (!previousState.Nodes.SequenceEqual(State.Nodes)
            || !previousState.Roles.SequenceEqual(State.Roles)
            || !previousState.Contents.SequenceEqual(State.Contents)
            || !previousState.Dependencies.SequenceEqual(State.Dependencies))
        {
            ChangeVersion++;
        }

        return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Success(
            new WorkingContentMutationExecution<T>(decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
    }
}

/// <summary>In-Memory-Rollen-Mutations-Port für MCP-Handler-Vertragstests ohne SQL-Infrastruktur.</summary>
public sealed class InMemoryRoleMutationRepository(WorkingRoleMutationState state) : IRoleMutationRepository
{
    public WorkingRoleMutationState State { get; private set; } = state;

    public long ChangeVersion { get; private set; }

    public DomainError? Rejection { get; set; }

    public Task<Result<WorkingRoleMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingRoleMutationState, Result<WorkingRoleMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default)
    {
        if (Rejection is not null)
            return Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Failure(Rejection));

        var previousState = State;
        var decisionResult = mutate(previousState);
        if (!decisionResult.IsSuccess)
            return Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Failure(decisionResult.Error!));

        var decision = decisionResult.Value!;
        State = decision.State;
        if (!previousState.Roles.SequenceEqual(State.Roles)
            || !previousState.Resolutions.SequenceEqual(State.Resolutions))
        {
            ChangeVersion++;
        }

        return Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Success(
            new WorkingRoleMutationExecution<T>(decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
    }
}
