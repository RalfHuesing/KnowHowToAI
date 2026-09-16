using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;

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

    private sealed class ReleaseTestHarness
    {
        public InMemoryReleaseMutationRepository ReleaseRepo { get; } = new();
        public List<Snapshot> Snapshots { get; } =
        [
            new Snapshot(CommittedSnapshotId, null, SnapshotState.Committed, Now, Now),
            new Snapshot(WorkingSnapshotId, null, SnapshotState.Working, Now, null)
        ];
        public List<Node> Nodes { get; } = [];
        public List<Role> Roles { get; } = [];
        public List<RoleResolution> Resolutions { get; } = [];
        public List<NodeContent> Contents { get; } = [];
        public List<ContentDependency> Dependencies { get; } = [];

        public void SetupCleanSnapshot(SnapshotId snapshotId)
        {
            var roleId = new RoleId("default");
            Roles.Add(new Role(snapshotId, roleId, "Default", "Default role", false));
            Resolutions.Add(new RoleResolution(snapshotId, roleId, roleId, 1));

            var nodeId = new NodeId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            Nodes.Add(new Node(snapshotId, nodeId, null, "Root", "Root desc", 1, false));
            Contents.Add(new NodeContent(snapshotId, nodeId, roleId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Hello clean content", false));
        }

        public void SetupSnapshotWithStaleContent(SnapshotId snapshotId)
        {
            SetupCleanSnapshot(snapshotId);

            var derivedRoleId = new RoleId("derived");
            Roles.Add(new Role(snapshotId, derivedRoleId, "Derived", "Derived role", false));
            Resolutions.Add(new RoleResolution(snapshotId, derivedRoleId, derivedRoleId, 1));

            var sourceRev = new ContentRevisionId(Guid.NewGuid());
            var olderRev = new ContentRevisionId(Guid.NewGuid());
            var nodeId = new NodeId(Guid.Parse("11111111-1111-1111-1111-111111111111"));

            Contents.Clear();
            Contents.Add(new NodeContent(snapshotId, nodeId, new RoleId("default"), sourceRev, ContentMode.Independent, "Source content", false));
            Contents.Add(new NodeContent(snapshotId, nodeId, derivedRoleId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Derived content", false));

            // Dependency points to olderRev which does not match current sourceRev -> StaleDerivedContent
            Dependencies.Add(new ContentDependency(snapshotId, nodeId, derivedRoleId, nodeId, new RoleId("default"), olderRev));
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
            new HistoryRepositories(
                new DelegatingSnapshotRepository(Snapshots),
                new ThrowingTransactionRepository(),
                new DelegatingHierarchyRepository(Nodes),
                new DelegatingContentRepository(Contents),
                new DelegatingRoleRepository(Roles, Resolutions),
                new DelegatingDependencyRepository(Dependencies)),
            ReleaseRepo,
            new ConstantClock(Now),
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

    private sealed class InMemoryReleaseMutationRepository : IReleaseMutationRepository
    {
        public List<Release> ExistingReleases { get; } = [];
        public CreateReleaseRecord? LastRequest { get; private set; }
        public DomainError? CreateError { get; set; }

        public Task<Result<Release>> CreateAsync(CreateReleaseRecord request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            if (CreateError is not null)
                return Task.FromResult(Result<Release>.Failure(CreateError));

            var newRelease = new Release(
                new ReleaseId(ExistingReleases.Count + 1),
                request.SnapshotId,
                request.Name,
                request.Description,
                request.CreatedAtUtc);

            ExistingReleases.Add(newRelease);
            return Task.FromResult(Result<Release>.Success(newRelease));
        }

        public Task<IReadOnlyList<Release>> ListAsync(int limit, long? afterReleaseId, CancellationToken cancellationToken = default)
        {
            var query = ExistingReleases.AsEnumerable();
            if (afterReleaseId.HasValue)
                query = query.Where(r => r.ReleaseId.Value > afterReleaseId.Value);

            var items = query.OrderBy(r => r.ReleaseId.Value).Take(limit).ToArray();
            return Task.FromResult<IReadOnlyList<Release>>(items);
        }
    }

    private sealed class ConstantClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class DelegatingSnapshotRepository(List<Snapshot> snapshots) : ISnapshotRepository
    {
        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshots.FirstOrDefault(s => s.SnapshotId == snapshotId));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshots.First(s => s.State == SnapshotState.Committed));
    }

    private sealed class ThrowingTransactionRepository : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(KnowHowToAI.Core.Application.Transactions.BeginTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KnowHowToAI.Core.Application.Transactions.CommitTransactionResult> CommitAsync(KnowHowToAI.Core.Application.Transactions.CommitTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DelegatingHierarchyRepository(List<Node> nodes) : IHierarchyRepository
    {
        public Task<IReadOnlyList<Node>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Node>>(nodes.Where(n => n.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DelegatingRoleRepository(List<Role> roles, List<RoleResolution> resolutions) : IRoleRepository
    {
        public Task<IReadOnlyList<Role>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>(roles.Where(r => r.SnapshotId == snapshotId).ToArray());

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleResolution>>(resolutions.Where(r => r.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DelegatingContentRepository(List<NodeContent> contents) : IContentRepository
    {
        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NodeContent>>(contents.Where(c => c.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DelegatingDependencyRepository(List<ContentDependency> dependencies) : IDependencyRepository
    {
        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContentDependency>>(dependencies.Where(d => d.SnapshotId == snapshotId).ToArray());
    }
}
