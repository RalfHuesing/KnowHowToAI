using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für Node-Mutationen auf einem offenen Working Snapshot.
/// Führt Lesen, fachliche Entscheidung und Persistenz unter derselben kurzen Sperre aus.
/// </summary>
public interface INodeMutationRepository
{
    /// <summary>Liest den vollständigen Node-Zustand und persistiert eine erfolgreiche Entscheidung atomar.</summary>
    Task<Result<WorkingNodeMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingNodeMutationState, Result<WorkingNodeMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default,
        long? expectedChangeVersion = null);
}
