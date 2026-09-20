using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="INodeMutationRepository"/>: führt die Mutation-Funktion auf
/// einem veränderlich gehaltenen Working-Zustand aus, persistiert erfolgreiche
/// Entscheidungen und erhöht die ChangeVersion nur bei echten Zustandsänderungen
/// (Nodes, Contents oder Dependencies). Über <see cref="Rejection"/> lässt sich ein
/// Transaction-Fehler vorab einschleusen.
/// </summary>
public sealed class InMemoryNodeMutationRepository(WorkingNodeMutationState state)
    : INodeMutationRepository
{
    /// <summary>Aktueller Working-Zustand des Stores.</summary>
    public WorkingNodeMutationState State { get; private set; } = state;

    /// <summary>Anzahl der persistierten Zustandsänderungen.</summary>
    public long ChangeVersion { get; private set; }

    /// <summary>Synchronisiert verbundene In-Memory-Write-Ports derselben Transaction.</summary>
    public Action<long>? ChangeVersionChanged { get; set; }

    /// <summary>Optionale Rejection-Injection: Fehler, der ExecuteAsync vorab zurückliefert.</summary>
    public DomainError? Rejection { get; set; }

    public Task<Result<WorkingNodeMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingNodeMutationState, Result<WorkingNodeMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default,
        long? expectedChangeVersion = null)
    {
        if (Rejection is not null)
            return Task.FromResult(Result<WorkingNodeMutationExecution<T>>.Failure(Rejection));

        if (expectedChangeVersion.HasValue && expectedChangeVersion.Value != ChangeVersion)
        {
            return Task.FromResult(Result<WorkingNodeMutationExecution<T>>.Failure(new DomainError(
                TransactionValidationErrorCodes.ChangeVersionConflict,
                "Die Transaction wurde zwischen Laden und Speichern geändert.",
                new Dictionary<string, string>
                {
                    [TransactionValidationErrorCodes.ExpectedChangeVersionDetail] = expectedChangeVersion.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [TransactionValidationErrorCodes.ActualChangeVersionDetail] = ChangeVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
                })));
        }

        var previousState = State;
        var decisionResult = mutate(previousState);
        if (!decisionResult.IsSuccess)
            return Task.FromResult(Result<WorkingNodeMutationExecution<T>>.Failure(
                decisionResult.Error!, decisionResult.Warnings));

        var decision = decisionResult.Value!;
        var stateChanged = HasStateChanged(previousState, decision.State);
        State = decision.State;
        if (stateChanged)
        {
            ChangeVersion++;
            ChangeVersionChanged?.Invoke(ChangeVersion);
        }

        return Task.FromResult(Result<WorkingNodeMutationExecution<T>>.Success(
            new WorkingNodeMutationExecution<T>(
                decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
    }

    private static bool HasStateChanged(WorkingNodeMutationState before, WorkingNodeMutationState after) =>
        !before.Nodes.SequenceEqual(after.Nodes)
        || !before.Contents.SequenceEqual(after.Contents)
        || !before.Dependencies.SequenceEqual(after.Dependencies);
}
