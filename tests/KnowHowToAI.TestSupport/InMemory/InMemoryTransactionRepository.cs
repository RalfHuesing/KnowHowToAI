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
        CancellationToken cancellationToken = default)
    {
        var baseSnapshotId = store.CurrentSnapshotId ?? new SnapshotId(1);
        var workingSnapshotId = new SnapshotId(baseSnapshotId.Value + 1);
        var tx = new KnowledgeTransaction(
            request.TransactionId,
            baseSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            ChangeVersion: 0,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            CommittedAtUtc: null,
            request.Purpose,
            request.Actor,
            request.Client,
            CommitMessage: null);
        store.Transactions[request.TransactionId] = tx;
        return Task.FromResult(tx);
    }

    public Task<KnowledgeTransaction?> FindAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Transactions.GetValueOrDefault(transactionId));

    public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<KnowledgeTransaction>>(
            store.Transactions.Values
                .Where(t => t.State == TransactionState.Open)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToArray());

    public Task<CommitTransactionResult> CommitAsync(
        CommitTransactionRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("CommitAsync ist im In-Memory-Read-Fake nicht unterstützt.");

    public Task<Result<KnowledgeTransaction>> DiscardAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("DiscardAsync ist im In-Memory-Read-Fake nicht unterstützt.");
}
