using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IWorkingSnapshotReadRepository"/> über einen gemeinsamen
/// <see cref="InMemoryKnowledgeStore"/>: prüft Existenz und Offenheit der Transaction
/// sowie den Working-Zustand des Snapshots mit denselben stabilen Fehlercodes wie die
/// Produktions-Storage-Implementierung und liefert die auf den Working Snapshot
/// gefilterten Nodes, Zielgruppen, Auflösungen, Contents und Dependencies atomar zurück.
/// </summary>
public sealed class InMemoryWorkingSnapshotReadRepository(InMemoryKnowledgeStore store)
    : IWorkingSnapshotReadRepository
{
    public Task<Result<WorkingSnapshotReadData>> ReadOpenWorkingAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        if (!store.Transactions.TryGetValue(transactionId, out var transaction))
        {
            return Task.FromResult(Result<WorkingSnapshotReadData>.Failure(new DomainError(
                ReadContextErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                new Dictionary<string, string> { [NavigationErrorCodes.TransactionIdDetail] = transactionId.ToString() })));
        }

        if (transaction.State != TransactionState.Open)
        {
            return Task.FromResult(Result<WorkingSnapshotReadData>.Failure(new DomainError(
                ReadContextErrorCodes.TransactionClosed,
                "Die angefragte Transaction ist nicht offen.",
                new Dictionary<string, string> { [NavigationErrorCodes.TransactionIdDetail] = transactionId.ToString() })));
        }

        var snapshot = store.Snapshots.FirstOrDefault(s => s.SnapshotId == transaction.WorkingSnapshotId);
        if (snapshot is null || snapshot.State != SnapshotState.Working)
        {
            return Task.FromResult(Result<WorkingSnapshotReadData>.Failure(new DomainError(
                TransactionValidationErrorCodes.WorkingSnapshotNotOpen,
                "Der Working Snapshot der Transaction ist nicht bearbeitbar.",
                new Dictionary<string, string> { [NavigationErrorCodes.TransactionIdDetail] = transactionId.ToString() })));
        }

        var snapshotId = transaction.WorkingSnapshotId;
        return Task.FromResult(Result<WorkingSnapshotReadData>.Success(new WorkingSnapshotReadData(
            transaction,
            transaction.ChangeVersion,
            store.Nodes.Where(n => n.SnapshotId == snapshotId).ToList(),
            store.Audiences.Where(r => r.SnapshotId == snapshotId).ToList(),
            store.Resolutions.Where(r => r.SnapshotId == snapshotId).ToList(),
            store.Contents.Where(c => c.SnapshotId == snapshotId).ToList(),
            store.Dependencies.Where(d => d.SnapshotId == snapshotId).ToList())));
    }
}
