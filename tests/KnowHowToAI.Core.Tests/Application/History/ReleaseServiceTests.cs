using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.History;

[Trait("Category", "Unit")]
public sealed class ReleaseServiceTests
{
    private static readonly SnapshotId CommittedSnapshotId = new(17);
    private static readonly SnapshotId WorkingSnapshotId = new(18);
    private static readonly SnapshotId UnknownSnapshotId = new(999);
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateReleaseAsync_NameNullOrWhitespace_ReturnsReleaseNameRequired(string? name)
    {
        var harness = new ReleaseTestHarness();
        var service = harness.CreateService();

        var result = await service.CreateReleaseAsync(name!, CommittedSnapshotId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.ReleaseNameRequired, result.Error!.Code);
    }

    [Fact]
    public async Task CreateReleaseAsync_SnapshotNotFound_ReturnsSnapshotNotFound()
    {
        var harness = new ReleaseTestHarness();
        var service = harness.CreateService();

        var result = await service.CreateReleaseAsync("v1.0", UnknownSnapshotId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.SnapshotNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CreateReleaseAsync_SnapshotNotCommitted_ReturnsSnapshotNotCommitted()
    {
        var harness = new ReleaseTestHarness();
        var service = harness.CreateService();

        var result = await service.CreateReleaseAsync("v1.0", WorkingSnapshotId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.SnapshotNotCommitted, result.Error!.Code);
    }

    [Fact]
    public async Task CreateReleaseAsync_RepositoryRejectsDuplicateName_ForwardsReleaseNameConflict()
    {
        var expectedError = new DomainError(ReleaseErrorCodes.ReleaseNameConflict, "Bereits vergeben.");
        var harness = new ReleaseTestHarness();
        harness.ReleaseRepo.CreateError = expectedError;
        var service = harness.CreateService();

        var result = await service.CreateReleaseAsync("v1.0", CommittedSnapshotId);

        Assert.False(result.IsSuccess);
        Assert.Same(expectedError, result.Error);
        Assert.Equal("v1.0", harness.ReleaseRepo.LastRequest?.Name);
    }

    [Fact]
    public async Task CreateReleaseAsync_CleanCommittedSnapshot_ReturnsReleaseAndEmptyFindings()
    {
        var harness = new ReleaseTestHarness();
        harness.SetupCleanSnapshot(CommittedSnapshotId);
        var service = harness.CreateService();

        var result = await service.CreateReleaseAsync("v1.0", CommittedSnapshotId, "Initial Release");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("v1.0", result.Value.Release.Name);
        Assert.Equal("Initial Release", result.Value.Release.Description);
        Assert.Equal(CommittedSnapshotId, result.Value.Release.SnapshotId);
        Assert.Empty(result.Value.Findings);
    }

    [Fact]
    public async Task CreateReleaseAsync_SnapshotWithStaleContent_SucceedsAndReturnsTransparentFindings()
    {
        var harness = new ReleaseTestHarness();
        harness.SetupSnapshotWithStaleContent(CommittedSnapshotId);
        var service = harness.CreateService();

        var result = await service.CreateReleaseAsync("v1.1", CommittedSnapshotId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("v1.1", result.Value.Release.Name);
        Assert.NotEmpty(result.Value.Findings);
        Assert.Contains(result.Value.Findings, f => f.Code == QualityWarningCodes.StaleDerivedContent);
    }

    [Fact]
    public async Task ListReleasesAsync_NoCursor_ReturnsFirstPageWithNextCursor()
    {
        var harness = new ReleaseTestHarness();
        harness.SeedReleases(count: 5);
        var service = harness.CreateService(defaultPageSize: 2);

        var result = await service.ListReleasesAsync(limit: 2, cursor: null);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.NotNull(result.Value.NextCursor);

        var decodedCursor = ReleaseCursor.TryDecode(result.Value.NextCursor);
        Assert.NotNull(decodedCursor);
        Assert.Equal(result.Value.Items[^1].ReleaseId.Value, decodedCursor.AfterReleaseId);
    }

    [Fact]
    public async Task ListReleasesAsync_WithCursor_ReturnsNextPage()
    {
        var harness = new ReleaseTestHarness();
        harness.SeedReleases(count: 5);
        var service = harness.CreateService(defaultPageSize: 2);

        var firstPage = await service.ListReleasesAsync(limit: 2, cursor: null);
        var secondPage = await service.ListReleasesAsync(limit: 2, cursor: firstPage.Value!.NextCursor);

        Assert.True(secondPage.IsSuccess);
        Assert.Equal(2, secondPage.Value!.Items.Count);
        Assert.Equal(3, secondPage.Value.Items[0].ReleaseId.Value);
        Assert.Equal(4, secondPage.Value.Items[1].ReleaseId.Value);
        Assert.NotNull(secondPage.Value.NextCursor);
    }

    [Fact]
    public async Task ListReleasesAsync_InvalidCursor_ReturnsInvalidCursorError()
    {
        var harness = new ReleaseTestHarness();
        var service = harness.CreateService();

        var result = await service.ListReleasesAsync(limit: 10, cursor: "invalid-cursor-value");

        Assert.False(result.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.InvalidCursor, result.Error!.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListReleasesAsync_NonPositiveLimit_UsesConfiguredDefault(int limit)
    {
        var harness = new ReleaseTestHarness();
        harness.SeedReleases(count: 5);
        var service = harness.CreateService(defaultPageSize: 2);

        var result = await service.ListReleasesAsync(limit, cursor: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.NotNull(result.Value.NextCursor);
    }

    // ── Test Harness (düner Wrapper über die gemeinsamen TestSupport-Fakes) ──

    private sealed class ReleaseTestHarness
    {
        private readonly InMemoryKnowledgeStore _store = new();

        public InMemoryReleaseMutationRepository ReleaseRepo { get; } = new();
        public List<Node> Nodes => _store.Nodes;
        public List<Audience> Audiences => _store.Audiences;
        public List<AudienceResolution> Resolutions => _store.Resolutions;
        public List<NodeContent> Contents => _store.Contents;
        public List<ContentDependency> Dependencies => _store.Dependencies;

        public ReleaseTestHarness()
        {
            _store.Snapshots.Add(new Snapshot(CommittedSnapshotId, null, SnapshotState.Committed, Now, Now));
            _store.Snapshots.Add(new Snapshot(WorkingSnapshotId, null, SnapshotState.Working, Now, null));
        }

        public void SetupCleanSnapshot(SnapshotId snapshotId)
        {
            var audienceId = new AudienceId("default");
            Audiences.Add(new Audience(snapshotId, audienceId, "Default", "Default audience", false));
            Resolutions.Add(new AudienceResolution(snapshotId, audienceId, audienceId, 1));

            var nodeId = new NodeId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            Nodes.Add(new Node(snapshotId, nodeId, null, "Root", "Root desc", 1, false));
            Contents.Add(new NodeContent(snapshotId, nodeId, audienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Hello clean content", false));
        }

        public void SetupSnapshotWithStaleContent(SnapshotId snapshotId)
        {
            SetupCleanSnapshot(snapshotId);

            var derivedAudienceId = new AudienceId("derived");
            Audiences.Add(new Audience(snapshotId, derivedAudienceId, "Derived", "Derived audience", false));
            Resolutions.Add(new AudienceResolution(snapshotId, derivedAudienceId, derivedAudienceId, 1));

            var sourceRev = new ContentRevisionId(Guid.NewGuid());
            var olderRev = new ContentRevisionId(Guid.NewGuid());
            var nodeId = new NodeId(Guid.Parse("11111111-1111-1111-1111-111111111111"));

            Contents.Clear();
            Contents.Add(new NodeContent(snapshotId, nodeId, new AudienceId("default"), sourceRev, ContentMode.Independent, "Source content", false));
            Contents.Add(new NodeContent(snapshotId, nodeId, derivedAudienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Derived content", false));

            // Dependency points to olderRev which does not match current sourceRev -> StaleDerivedContent
            Dependencies.Add(new ContentDependency(snapshotId, nodeId, derivedAudienceId, nodeId, new AudienceId("default"), olderRev));
        }

        public void SeedReleases(int count)
        {
            for (var i = 1; i <= count; i++)
            {
                ReleaseRepo.ExistingReleases.Add(new Release(
                    new ReleaseId(i),
                    CommittedSnapshotId,
                    $"v{i}.0",
                    $"Release {i}",
                    Now.AddDays(i)));
            }
        }

        public ReleaseService CreateService(int defaultPageSize = 10) => new(
            new SnapshotReadRepositories(
                new InMemorySnapshotRepository(_store),
                new InMemoryTransactionRepository(_store),
                new InMemoryHierarchyRepository(_store),
                new InMemoryContentRepository(_store),
                new InMemoryAudienceRepository(_store),
                new InMemoryDependencyRepository(_store)),
            ReleaseRepo,
            new FixedClock(Now),
            new RetrievalPolicy
            {
                DefaultPageSize = defaultPageSize,
                MaximumPageSize = 100,
                SearchPageSize = 10,
                SearchMaximumPageSize = 100,
                SnippetMaximumCharacters = 100
            },
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 25,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            });
    }
}
