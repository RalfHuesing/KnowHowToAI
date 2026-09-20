using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Core.Application.Transactions;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IContentMutationRepository"/>: führt die Mutation-Funktion auf
/// einem veränderlich gehaltenen Working-Zustand aus, persistiert erfolgreiche
/// Entscheidungen und erhöht die ChangeVersion nur bei echten Zustandsänderungen.
/// Über <see cref="Rejection"/> lässt sich ein Transaction-Fehler vorab einschleusen.
/// </summary>
public sealed class InMemoryContentMutationRepository(WorkingContentMutationState state)
    : IContentMutationRepository
{
    /// <summary>Aktueller Working-Zustand des Stores.</summary>
    public WorkingContentMutationState State { get; private set; } = state;

    /// <summary>Anzahl der persistierten Zustandsänderungen.</summary>
    public long ChangeVersion { get; private set; }

    /// <summary>Optionale Rejection-Injection: Fehler, der ExecuteAsync vorab zurückliefert.</summary>
    public DomainError? Rejection { get; set; }

    public Task<Result<WorkingContentMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingContentMutationState, Result<WorkingContentMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default,
        long? expectedChangeVersion = null)
    {
        if (Rejection is not null)
            return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Failure(Rejection));

        if (expectedChangeVersion.HasValue && expectedChangeVersion.Value != ChangeVersion)
        {
            return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Failure(new DomainError(
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
            return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Failure(
                decisionResult.Error!, decisionResult.Warnings));

        var decision = decisionResult.Value!;
        var stateChanged = HasStateChanged(previousState, decision.State);
        State = decision.State;
        if (stateChanged)
            ChangeVersion++;

        return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Success(
            new WorkingContentMutationExecution<T>(
                decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
    }

    private static bool HasStateChanged(WorkingContentMutationState before, WorkingContentMutationState after) =>
        !before.Contents.SequenceEqual(after.Contents)
        || !before.Dependencies.SequenceEqual(after.Dependencies);
}
