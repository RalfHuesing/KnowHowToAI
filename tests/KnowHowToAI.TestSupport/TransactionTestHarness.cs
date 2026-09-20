using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
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
    private readonly InMemorySnapshotRepository _snapshotRepo;
    private readonly InMemoryHierarchyRepository _hierarchyRepo;
    private readonly InMemoryContentRepository _contentRepo;
    private readonly InMemoryAudienceRepository _audienceRepo;
    private readonly InMemoryDependencyRepository _dependencyRepo;
    private readonly InMemoryWorkingSnapshotReadRepository _workingSnapshotRepo;
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
        _snapshotRepo = new InMemorySnapshotRepository(Store);
        _hierarchyRepo = new InMemoryHierarchyRepository(Store);
        _contentRepo = new InMemoryContentRepository(Store);
        _audienceRepo = new InMemoryAudienceRepository(Store);
        _dependencyRepo = new InMemoryDependencyRepository(Store);
        _workingSnapshotRepo = new InMemoryWorkingSnapshotReadRepository(Store);
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

    public HistoryService CreateHistoryService(RetrievalPolicy? retrievalPolicy = null) =>
        new(
            new SnapshotReadRepositories(
                _snapshotRepo,
                _transactionRepo,
                _hierarchyRepo,
                _contentRepo,
                _audienceRepo,
                _dependencyRepo,
                _workingSnapshotRepo),
            retrievalPolicy ?? new RetrievalPolicy
            {
                DefaultPageSize = 100,
                MaximumPageSize = 100,
                SearchPageSize = 100,
                SearchMaximumPageSize = 100,
                SnippetMaximumCharacters = 200
            });
}
