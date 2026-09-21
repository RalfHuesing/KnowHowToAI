using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Tools.History;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der History- und Release-Tools: dünne Delegation an
/// HistoryService und ReleaseService mit protokollkonformer Error-Struktur,
/// ID-Round-Trip und Snapshot-/Release-Metadaten. Keine SQL- oder Server-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpHistoryToolsTests
{
    private static readonly SnapshotId BaseSnapshotId = new(41);
    private static readonly SnapshotId TargetSnapshotId = new(42);
    private static readonly TransactionId SampleTransactionId =
        new(Guid.Parse("0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90"));

    // ── get_snapshot ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSnapshot_CommittedSnapshot_ReturnsSnapshotMetadataWithIdRoundTrip()
    {
        var repos = RepositoriesWithSnapshots(CommittedSnapshot(TargetSnapshotId, BaseSnapshotId));
        var tools = CreateTools(repos);

        var envelope = await tools.GetSnapshot(TargetSnapshotId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal(TargetSnapshotId.ToString(), envelope.Data!.SnapshotId);
        Assert.Equal(BaseSnapshotId.ToString(), envelope.Data.BaseSnapshotId);
        Assert.Equal("Committed", envelope.Data.State);
        Assert.NotNull(envelope.Data.CommittedAtUtc);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData("abc-xyz")]
    public async Task GetSnapshot_MalformedSnapshotId_ReturnsSnapshotNotFoundEnvelope(string rawId)
    {
        var tools = CreateTools(RepositoriesWithSnapshots());

        var envelope = await tools.GetSnapshot(rawId);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, envelope.Code);
        Assert.Equal(rawId, envelope.Details![HistoryErrorCodes.SnapshotIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task GetSnapshot_UnknownSnapshotId_ReturnsSnapshotNotFoundEnvelope()
    {
        var tools = CreateTools(RepositoriesWithSnapshots());

        var envelope = await tools.GetSnapshot(TargetSnapshotId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, envelope.Code);
        Assert.Equal(TargetSnapshotId.ToString(), envelope.Details![HistoryErrorCodes.SnapshotIdDetail]);
        Assert.Null(envelope.Data);
    }

    // ── compare_snapshots ─────────────────────────────────────────────────────

    [Fact]
    public async Task CompareSnapshots_TwoCommittedSnapshots_ReturnsDiffWithStableIds()
    {
        var repos = RepositoriesWithSnapshots(
            CommittedSnapshot(BaseSnapshotId, null),
            CommittedSnapshot(TargetSnapshotId, BaseSnapshotId));
        var tools = CreateTools(repos);

        var envelope = await tools.CompareSnapshots(
            BaseSnapshotId.ToString(), TargetSnapshotId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal(BaseSnapshotId.ToString(), envelope.Data!.BaseSnapshotId);
        Assert.Equal(TargetSnapshotId.ToString(), envelope.Data.TargetSnapshotId);
        Assert.NotNull(envelope.Data.Items);
    }

    [Theory]
    [InlineData("bad-base", "42")]
    [InlineData("41", "bad-target")]
    public async Task CompareSnapshots_MalformedSnapshotId_ReturnsSnapshotNotFoundEnvelope(
        string rawBase, string rawTarget)
    {
        var tools = CreateTools(RepositoriesWithSnapshots());

        var envelope = await tools.CompareSnapshots(rawBase, rawTarget);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, envelope.Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task CompareSnapshots_WorkingSnapshot_ReturnsSnapshotNotCommittedEnvelope()
    {
        var repos = RepositoriesWithSnapshots(
            WorkingSnapshot(BaseSnapshotId),
            CommittedSnapshot(TargetSnapshotId, BaseSnapshotId));
        var tools = CreateTools(repos);

        var envelope = await tools.CompareSnapshots(
            BaseSnapshotId.ToString(), TargetSnapshotId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotCommitted, envelope.Code);
    }

    // ── get_transaction_changes ───────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionChanges_OpenTransaction_ReturnsDiffWithTransactionState()
    {
        var repos = RepositoriesWithSnapshots(
            CommittedSnapshot(BaseSnapshotId, null),
            WorkingSnapshot(TargetSnapshotId, BaseSnapshotId));
        var reposWithTransaction = repos with
        {
            Transactions = new ScriptedTransactionRepository(OpenTransaction())
        };
        var tools = CreateTools(reposWithTransaction);

        var envelope = await tools.GetTransactionChanges(SampleTransactionId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal(SampleTransactionId.ToString(), envelope.Data!.TransactionId);
        Assert.Equal("Open", envelope.Data.State);
        Assert.Equal(BaseSnapshotId.ToString(), envelope.Data.BaseSnapshotId);
        Assert.Equal(TargetSnapshotId.ToString(), envelope.Data.WorkingSnapshotId);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("17")]
    public async Task GetTransactionChanges_MalformedTransactionId_ReturnsTransactionNotFoundEnvelope(string rawId)
    {
        var tools = CreateTools(RepositoriesWithSnapshots());

        var envelope = await tools.GetTransactionChanges(rawId);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HistoryErrorCodes.TransactionNotFound, envelope.Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task GetTransactionChanges_UnknownTransaction_ReturnsTransactionNotFoundEnvelope()
    {
        var tools = CreateTools(RepositoriesWithSnapshots());

        var envelope = await tools.GetTransactionChanges(SampleTransactionId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HistoryErrorCodes.TransactionNotFound, envelope.Code);
        Assert.Equal(SampleTransactionId.ToString(), envelope.Details![HistoryErrorCodes.TransactionIdDetail]);
    }

    // ── create_release ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRelease_ValidSnapshotAndName_ReturnsReleaseDataWithIdRoundTrip()
    {
        var repos = RepositoriesWithSnapshots(CommittedSnapshot(TargetSnapshotId, BaseSnapshotId));
        var releaseRepository = new ScriptedReleaseMutationRepository();
        var tools = CreateTools(repos, releaseRepository);

        var envelope = await tools.CreateRelease("v1.0", TargetSnapshotId.ToString(), "Erstes Release");

        Assert.True(envelope.IsSuccess);
        Assert.Equal("v1.0", envelope.Data!.Name);
        Assert.Equal(TargetSnapshotId.ToString(), envelope.Data.SnapshotId);
        Assert.Equal("Erstes Release", envelope.Data.Description);
    }

    [Theory]
    [InlineData("bad-id")]
    [InlineData("")]
    [InlineData("abc")]
    public async Task CreateRelease_MalformedSnapshotId_ReturnsSnapshotNotFoundEnvelope(string rawSnapshotId)
    {
        var tools = CreateTools(RepositoriesWithSnapshots());

        var envelope = await tools.CreateRelease("v1.0", rawSnapshotId);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.SnapshotNotFound, envelope.Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task CreateRelease_WorkingSnapshot_ReturnsSnapshotNotCommittedEnvelope()
    {
        var repos = RepositoriesWithSnapshots(WorkingSnapshot(TargetSnapshotId));
        var tools = CreateTools(repos);

        var envelope = await tools.CreateRelease("v1.0", TargetSnapshotId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.SnapshotNotCommitted, envelope.Code);
    }

    [Fact]
    public async Task CreateRelease_NameConflict_ReturnsReleaseNameConflictEnvelope()
    {
        var repos = RepositoriesWithSnapshots(CommittedSnapshot(TargetSnapshotId, BaseSnapshotId));
        var releaseRepository = new ScriptedReleaseMutationRepository
        {
            CreateResult = Result<Release>.Failure(new DomainError(
                ReleaseErrorCodes.ReleaseNameConflict,
                "Der Release-Name ist bereits vergeben.",
                new Dictionary<string, string> { [ReleaseErrorCodes.ReleaseNameDetail] = "v1.0" }))
        };
        var tools = CreateTools(repos, releaseRepository);

        var envelope = await tools.CreateRelease("v1.0", TargetSnapshotId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.ReleaseNameConflict, envelope.Code);
        Assert.Equal("v1.0", envelope.Details![ReleaseErrorCodes.ReleaseNameDetail]);
        Assert.Null(envelope.Data);
    }

    // ── list_releases ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListReleases_WithEntries_ReturnsPaginatedReleaseList()
    {
        var repos = RepositoriesWithSnapshots();
        var releaseRepository = new ScriptedReleaseMutationRepository
        {
            ListResult = [SampleRelease("v1.0"), SampleRelease("v2.0")]
        };
        var tools = CreateTools(repos, releaseRepository);

        var envelope = await tools.ListReleases();

        Assert.True(envelope.IsSuccess);
        Assert.Equal(2, envelope.Data!.Items.Count);
        Assert.Equal("v1.0", envelope.Data.Items[0].Name);
        Assert.Equal("v2.0", envelope.Data.Items[1].Name);
        Assert.Null(envelope.Data.NextCursor);
    }

    [Fact]
    public async Task ListReleases_EmptyStore_ReturnsEmptyPageWithoutCursor()
    {
        var tools = CreateTools(RepositoriesWithSnapshots());

        var envelope = await tools.ListReleases();

        Assert.True(envelope.IsSuccess);
        Assert.Empty(envelope.Data!.Items);
        Assert.Null(envelope.Data.NextCursor);
    }

    // ── JSON-Serialisierung der History-DTOs ──────────────────────────────────

    [Fact]
    public async Task GetSnapshot_Response_SerializesWithStableCamelCaseFieldNames()
    {
        var repos = RepositoriesWithSnapshots(CommittedSnapshot(TargetSnapshotId, BaseSnapshotId));
        var tools = CreateTools(repos);

        var json = JsonSerializer.Serialize(await tools.GetSnapshot(TargetSnapshotId.ToString()));

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(TargetSnapshotId.ToString(), data.GetProperty("snapshotId").GetString());
        Assert.Equal("Committed", data.GetProperty("state").GetString());
        Assert.True(data.TryGetProperty("baseSnapshotId", out _));
        Assert.True(data.TryGetProperty("committedAtUtc", out _));
    }

    [Fact]
    public async Task CreateRelease_Response_SerializesReleaseIdAsStringWithoutReformatting()
    {
        var repos = RepositoriesWithSnapshots(CommittedSnapshot(TargetSnapshotId, BaseSnapshotId));
        var releaseRepository = new ScriptedReleaseMutationRepository();
        var tools = CreateTools(repos, releaseRepository);

        var json = JsonSerializer.Serialize(await tools.CreateRelease("v1.0", TargetSnapshotId.ToString()));

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Success", doc.RootElement.GetProperty("code").GetString());
        Assert.True(doc.RootElement.GetProperty("data").TryGetProperty("releaseId", out var releaseId));
        Assert.NotEmpty(releaseId.GetString()!);
    }

    // ── Fabrik-Methoden ───────────────────────────────────────────────────────

    private static HistoryTools CreateTools(
        SnapshotReadRepositories repos,
        IReleaseMutationRepository? releaseRepository = null) =>
        new(
            new HistoryService(repos, RetrievalPolicy()),
            new ReleaseService(
                repos,
                releaseRepository ?? new ScriptedReleaseMutationRepository(),
                new FixedClock(),
                RetrievalPolicy(),
                ValidationPolicy()),
            RetrievalPolicy());

    private static RetrievalPolicy RetrievalPolicy() => new()
    {
        DefaultPageSize = 20,
        MaximumPageSize = 100,
        SearchPageSize = 10,
        SearchMaximumPageSize = 50,
        SnippetMaximumCharacters = 300
    };

    private static ValidationPolicy ValidationPolicy() => new()
    {
        ContentSizeWarningBytes = 4096,
        ChildCountWarning = 25,
        HierarchyDepthWarning = 8,
        PossibleEmbeddedHeadingWarning = true
    };

    private static SnapshotReadRepositories RepositoriesWithSnapshots(params Snapshot[] snapshots) =>
        new(
            new ScriptedSnapshotRepository(snapshots),
            new ScriptedTransactionRepository(null),
            new EmptyHierarchyRepository(),
            new EmptyContentRepository(),
            new EmptyAudienceRepository(),
            new EmptyDependencyRepository());

    private static Snapshot CommittedSnapshot(SnapshotId id, SnapshotId? baseId) =>
        new(
            id,
            baseId,
            SnapshotState.Committed,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            new SnapshotCommitMetadata(SampleTransactionId, "MCP actor", "MCP client", "MCP purpose", "MCP commit"));

    private static Snapshot WorkingSnapshot(SnapshotId id, SnapshotId? baseId = null) =>
        new(id, baseId, SnapshotState.Working, DateTimeOffset.UnixEpoch, null);

    private static KnowledgeTransaction OpenTransaction() => new(
        SampleTransactionId,
        BaseSnapshotId,
        TargetSnapshotId,
        TransactionState.Open,
        ChangeVersion: 0,
        CreatedAtUtc: DateTimeOffset.UnixEpoch,
        CommittedAtUtc: null,
        Purpose: null,
        Actor: null,
        Client: null,
        CommitMessage: null);

    private static Release SampleRelease(string name) => new(
        new ReleaseId(1),
        TargetSnapshotId,
        name,
        null,
        DateTimeOffset.UnixEpoch);

    // ── Scripted Test-Doubles ─────────────────────────────────────────────────

    private sealed class ScriptedSnapshotRepository(Snapshot[] snapshots) : ISnapshotRepository
    {
        private readonly Dictionary<long, Snapshot> _byId =
            snapshots.ToDictionary(s => s.SnapshotId.Value);

        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.GetValueOrDefault(snapshotId.Value));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _byId.Values.LastOrDefault(s => s.State == SnapshotState.Committed)
                ?? new Snapshot(new SnapshotId(1), null, SnapshotState.Committed, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));

        public Task<IReadOnlyList<Snapshot>> ListCommittedAsync(
            int limit,
            SnapshotId? beforeSnapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Snapshot>>(_byId.Values
                .Where(snapshot => snapshot.State == SnapshotState.Committed)
                .Where(snapshot => beforeSnapshotId is null || snapshot.SnapshotId.Value < beforeSnapshotId.Value.Value)
                .OrderByDescending(snapshot => snapshot.SnapshotId.Value)
                .Take(limit)
                .ToArray());
    }

    private sealed class ScriptedTransactionRepository(KnowledgeTransaction? transaction) : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(
            BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<KnowledgeTransaction?> FindAsync(
            TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(transaction?.TransactionId == transactionId ? transaction : null);

        public Task<CommitTransactionResult> CommitAsync(
            CommitTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(
            TransactionId transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ScriptedReleaseMutationRepository : IReleaseMutationRepository
    {
        public Result<Release>? CreateResult { get; init; }

        public IReadOnlyList<Release> ListResult { get; init; } = [];

        public Task<Result<Release>> CreateAsync(
            CreateReleaseRecord request, CancellationToken cancellationToken = default)
        {
            if (CreateResult is not null)
                return Task.FromResult(CreateResult);

            return Task.FromResult(Result<Release>.Success(new Release(
                new ReleaseId(1),
                request.SnapshotId,
                request.Name,
                request.Description,
                DateTimeOffset.UnixEpoch)));
        }

        public Task<IReadOnlyList<Release>> ListAsync(
            int limit, long? afterReleaseId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ListResult);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    // ── Minimale No-op-Repository-Implementierungen ───────────────────────────

    private sealed class EmptyHierarchyRepository : IHierarchyRepository
    {
        public Task<IReadOnlyList<Node>> ListBySnapshotAsync(
            SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Node>>([]);
    }

    private sealed class EmptyContentRepository : IContentRepository
    {
        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(
            SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NodeContent>>([]);
    }

    private sealed class EmptyAudienceRepository : IAudienceRepository
    {
        public Task<IReadOnlyList<Audience>> ListBySnapshotAsync(
            SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Audience>>([]);

        public Task<IReadOnlyList<AudienceResolution>> ListResolutionsBySnapshotAsync(
            SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AudienceResolution>>([]);
    }

    private sealed class EmptyDependencyRepository : IDependencyRepository
    {
        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(
            SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContentDependency>>([]);
    }
}
