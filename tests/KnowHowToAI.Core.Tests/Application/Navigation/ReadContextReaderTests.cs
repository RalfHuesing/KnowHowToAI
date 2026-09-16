using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using Xunit;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class ReadContextReaderTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly SnapshotId WorkingSnapshotId = new(11);
    private static readonly SnapshotId HistoricalSnapshotId = new(5);
    private static readonly TransactionId TransactionId = new(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 9, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ResolveAsync_TransactionSelector_DoesNotLoadCurrentSnapshot()
    {
        var snapshots = new SnapshotRepoFake { ThrowOnGetCurrent = true };
        snapshots.Add(new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, CreatedAtUtc, CreatedAtUtc));

        var result = await ReadContextReader.ResolveAsync(
            new ReadContext(TransactionId: TransactionId),
            snapshots,
            new TransactionRepoFake(Transaction()));

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkingSnapshotId, result.Value!.SnapshotId);
        Assert.Equal(ReadContextSource.Transaction, result.Value.Source);
        Assert.Equal(TransactionId, result.Value.TransactionId);
        Assert.Equal(0, snapshots.GetCurrentCalls);
    }

    [Fact]
    public async Task ResolveAsync_SnapshotSelector_DoesNotLoadCurrentSnapshot()
    {
        var snapshots = new SnapshotRepoFake { ThrowOnGetCurrent = true };
        snapshots.Add(new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, CreatedAtUtc, CreatedAtUtc));

        var result = await ReadContextReader.ResolveAsync(
            new ReadContext(SnapshotId: HistoricalSnapshotId),
            snapshots,
            new TransactionRepoFake(null));

        Assert.True(result.IsSuccess);
        Assert.Equal(HistoricalSnapshotId, result.Value!.SnapshotId);
        Assert.Equal(ReadContextSource.Snapshot, result.Value.Source);
        Assert.Null(result.Value.TransactionId);
        Assert.Equal(0, snapshots.GetCurrentCalls);
    }

    [Fact]
    public async Task ResolveAsync_WithoutSelector_LoadsCurrentSnapshotExactlyOnce()
    {
        var snapshots = new SnapshotRepoFake();
        snapshots.Add(new Snapshot(CurrentSnapshotId, null, SnapshotState.Committed, CreatedAtUtc, CreatedAtUtc));

        var result = await ReadContextReader.ResolveAsync(
            new ReadContext(),
            snapshots,
            new TransactionRepoFake(null));

        Assert.True(result.IsSuccess);
        Assert.Equal(CurrentSnapshotId, result.Value!.SnapshotId);
        Assert.Equal(ReadContextSource.Current, result.Value.Source);
        Assert.Null(result.Value.TransactionId);
        Assert.Equal(1, snapshots.GetCurrentCalls);
    }

    private static KnowledgeTransaction Transaction() =>
        new(
            TransactionId,
            CurrentSnapshotId,
            WorkingSnapshotId,
            TransactionState.Open,
            0,
            CreatedAtUtc,
            null,
            null,
            null,
            null,
            null);

    private sealed class SnapshotRepoFake : ISnapshotRepository
    {
        private readonly List<Snapshot> _snapshots = [];

        public int GetCurrentCalls { get; private set; }

        public bool ThrowOnGetCurrent { get; init; }

        public void Add(Snapshot snapshot) => _snapshots.Add(snapshot);

        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshots.FirstOrDefault(snapshot => snapshot.SnapshotId == snapshotId));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            GetCurrentCalls++;
            if (ThrowOnGetCurrent)
                throw new InvalidOperationException("GetCurrentAsync darf nur im Current-Zweig aufgerufen werden.");

            return Task.FromResult(_snapshots.First(snapshot => snapshot.SnapshotId == CurrentSnapshotId));
        }
    }

    private sealed class TransactionRepoFake(KnowledgeTransaction? transaction) : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(transaction);

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
