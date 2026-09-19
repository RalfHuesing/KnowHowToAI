using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Gemeinsamer Test-Harness für Transaction-Tests (Core- und Web-Tests).
/// </summary>
public sealed class TransactionTestHarness
{
    public InMemoryKnowledgeStore Store { get; } = new();
    private readonly InMemoryTransactionRepository _transactionRepo;
    private readonly InMemoryWorkingSnapshotValidationDataRepository _validationDataRepo = new();
    public IIdentifierGenerator IdentifierGenerator { get; set; } = new GuidIdentifierGenerator();
    public ValidationPolicy ValidationPolicy { get; set; } = new();

    public TransactionTestHarness(SnapshotId? initialSnapshotId = null, DateTimeOffset? now = null)
    {
        var snapshotId = initialSnapshotId ?? new SnapshotId(1);
        var timestamp = now ?? DateTimeOffset.UtcNow;

        Store.CurrentSnapshotId = snapshotId;
        Store.Snapshots.Add(new Snapshot(snapshotId, null, SnapshotState.Committed, timestamp, timestamp));

        _transactionRepo = new InMemoryTransactionRepository(Store);
    }

    public void AddTransaction(KnowledgeTransaction tx)
    {
        Store.Transactions[tx.TransactionId] = tx;
    }

    public void SetValidationData(TransactionId txId, WorkingSnapshotValidationData data) =>
        _validationDataRepo.SetValidationData(txId, data);

    public TransactionService CreateService() =>
        new(
            _transactionRepo,
            _validationDataRepo,
            IdentifierGenerator,
            ValidationPolicy);
}
