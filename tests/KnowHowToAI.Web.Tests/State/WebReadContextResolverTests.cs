using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout;
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

    [Fact]
    public async Task ResolveAsync_WithNoSelectors_ReturnsCurrentSnapshot()
    {
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);

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
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);

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
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);
        var txGuid = Guid.NewGuid();

        var result = await resolver.ResolveAsync(txGuid.ToString("D"), null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(txGuid, result.Value!.ReadContext.TransactionId?.Value);
        Assert.Null(result.Value.ReadContext.SnapshotId);
        Assert.Equal(KnowledgeReadContextKind.Transaction, result.Value.ContextViewModel.ReadContext);
        Assert.Equal(txGuid.ToString("D"), result.Value.ContextViewModel.ContextId);
        Assert.Equal($"Transaktion {txGuid:D}", result.Value.ContextViewModel.DisplayName);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidTransactionId_FailsWithInvalidReadContext()
    {
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);

        var result = await resolver.ResolveAsync("not-a-guid", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithValidSnapshotId_ReturnsSnapshotContext()
    {
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);

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
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);

        var result = await resolver.ResolveAsync(null, "-5", null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithExistingReleaseId_ResolvesToReleaseSnapshot()
    {
        var repo = new FakeReleaseRepository();
        var releaseId = new ReleaseId(1);
        var snapshotId = new SnapshotId(100);
        repo.Add(new Release(releaseId, snapshotId, "v1.0.0", "Release 1", DateTimeOffset.UtcNow));

        var resolver = new WebReadContextResolver(repo);

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
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);

        var result = await resolver.ResolveAsync(null, null, "999");

        Assert.False(result.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.ReleaseNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidReleaseId_FailsWithInvalidReadContext()
    {
        var repo = new FakeReleaseRepository();
        var resolver = new WebReadContextResolver(repo);

        var result = await resolver.ResolveAsync(null, null, "abc");

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
    }
}
