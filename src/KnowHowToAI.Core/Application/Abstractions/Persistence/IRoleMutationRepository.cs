using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Application.Mutations.Roles;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für Rollen- und Resolution-Order-Mutationen auf einem offenen Working Snapshot.
/// </summary>
public interface IRoleMutationRepository
{
    /// <summary>Liest, transformiert und speichert Rollen und Resolution Orders unter derselben kurzen Sperre.</summary>
    Task<Result<WorkingRoleMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingRoleMutationState, Result<WorkingRoleMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default,
        long? expectedChangeVersion = null);
}
