using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Search;

[Trait("Category", "Unit")]
public sealed class SearchServiceTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(100);
    private static readonly SnapshotId WorkingSnapshotId = new(101);
    private static readonly SnapshotId OtherSnapshotId = new(999);
    private static readonly TransactionId OpenTransactionId = new(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"));
    private static readonly AudienceId AudienceDev = new("Developer");

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

        harness.RetrievalRepo.ConfigureActiveAudience(AudienceDev);
        harness.RetrievalRepo.ResultsToReturn = new List<SearchHit>
        {
            new(node1, "Node 1", "Desc 1", null, "Title", Availability.Explicit, AudienceDev, Freshness.Current, 10),
            new(node2, "Node 2", "Desc 2", "Snippet 2", "Description", Availability.Explicit, AudienceDev, Freshness.Current, 20),
            new(node3, "Node 3", "Desc 3", "Snippet 3", "Content", Availability.Fallback, AudienceDev, Freshness.Current, 30)
        };

        var service = harness.CreateService();
        var query = new SearchQuery("Node", Limit: 2, AudienceId: AudienceDev);
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
        Assert.Equal(AudienceDev, decodedCursor.AudienceId);
        Assert.Equal(2, decodedCursor.LastRank); // HitField = Description -> Rank 2
        Assert.Equal(20, decodedCursor.LastSortOrder);
        Assert.Equal(node2, decodedCursor.LastNodeId);
    }

    [Fact]
    public async Task SearchAsync_FiltersFacetsWithOrInsideAndAcrossGroupsBeforePaging()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var fallbackAudience = new AudienceId("Shared");
        harness.RetrievalRepo.ConfigureActiveAudience(AudienceDev);
        harness.RetrievalRepo.ResultsToReturn =
        [
            new(new NodeId(Guid.Parse("11111111-1111-1111-1111-111111111111")), "Explicit", null, null, "Title", Availability.Explicit, AudienceDev, Freshness.Current, 1),
            new(new NodeId(Guid.Parse("22222222-2222-2222-2222-222222222222")), "Fallback", null, null, "Title", Availability.Fallback, fallbackAudience, Freshness.Stale, 2, ["StaleDerivedContent"]),
            new(new NodeId(Guid.Parse("33333333-3333-3333-3333-333333333333")), "Other", null, null, "Title", Availability.Fallback, new AudienceId("Other"), Freshness.Stale, 3, ["StaleDerivedContent"])
        ];
        var filter = new SearchFilter(
            [AudienceDev, fallbackAudience],
            [Availability.Explicit, Availability.Fallback],
            [Freshness.Stale],
            ["StaleDerivedContent"]);

        var result = await harness.CreateService().SearchAsync(
            new SearchQuery("text", Limit: 1, AudienceId: AudienceDev, Filter: filter), new ReadContext());

        Assert.True(result.IsSuccess);
        Assert.Equal("Fallback", Assert.Single(result.Value!.Items).Title);
        Assert.Equal(filter, harness.RetrievalRepo.LastRequest!.Filter);
    }

    [Fact]
    public async Task SearchAsync_CursorFromAnotherFilter_ReturnsInvalidCursor()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var oldFilter = new SearchFilter(Availabilities: [Availability.Explicit]);
        var cursor = new SearchCursor(
            CurrentSnapshotId, null, "text", null, 1, 0, new NodeId(Guid.NewGuid()), oldFilter.Fingerprint).Encode();

        var result = await harness.CreateService().SearchAsync(
            new SearchQuery("text", Cursor: cursor, Filter: new SearchFilter(Availabilities: [Availability.Fallback])),
            new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.InvalidCursor, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_WithoutAudience_DoesNotRequireAudienceResolutionData()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var query = new SearchQuery("text");
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.NextCursor);
        Assert.Equal(1, harness.RetrievalRepo.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WithUnknownRequestedAudience_ReturnsRequestedAudienceNotFound()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var service = harness.CreateService();

        var query = new SearchQuery("text", AudienceId: AudienceDev);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceNotFound, result.Error!.Code);
        Assert.Equal(
            AudienceDev.ToString(),
            Assert.Single(result.Error.Details).Value);
    }

    [Fact]
    public async Task SearchAsync_WithDeletedRequestedAudience_ReturnsRequestedAudienceDeleted()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        harness.RetrievalRepo.ConfigureActiveAudience(AudienceDev, isDeleted: true);
        var service = harness.CreateService();

        var query = new SearchQuery("text", AudienceId: AudienceDev);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceDeleted, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_WithDeletedCandidateAudience_ReturnsCandidateAudienceDeleted()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var candidateAudience = new AudienceId("Consultant");
        harness.RetrievalRepo.ConfigureActiveAudience(AudienceDev);
        harness.RetrievalRepo.Audiences.Add(new Audience(CurrentSnapshotId, candidateAudience, "Consultant", null, IsDeleted: true));
        harness.RetrievalRepo.Resolutions.Add(new AudienceResolution(CurrentSnapshotId, AudienceDev, candidateAudience, 2));
        var service = harness.CreateService();

        var query = new SearchQuery("text", AudienceId: AudienceDev);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceDeleted, result.Error!.Code);
        Assert.Equal(
            candidateAudience.ToString(),
            result.Error.Details[AudienceResolutionErrorCodes.CandidateAudienceIdDetail]);
    }

    [Fact]
    public async Task SearchAsync_WithUnknownCandidateAudience_ReturnsCandidateAudienceNotFound()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        harness.RetrievalRepo.ConfigureActiveAudience(AudienceDev);
        harness.RetrievalRepo.Resolutions.Add(new AudienceResolution(CurrentSnapshotId, AudienceDev, new AudienceId("Consultant"), 2));
        var service = harness.CreateService();

        var query = new SearchQuery("text", AudienceId: AudienceDev);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_WithDuplicateCandidateAudience_ReturnsDuplicateCandidateAudience()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var candidateAudience = new AudienceId("Consultant");
        harness.RetrievalRepo.ConfigureActiveAudience(AudienceDev);
        harness.RetrievalRepo.Audiences.Add(new Audience(CurrentSnapshotId, candidateAudience, "Consultant", null, IsDeleted: false));
        harness.RetrievalRepo.Resolutions.Add(new AudienceResolution(CurrentSnapshotId, AudienceDev, candidateAudience, 2));
        harness.RetrievalRepo.Resolutions.Add(new AudienceResolution(CurrentSnapshotId, AudienceDev, candidateAudience, 3));
        var service = harness.CreateService();

        var query = new SearchQuery("text", AudienceId: AudienceDev);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.DuplicateCandidateAudience, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_WithoutConfiguredResolutionOrder_ReturnsHitsWithoutError()
    {
        var harness = new SearchTestHarness(CurrentSnapshotId);
        var node1 = new NodeId(Guid.Parse("11111111-0000-0000-0000-000000000001"));
        harness.RetrievalRepo.ConfigureActiveAudience(AudienceDev);
        harness.RetrievalRepo.ResultsToReturn = new List<SearchHit>
        {
            new(node1, "Node 1", null, null, "Title", Availability.None, null, Freshness.Unknown, 10)
        };
        var service = harness.CreateService();

        var query = new SearchQuery("Node", AudienceId: AudienceDev);
        var result = await service.SearchAsync(query, new ReadContext());

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(node1, hit.NodeId);
        Assert.Equal("Title", hit.HitField);
    }

    // ── Test Harness (düner Wrapper über die gemeinsamen TestSupport-Fakes) ──

    private sealed class SearchTestHarness
    {
        private readonly InMemoryKnowledgeStore _store;

        public InMemoryRetrievalRepository RetrievalRepo { get; } = new(CurrentSnapshotId);

        public SearchTestHarness(SnapshotId currentSnapshotId) =>
            _store = InMemoryKnowledgeStore.WithCurrentCommittedSnapshot(currentSnapshotId, DateTimeOffset.UtcNow);

        public void AddTransaction(KnowledgeTransaction tx) => _store.Transactions[tx.TransactionId] = tx;

        public SearchService CreateService() => new(
            new SearchRepositories(
                new InMemorySnapshotRepository(_store),
                new InMemoryTransactionRepository(_store),
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
}
