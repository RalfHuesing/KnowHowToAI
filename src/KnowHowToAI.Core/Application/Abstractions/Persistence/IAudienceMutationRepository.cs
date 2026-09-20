using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Application.Mutations.Audiences;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für Zielgruppen- und Resolution-Order-Mutationen auf einem offenen Working Snapshot.
/// </summary>
public interface IAudienceMutationRepository
{
    /// <summary>Liest, transformiert und speichert Zielgruppen und Resolution Orders unter derselben kurzen Sperre.</summary>
    Task<Result<WorkingAudienceMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingAudienceMutationState, Result<WorkingAudienceMutationDecision<T>>> mutate,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default);
}
