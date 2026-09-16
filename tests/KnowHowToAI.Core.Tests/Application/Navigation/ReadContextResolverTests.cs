using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class ReadContextResolverTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly SnapshotId WorkingSnapshotId = new(11);
    private static readonly SnapshotId HistoricalSnapshotId = new(5);
    private static readonly TransactionId TransactionId = new(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 9, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_WithoutSelector_UsesCurrentSnapshot()
    {
        var result = ReadContextResolver.Resolve(new ReadContext(), Candidates());

        Assert.True(result.IsSuccess);
        Assert.Equal(CurrentSnapshotId, result.Value!.SnapshotId);
        Assert.Equal(ReadContextSource.Current, result.Value.Source);
        Assert.Null(result.Value.TransactionId);
        Assert.False(result.Value.IncludeDeleted);
    }

    [Fact]
    public void Resolve_TransactionSelector_UsesOnlyThatTransactionsWorkingSnapshot()
    {
        var result = ReadContextResolver.Resolve(new ReadContext(TransactionId: TransactionId), Candidates(transaction: Transaction()));

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkingSnapshotId, result.Value!.SnapshotId);
        Assert.NotEqual(CurrentSnapshotId, result.Value.SnapshotId);
        Assert.Equal(ReadContextSource.Transaction, result.Value.Source);
        Assert.Equal(TransactionId, result.Value.TransactionId);
    }

    [Fact]
    public void Resolve_SnapshotSelector_UsesExistingHistoricalSnapshot()
    {
        var historicalSnapshot = new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, CreatedAtUtc, CreatedAtUtc);

        var result = ReadContextResolver.Resolve(
            new ReadContext(SnapshotId: HistoricalSnapshotId),
            Candidates(snapshot: historicalSnapshot));

        Assert.True(result.IsSuccess);
        Assert.Equal(HistoricalSnapshotId, result.Value!.SnapshotId);
        Assert.Equal(ReadContextSource.Snapshot, result.Value.Source);
    }

    [Fact]
    public void Resolve_BothSelectors_ReturnsInvalidReadContextWithBothDetails()
    {
        var result = ReadContextResolver.Resolve(
            new ReadContext(TransactionId, HistoricalSnapshotId),
            Candidates(transaction: Transaction()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details["transactionId"]);
        Assert.Equal(HistoricalSnapshotId.ToString(), result.Details["snapshotId"]);
    }

    [Fact]
    public void Resolve_MissingTransaction_ReturnsTransactionNotFound()
    {
        var result = ReadContextResolver.Resolve(new ReadContext(TransactionId: TransactionId), Candidates());

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.TransactionNotFound, result.Code);
    }

    [Fact]
    public void Resolve_MissingHistoricalSnapshot_ReturnsSnapshotNotFound()
    {
        var result = ReadContextResolver.Resolve(
            new ReadContext(SnapshotId: HistoricalSnapshotId),
            Candidates());

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.SnapshotNotFound, result.Code);
    }

    [Fact]
    public void Resolve_TransactionSelector_ClosedTransaction_ReturnsTransactionClosed()
    {
        var closedTransaction = new KnowledgeTransaction(
            TransactionId,
            CurrentSnapshotId,
            WorkingSnapshotId,
            TransactionState.Committed,
            5,
            CreatedAtUtc,
            CreatedAtUtc,
            null,
            null,
            null,
            null);

        var result = ReadContextResolver.Resolve(
            new ReadContext(TransactionId: TransactionId),
            Candidates(transaction: closedTransaction));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.TransactionClosed, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details["transactionId"]);
    }

    [Fact]
    public void Resolve_SnapshotSelector_NotCommittedSnapshot_ReturnsSnapshotNotCommitted()
    {
        var workingSnapshot = new Snapshot(HistoricalSnapshotId, null, SnapshotState.Working, CreatedAtUtc, null);

        var result = ReadContextResolver.Resolve(
            new ReadContext(SnapshotId: HistoricalSnapshotId),
            Candidates(snapshot: workingSnapshot));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.SnapshotNotCommitted, result.Code);
        Assert.Equal(HistoricalSnapshotId.ToString(), result.Details["snapshotId"]);
    }

    [Fact]
    public void Resolve_TransactionSelector_PropagatesChangeVersion()
    {
        var transaction = new KnowledgeTransaction(
            TransactionId,
            CurrentSnapshotId,
            WorkingSnapshotId,
            TransactionState.Open,
            ChangeVersion: 12,
            CreatedAtUtc,
            null,
            null,
            null,
            null,
            null);

        var result = ReadContextResolver.Resolve(
            new ReadContext(TransactionId: TransactionId),
            Candidates(transaction: transaction));

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value!.ChangeVersion);
    }

    [Fact]
    public void ActiveReadFilter_ExcludesTombstonesUnlessExplicitlyRequested()
    {
        var activeNode = new Node(CurrentSnapshotId, new NodeId(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f")), null, "Aktiv", null, 0, false);
        var deletedNode = new Node(CurrentSnapshotId, new NodeId(Guid.Parse("342c9f4a-0a77-44be-8f98-f403444d3e9f")), null, "Gelöscht", null, 1, true);
        var entities = new[] { activeNode, deletedNode };
        var activeContext = ReadContextResolver.Resolve(new ReadContext(), Candidates()).Value!;
        var includingDeletedContext = ReadContextResolver.Resolve(new ReadContext(IncludeDeleted: true), Candidates()).Value!;

        Assert.Equal([activeNode], ActiveReadFilter.Apply(entities, activeContext));
        Assert.Equal(entities, ActiveReadFilter.Apply(entities, includingDeletedContext));
    }

    private static ReadContextCandidates Candidates(KnowledgeTransaction? transaction = null, Snapshot? snapshot = null) =>
        new(CurrentSnapshotId, transaction, snapshot);

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
