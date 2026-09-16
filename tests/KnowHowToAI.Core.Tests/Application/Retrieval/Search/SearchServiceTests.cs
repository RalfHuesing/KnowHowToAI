using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Search;

[Trait("Category", "Unit")]
public sealed class SearchServiceTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(100);
    private static readonly SnapshotId WorkingSnapshotId = new(101);
    private static readonly SnapshotId OtherSnapshotId = new(999);
    private static readonly TransactionId OpenTransactionId = new(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"));
    private static readonly RoleId RoleDev = new("Developer");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_EmptyOrWhitespaceQuery_ReturnsEmptyPageWithoutCallingRepo(string? emptyText)
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var query = new SearchQuery(emptyText!);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Null(result.Value.NextCursor);
        Assert.Equal(0, harness.RetrievalRepo.CallCount);
    }

    [Fact]
    public async Task SearchAsync_TransactionNotFound_ReturnsDomainError()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var unknownTx = new TransactionId(Guid.NewGuid());
        var query = new SearchQuery("text");
        var context = new ReadContext(TransactionId: unknownTx);

        var result = await service.SearchAsync(query, context);

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.TransactionNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_InvalidCursorString_ReturnsInvalidCursorError()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var query = new SearchQuery("text", Cursor: "invalid-cursor-value");
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.InvalidCursor, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_CursorFromDifferentSnapshotOnCurrentRead_ReturnsCursorExpired()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var cursor = new SearchCursor(OtherSnapshotId, null, "text", null, 1, 0, new NodeId(Guid.NewGuid())).Encode();
        var query = new SearchQuery("text", Cursor: cursor);

        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.CursorExpired, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_CursorQueryMismatch_ReturnsInvalidCursor()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var cursor = new SearchCursor(CurrentSnapshotId, null, "originalQuery", null, 1, 0, new NodeId(Guid.NewGuid())).Encode();
        var query = new SearchQuery("differentQuery", Cursor: cursor);

        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.InvalidCursor, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_WorkingReadChangeVersionMismatch_ReturnsCursorExpired()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        harness.AddTransaction(new KnowledgeTransaction(
            OpenTransactionId, CurrentSnapshotId, WorkingSnapshotId,
            TransactionState.Open, ChangeVersion: 5L, DateTimeOffset.UtcNow, null, "test", "tester", "client", null));

        var service = harness.CreateService();

        var cursor = new SearchCursor(WorkingSnapshotId, 3L, "text", null, 1, 0, new NodeId(Guid.NewGuid())).Encode();
        var query = new SearchQuery("text", Cursor: cursor);
        var context = new ReadContext(TransactionId: OpenTransactionId);

        var result = await service.SearchAsync(query, context);

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.CursorExpired, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_ClampsLimitToMaximumSearchPageSize()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var query = new SearchQuery("term", Limit: 500);
        await service.SearchAsync(query, new ReadContext());

        Assert.NotNull(harness.RetrievalRepo.LastRequest);
        // Maximum configured in harness is 50, +1 for hasNext detection
        Assert.Equal(51, harness.RetrievalRepo.LastRequest!.Limit);
    }

    [Fact]
    public async Task SearchAsync_HasMoreItemsThanLimit_ReturnsPagedItemsAndNextCursor()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var node1 = new NodeId(Guid.Parse("11111111-0000-0000-0000-000000000001"));
        var node2 = new NodeId(Guid.Parse("22222222-0000-0000-0000-000000000002"));
        var node3 = new NodeId(Guid.Parse("33333333-0000-0000-0000-000000000003"));

        harness.RetrievalRepo.ResultsToReturn = new List<SearchHit>
        {
            new(node1, "Node 1", "Desc 1", null, "Title", Availability.Explicit, RoleDev, Freshness.Current, 10),
            new(node2, "Node 2", "Desc 2", "Snippet 2", "Description", Availability.Explicit, RoleDev, Freshness.Current, 20),
            new(node3, "Node 3", "Desc 3", "Snippet 3", "Content", Availability.Fallback, RoleDev, Freshness.Current, 30)
        };

        var service = harness.CreateService();
        var query = new SearchQuery("Node", Limit: 2, RoleId: RoleDev);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.True(result.IsSuccess);
        var page = result.Value!;
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("Node 1", page.Items[0].Title);
        Assert.Equal("Node 2", page.Items[1].Title);
        Assert.NotNull(page.NextCursor);

        var decodedCursor = SearchCursor.TryDecode(page.NextCursor);
        Assert.NotNull(decodedCursor);
        Assert.Equal(CurrentSnapshotId, decodedCursor.SnapshotId);
        Assert.Equal("Node", decodedCursor.QueryText);
        Assert.Equal(RoleDev, decodedCursor.RoleId);
        Assert.Equal(2, decodedCursor.LastRank); // HitField = Description -> Rank 2
        Assert.Equal(20, decodedCursor.LastSortOrder);
        Assert.Equal(node2, decodedCursor.LastNodeId);
    }

    private sealed class SearchTestHarness
    {
        private readonly SnapshotId _currentSnapshotId;
        private readonly List<Snapshot> _snapshots = new();
        private readonly Dictionary<TransactionId, KnowledgeTransaction> _transactions = new();

        public FakeRetrievalRepository RetrievalRepo { get; } = new();

        public SearchTestHarness(SnapshotId currentSnapshotId)
        {
            _currentSnapshotId = currentSnapshotId;
            _snapshots.Add(new Snapshot(_currentSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        }

        public void AddTransaction(KnowledgeTransaction tx) => _transactions[tx.TransactionId] = tx;

        public SearchService CreateService() => new(
            new SearchRepositories(
                new FakeSnapshotRepository(_snapshots, _currentSnapshotId),
                new FakeTransactionRepository(_transactions),
                RetrievalRepo),
            new RetrievalPolicy
            {
                DefaultPageSize = 10,
                MaximumPageSize = 100,
                SearchPageSize = 10,
                SearchMaximumPageSize = 50,
                SnippetMaximumCharacters = 100
            });
    }

    private sealed class FakeRetrievalRepository : IRetrievalRepository
    {
        public int CallCount { get; private set; }
        public SearchRequest? LastRequest { get; private set; }
        public List<SearchHit> ResultsToReturn { get; set; } = new();
        public long? ChangeVersionToReturn { get; set; }

        public Task<SearchRepositoryResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new SearchRepositoryResult(ResultsToReturn, ChangeVersionToReturn));
        }
    }

    private sealed class FakeSnapshotRepository : ISnapshotRepository
    {
        private readonly List<Snapshot> _snapshots;
        private readonly SnapshotId _currentSnapshotId;

        public FakeSnapshotRepository(List<Snapshot> snapshots, SnapshotId currentSnapshotId)
        {
            _snapshots = snapshots;
            _currentSnapshotId = currentSnapshotId;
        }

        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshots.FirstOrDefault(s => s.SnapshotId == snapshotId));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshots.First(s => s.SnapshotId == _currentSnapshotId));
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private readonly Dictionary<TransactionId, KnowledgeTransaction> _transactions;

        public FakeTransactionRepository(Dictionary<TransactionId, KnowledgeTransaction> transactions)
        {
            _transactions = transactions;
        }

        public Task<KnowledgeTransaction> BeginAsync(KnowHowToAI.Core.Application.Transactions.BeginTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_transactions.GetValueOrDefault(transactionId));
        public Task<KnowHowToAI.Core.Application.Transactions.CommitTransactionResult> CommitAsync(KnowHowToAI.Core.Application.Transactions.CommitTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
