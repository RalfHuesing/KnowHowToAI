using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Core.Application.Transactions;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IAudienceMutationRepository"/>: führt die Mutation-Funktion auf
/// einem veränderlich gehaltenen Working-Zustand aus, persistiert erfolgreiche
/// Entscheidungen und erhöht die ChangeVersion nur bei echten Zustandsänderungen
/// (Zielgruppen oder Auflösungsreihenfolgen). Über <see cref="Rejection"/> lässt sich ein
/// Transaction-Fehler vorab einschleusen.
/// </summary>
public sealed class InMemoryAudienceMutationRepository(WorkingAudienceMutationState state)
    : IAudienceMutationRepository
{
    /// <summary>Aktueller Working-Zustand des Stores.</summary>
    public WorkingAudienceMutationState State { get; private set; } = state;

    /// <summary>Anzahl der persistierten Zustandsänderungen.</summary>
    public long ChangeVersion { get; private set; }

    /// <summary>Optionale Rejection-Injection: Fehler, der ExecuteAsync vorab zurückliefert.</summary>
    public DomainError? Rejection { get; set; }

    public Task<Result<WorkingAudienceMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingAudienceMutationState, Result<WorkingAudienceMutationDecision<T>>> mutate,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        if (Rejection is not null)
            return Task.FromResult(Result<WorkingAudienceMutationExecution<T>>.Failure(Rejection));

        if (expectedChangeVersion != ChangeVersion)
        {
            return Task.FromResult(Result<WorkingAudienceMutationExecution<T>>.Failure(new DomainError(
                TransactionValidationErrorCodes.ChangeVersionConflict,
                "Die Transaction wurde zwischen Laden und Speichern geändert.",
                new Dictionary<string, string>
                {
                    [TransactionValidationErrorCodes.ExpectedChangeVersionDetail] = expectedChangeVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [TransactionValidationErrorCodes.ActualChangeVersionDetail] = ChangeVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
                })));
        }

        var previousState = State;
        var decisionResult = mutate(previousState);
        if (!decisionResult.IsSuccess)
            return Task.FromResult(Result<WorkingAudienceMutationExecution<T>>.Failure(
                decisionResult.Error!, decisionResult.Warnings));

        var decision = decisionResult.Value!;
        var stateChanged = HasStateChanged(previousState, decision.State);
        State = decision.State;
        if (stateChanged)
            ChangeVersion++;

        return Task.FromResult(Result<WorkingAudienceMutationExecution<T>>.Success(
            new WorkingAudienceMutationExecution<T>(
                decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
    }

    private static bool HasStateChanged(WorkingAudienceMutationState before, WorkingAudienceMutationState after) =>
        !before.Audiences.SequenceEqual(after.Audiences)
        || !before.Resolutions.SequenceEqual(after.Resolutions);
}
