using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;
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
        var store = new InMemoryKnowledgeStore();
        var snapshots = new InMemorySnapshotRepository(store) { ThrowOnGetCurrent = true };
        store.Snapshots.Add(new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, CreatedAtUtc, CreatedAtUtc));
        store.Transactions[TransactionId] = Transaction();

        var result = await ReadContextReader.ResolveAsync(
            new ReadContext(TransactionId: TransactionId),
            snapshots,
            new InMemoryTransactionRepository(store));

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkingSnapshotId, result.Value!.SnapshotId);
        Assert.Equal(ReadContextSource.Transaction, result.Value.Source);
        Assert.Equal(TransactionId, result.Value.TransactionId);
        Assert.Equal(0, snapshots.GetCurrentCalls);
    }

    [Fact]
    public async Task ResolveAsync_SnapshotSelector_DoesNotLoadCurrentSnapshot()
    {
        var store = new InMemoryKnowledgeStore();
        var snapshots = new InMemorySnapshotRepository(store) { ThrowOnGetCurrent = true };
        store.Snapshots.Add(new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, CreatedAtUtc, CreatedAtUtc));

        var result = await ReadContextReader.ResolveAsync(
            new ReadContext(SnapshotId: HistoricalSnapshotId),
            snapshots,
            new InMemoryTransactionRepository(store));

        Assert.True(result.IsSuccess);
        Assert.Equal(HistoricalSnapshotId, result.Value!.SnapshotId);
        Assert.Equal(ReadContextSource.Snapshot, result.Value.Source);
        Assert.Null(result.Value.TransactionId);
        Assert.Equal(0, snapshots.GetCurrentCalls);
    }

    [Fact]
    public async Task ResolveAsync_WithoutSelector_LoadsCurrentSnapshotExactlyOnce()
    {
        var store = new InMemoryKnowledgeStore { CurrentSnapshotId = CurrentSnapshotId };
        var snapshots = new InMemorySnapshotRepository(store);
        store.Snapshots.Add(new Snapshot(CurrentSnapshotId, null, SnapshotState.Committed, CreatedAtUtc, CreatedAtUtc));

        var result = await ReadContextReader.ResolveAsync(
            new ReadContext(),
            snapshots,
            new InMemoryTransactionRepository(store));

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
}
