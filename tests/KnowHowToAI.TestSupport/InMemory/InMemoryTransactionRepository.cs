using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="ITransactionRepository"/> für Read-Szenarien: <c>FindAsync</c>
/// löst über das Transaction-Verzeichnis des gemeinsamen Stores auf; schreibende
/// Operationen (Begin, Commit, Discard) sind im Read-Fake bewusst nicht unterstützt
/// und werfen eine <see cref="NotSupportedException"/>.
/// </summary>
public sealed class InMemoryTransactionRepository(InMemoryKnowledgeStore store) : ITransactionRepository
{
    public Task<KnowledgeTransaction> BeginAsync(
        BeginTransactionRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("BeginAsync ist im In-Memory-Read-Fake nicht unterstützt.");

    public Task<KnowledgeTransaction?> FindAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Transactions.GetValueOrDefault(transactionId));

    public Task<CommitTransactionResult> CommitAsync(
        CommitTransactionRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("CommitAsync ist im In-Memory-Read-Fake nicht unterstützt.");

    public Task<Result<KnowledgeTransaction>> DiscardAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("DiscardAsync ist im In-Memory-Read-Fake nicht unterstützt.");
}
