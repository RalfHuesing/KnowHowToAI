using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Core.Application.Transactions;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IRoleMutationRepository"/>: führt die Mutation-Funktion auf
/// einem veränderlich gehaltenen Working-Zustand aus, persistiert erfolgreiche
/// Entscheidungen und erhöht die ChangeVersion nur bei echten Zustandsänderungen
/// (Rollen oder Auflösungsreihenfolgen). Über <see cref="Rejection"/> lässt sich ein
/// Transaction-Fehler vorab einschleusen.
/// </summary>
public sealed class InMemoryRoleMutationRepository(WorkingRoleMutationState state)
    : IRoleMutationRepository
{
    /// <summary>Aktueller Working-Zustand des Stores.</summary>
    public WorkingRoleMutationState State { get; private set; } = state;

    /// <summary>Anzahl der persistierten Zustandsänderungen.</summary>
    public long ChangeVersion { get; private set; }

    /// <summary>Optionale Rejection-Injection: Fehler, der ExecuteAsync vorab zurückliefert.</summary>
    public DomainError? Rejection { get; set; }

    public Task<Result<WorkingRoleMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingRoleMutationState, Result<WorkingRoleMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default,
        long? expectedChangeVersion = null)
    {
        if (Rejection is not null)
            return Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Failure(Rejection));

        if (expectedChangeVersion.HasValue && expectedChangeVersion.Value != ChangeVersion)
        {
            return Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Failure(new DomainError(
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
            return Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Failure(
                decisionResult.Error!, decisionResult.Warnings));

        var decision = decisionResult.Value!;
        var stateChanged = HasStateChanged(previousState, decision.State);
        State = decision.State;
        if (stateChanged)
            ChangeVersion++;

        return Task.FromResult(Result<WorkingRoleMutationExecution<T>>.Success(
            new WorkingRoleMutationExecution<T>(
                decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
    }

    private static bool HasStateChanged(WorkingRoleMutationState before, WorkingRoleMutationState after) =>
        !before.Roles.SequenceEqual(after.Roles)
        || !before.Resolutions.SequenceEqual(after.Resolutions);
}
