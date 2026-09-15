using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Application.Mutations.Content;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für Content-Mutationen auf einem offenen Working Snapshot.
/// </summary>
public interface IContentMutationRepository
{
    /// <summary>
    /// Liest, transformiert und speichert Contents samt Dependencies unter derselben kurzen Sperre.
    /// </summary>
    Task<Result<WorkingContentMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingContentMutationState, Result<WorkingContentMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default);
}
