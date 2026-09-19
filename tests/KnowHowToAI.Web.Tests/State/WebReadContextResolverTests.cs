using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.State;

[Trait("Category", "Unit")]
public sealed class WebReadContextResolverTests
{
    private sealed class FakeReleaseRepository : IReleaseRepository
    {
        private readonly Dictionary<ReleaseId, Release> _releases = new();

        public void Add(Release release) => _releases[release.ReleaseId] = release;

        public Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        {
            _releases.TryGetValue(releaseId, out var release);
            return Task.FromResult(release);
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private readonly Dictionary<TransactionId, KnowledgeTransaction> _transactions = new();

        public void Add(KnowledgeTransaction tx) => _transactions[tx.TransactionId] = tx;

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
        {
            _transactions.TryGetValue(transactionId, out var tx);
            return Task.FromResult(tx);
        }

        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task ResolveAsync_WithNoSelectors_ReturnsCurrentSnapshot()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ReadContext.TransactionId);
        Assert.Null(result.Value.ReadContext.SnapshotId);
        Assert.Equal(KnowledgeReadContextKind.Current, result.Value.ContextViewModel.ReadContext);
        Assert.Null(result.Value.ContextViewModel.ContextId);
    }

    [Fact]
    public async Task ResolveAsync_WithMultipleSelectors_FailsWithInvalidReadContext()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(
            Guid.NewGuid().ToString("D"),
            "10",
            null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithValidTransactionId_ReturnsTransactionContext()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var txGuid = Guid.NewGuid();
        var txId = new TransactionId(txGuid);
        var tx = new KnowledgeTransaction(
            txId,
            new SnapshotId(10),
            new SnapshotId(11),
            TransactionState.Open,
            ChangeVersion: 3,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            CommittedAtUtc: null,
            Purpose: "Neue Feature-Dokumentation",
            Actor: "Alice",
            Client: "Web UI",
            CommitMessage: null);
        txRepo.Add(tx);

        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(txGuid.ToString("D"), null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(txGuid, result.Value!.ReadContext.TransactionId?.Value);
        Assert.Null(result.Value.ReadContext.SnapshotId);
        Assert.Equal(KnowledgeReadContextKind.Transaction, result.Value.ContextViewModel.ReadContext);
        Assert.Equal(txGuid.ToString("D"), result.Value.ContextViewModel.ContextId);
        Assert.Equal("Neue Feature-Dokumentation", result.Value.ContextViewModel.DisplayName);
        Assert.Equal(10L, result.Value.ContextViewModel.BaseSnapshotId);
        Assert.Equal(3L, result.Value.ChangeVersion);
    }

    [Fact]
    public async Task ResolveAsync_WithNonExistentTransactionId_FailsWithTransactionNotFound()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(Guid.NewGuid().ToString("D"), null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.TransactionNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithClosedTransaction_FailsWithTransactionClosed()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var txGuid = Guid.NewGuid();
        var txId = new TransactionId(txGuid);
        var tx = new KnowledgeTransaction(
            txId,
            new SnapshotId(10),
            new SnapshotId(11),
            TransactionState.Committed,
            ChangeVersion: 2,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
            CommittedAtUtc: DateTimeOffset.UtcNow,
            Purpose: "Bereits abgeschlossen",
            Actor: "Bob",
            Client: "Web UI",
            CommitMessage: "Fertig");
        txRepo.Add(tx);

        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(txGuid.ToString("D"), null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.TransactionClosed, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidTransactionId_FailsWithInvalidReadContext()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync("not-a-guid", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithValidSnapshotId_ReturnsSnapshotContext()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(null, "42", null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ReadContext.TransactionId);
        Assert.Equal(42L, result.Value.ReadContext.SnapshotId?.Value);
        Assert.Equal(KnowledgeReadContextKind.Snapshot, result.Value.ContextViewModel.ReadContext);
        Assert.Equal("42", result.Value.ContextViewModel.ContextId);
        Assert.Equal("Snapshot 42", result.Value.ContextViewModel.DisplayName);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidSnapshotId_FailsWithInvalidReadContext()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(null, "-5", null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithExistingReleaseId_ResolvesToReleaseSnapshot()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var releaseId = new ReleaseId(1);
        var snapshotId = new SnapshotId(100);
        releaseRepo.Add(new Release(releaseId, snapshotId, "v1.0.0", "Release 1", DateTimeOffset.UtcNow));

        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(null, null, "1");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ReadContext.TransactionId);
        Assert.Equal(100L, result.Value.ReadContext.SnapshotId?.Value);
        Assert.Equal(KnowledgeReadContextKind.Release, result.Value.ContextViewModel.ReadContext);
        Assert.Equal("1", result.Value.ContextViewModel.ContextId);
        Assert.Equal("v1.0.0", result.Value.ContextViewModel.DisplayName);
    }

    [Fact]
    public async Task ResolveAsync_WithNonExistentReleaseId_FailsWithReleaseNotFound()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(null, null, "999");

        Assert.False(result.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.ReleaseNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidReleaseId_FailsWithInvalidReadContext()
    {
        var releaseRepo = new FakeReleaseRepository();
        var txRepo = new FakeTransactionRepository();
        var resolver = new WebReadContextResolver(releaseRepo, txRepo);

        var result = await resolver.ResolveAsync(null, null, "abc");

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
    }
}
