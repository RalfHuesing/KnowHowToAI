using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.History;

[Trait("Category", "Unit")]
public sealed class HistoryServiceTests
{
    private static readonly SnapshotId CommittedSnap1 = new(10);
    private static readonly SnapshotId CommittedSnap2 = new(11);
    private static readonly SnapshotId WorkingSnap = new(12);
    private static readonly SnapshotId UnknownSnap = new(999);
    private static readonly TransactionId OpenTxId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly TransactionId DiscardedTxId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly TransactionId CommittedTxId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public async Task GetSnapshotAsync_Found_ReturnsSnapshot()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var result = await service.GetSnapshotAsync(CommittedSnap1);

        Assert.True(result.IsSuccess);
        Assert.Equal(CommittedSnap1, result.Value!.SnapshotId);
        Assert.Equal(SnapshotState.Committed, result.Value.State);
    }

    [Fact]
    public async Task GetSnapshotAsync_NotFound_ReturnsSnapshotNotFound()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var result = await service.GetSnapshotAsync(UnknownSnap);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CompareSnapshotsAsync_BaseSnapshotNotFound_ReturnsError()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var result = await service.CompareSnapshotsAsync(UnknownSnap, CommittedSnap2);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CompareSnapshotsAsync_TargetSnapshotNotFound_ReturnsError()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var result = await service.CompareSnapshotsAsync(CommittedSnap1, UnknownSnap);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CompareSnapshotsAsync_SnapshotNotCommitted_ReturnsError()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        // WorkingSnap is not committed
        var result = await service.CompareSnapshotsAsync(CommittedSnap1, WorkingSnap);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotCommitted, result.Error!.Code);
    }

    [Fact]
    public async Task CompareSnapshotsAsync_InvalidCursor_ReturnsInvalidCursor()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var result = await service.CompareSnapshotsAsync(CommittedSnap1, CommittedSnap2, cursor: "invalid-cursor-string");

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.InvalidCursor, result.Error!.Code);
    }

    [Fact]
    public async Task CompareSnapshotsAsync_CursorMismatchedSnapshots_ReturnsInvalidCursor()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var wrongCursor = new DiffCursor(CommittedSnap1, UnknownSnap, null, 10).Encode();
        var result = await service.CompareSnapshotsAsync(CommittedSnap1, CommittedSnap2, cursor: wrongCursor);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.InvalidCursor, result.Error!.Code);
    }

    [Fact]
    public async Task CompareSnapshotsAsync_ValidDiff_ComputesAndPagesChanges()
    {
        var harness = new HistoryTestHarness();
        var node1 = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var node2 = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002"));

        harness.Nodes.Add(new Node(CommittedSnap1, node1, null, "Title 1", "Desc", 1, false));
        harness.Nodes.Add(new Node(CommittedSnap2, node1, null, "Updated Title 1", "Desc", 1, false));
        harness.Nodes.Add(new Node(CommittedSnap2, node2, null, "New Node 2", "Desc", 2, false));

        var service = harness.CreateService();
        var result = await service.CompareSnapshotsAsync(CommittedSnap1, CommittedSnap2, limit: 1);

        Assert.True(result.IsSuccess);
        var diff = result.Value!;
        Assert.Equal(2, diff.TotalCount);
        Assert.Single(diff.Nodes); // Limit is 1
        Assert.NotNull(diff.NextCursor);

        // Fetch page 2
        var p2Result = await service.CompareSnapshotsAsync(CommittedSnap1, CommittedSnap2, limit: 1, cursor: diff.NextCursor);
        Assert.True(p2Result.IsSuccess);
        Assert.Single(p2Result.Value!.Nodes);
        Assert.Null(p2Result.Value.NextCursor);
    }

    [Fact]
    public async Task GetTransactionChangesAsync_TransactionNotFound_ReturnsError()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var unknownTx = new TransactionId(Guid.NewGuid());
        var result = await service.GetTransactionChangesAsync(unknownTx);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.TransactionNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task GetTransactionChangesAsync_TransactionDiscarded_ReturnsError()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        var result = await service.GetTransactionChangesAsync(DiscardedTxId);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.TransactionDiscarded, result.Error!.Code);
    }

    [Fact]
    public async Task GetTransactionChangesAsync_OpenTransaction_ReturnsChanges()
    {
        var harness = new HistoryTestHarness();
        var nodeId = new NodeId(Guid.NewGuid());
        harness.Nodes.Add(new Node(WorkingSnap, nodeId, null, "New In Working", null, 0, false));

        var service = harness.CreateService();
        var result = await service.GetTransactionChangesAsync(OpenTxId);

        Assert.True(result.IsSuccess);
        Assert.Equal(OpenTxId, result.Value!.Transaction.TransactionId);
        var diff = result.Value.Changes;
        Assert.Equal(1, diff.TotalCount);
        var addedNode = Assert.Single(diff.Nodes);
        Assert.Equal(DiffChangeKind.Added, addedNode.Kind);
    }

    [Fact]
    public async Task GetTransactionChangesAsync_WorkingReadMutation_ReturnsCursorExpired()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();

        // Cursor was created for ChangeVersion 1
        var cursor = new DiffCursor(CommittedSnap1, WorkingSnap, ChangeVersion: 1L, NextOffset: 5).Encode();

        // Transaction in harness currently has ChangeVersion: 2
        var result = await service.GetTransactionChangesAsync(OpenTxId, cursor: cursor);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.CursorExpired, result.Error!.Code);
    }

    [Fact]
    public async Task GetTransactionChangesAsync_WorkingCursorWithoutChangeVersion_ReturnsCursorExpired()
    {
        var harness = new HistoryTestHarness();
        var service = harness.CreateService();
        var cursor = new DiffCursor(CommittedSnap1, WorkingSnap, ChangeVersion: null, NextOffset: 0).Encode();

        var result = await service.GetTransactionChangesAsync(OpenTxId, cursor: cursor);

        Assert.False(result.IsSuccess);
        Assert.Equal(HistoryErrorCodes.CursorExpired, result.Error!.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CompareSnapshotsAsync_NonPositiveLimit_UsesConfiguredDefault(int limit)
    {
        var harness = new HistoryTestHarness();
        for (var index = 0; index < 12; index++)
        {
            var nodeId = new NodeId(Guid.Parse($"00000000-0000-0000-0000-{index + 1:000000000000}"));
            harness.Nodes.Add(new Node(CommittedSnap2, nodeId, null, $"Node {index}", null, index, false));
        }

        var result = await harness.CreateService().CompareSnapshotsAsync(CommittedSnap1, CommittedSnap2, limit);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Nodes.Count);
        Assert.NotNull(result.Value.NextCursor);
    }

    // ── Test Harness (düner Wrapper über die gemeinsamen TestSupport-Fakes) ──

    private sealed class HistoryTestHarness
    {
        private readonly InMemoryKnowledgeStore _store = new();

        public List<Node> Nodes => _store.Nodes;

        public HistoryTestHarness()
        {
            _store.Snapshots.Add(new Snapshot(CommittedSnap1, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            _store.Snapshots.Add(new Snapshot(CommittedSnap2, CommittedSnap1, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            _store.Snapshots.Add(new Snapshot(WorkingSnap, CommittedSnap1, SnapshotState.Working, DateTimeOffset.UtcNow, null));

            _store.Transactions[OpenTxId] = new KnowledgeTransaction(
                OpenTxId, CommittedSnap1, WorkingSnap, TransactionState.Open, ChangeVersion: 2L,
                DateTimeOffset.UtcNow, null, "test", "tester", "client", null);

            _store.Transactions[DiscardedTxId] = new KnowledgeTransaction(
                DiscardedTxId, CommittedSnap1, WorkingSnap, TransactionState.Discarded, ChangeVersion: 1L,
                DateTimeOffset.UtcNow, null, "test", "tester", "client", null);

            _store.Transactions[CommittedTxId] = new KnowledgeTransaction(
                CommittedTxId, CommittedSnap1, CommittedSnap2, TransactionState.Committed, ChangeVersion: 3L,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "test", "tester", "client", "commit message");
        }

        public HistoryService CreateService() => new(
            new SnapshotReadRepositories(
                new InMemorySnapshotRepository(_store),
                new InMemoryTransactionRepository(_store),
                new InMemoryHierarchyRepository(_store),
                new InMemoryContentRepository(_store),
                new InMemoryRoleRepository(_store),
                new InMemoryDependencyRepository(_store)),
            new RetrievalPolicy
            {
                DefaultPageSize = 10,
                MaximumPageSize = 50,
                SearchPageSize = 10,
                SearchMaximumPageSize = 50,
                SnippetMaximumCharacters = 100
            });
    }
}
