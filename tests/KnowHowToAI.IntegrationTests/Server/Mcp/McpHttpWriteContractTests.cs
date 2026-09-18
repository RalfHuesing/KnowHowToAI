using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;
using KnowHowToAI.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Client;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// MCP-Vertragsregression über die HTTP-Boundary — schreibender Teil der
/// Vertragsmatrix (offizieller SDK-McpClient gegen reales Kestrel `/mcp`):
///
/// - Begin/Commit/Discard: BeginAndCommitWorkflow_ReturnsCommittedTransactionState (Commit) und
///   McpHttpTransportTests.StreamableHttpClient_CompletesBeginMutationAndDiscardWorkflow (Begin/Discard).
/// - Node-, Rollen- und Contentmutationen: CreateNode_CreatesRootNodeInWorkingSnapshotOverHttp,
///   ReplaceContent_WritesExplicitRoleContent; create_role über den Writepfad in
///   McpHttpTransportTests.StreamableHttpClient_CompletesBeginMutationAndDiscardWorkflow.
/// - Historie/Diff/Release (inkl. Release-Read-Context, da Read-Tools keinen Release-Selektor
///   annehmen): HistoryTools_ReturnSnapshotDiffAndReleaseContracts (weitere Historienvarianten: McpHistoryToolsTests).
/// - Warnungen: ReplaceContent_StandaloneTitleParagraph_SucceedsWithPossibleEmbeddedHeadingWarning
///   (weitere Warncodes: QualityWarningEvaluatorTests, McpContentMutationToolsTests, McpRetrievalToolsTests).
/// - Stabile Fehlercodes: Zustand → TransactionClosed (GetRoot_WithClosedTransaction_ReturnsStableTransactionClosed);
///   Konflikt → SnapshotConflict (CommitTransaction_AgainstAdvancedCurrentSnapshot_ReturnsStableSnapshotConflict).
/// </summary>
[Trait("Category", "Integration")]
public sealed class McpHttpWriteContractTests
{
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly TransactionId TransactionId =
        new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));

    private static readonly SnapshotId WorkingSnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000000"));
    private static readonly NodeId GeneratedNodeId = new(new Guid(10, 0, 0, new byte[8]));
    private static readonly ContentRevisionId GeneratedRevisionId = new(new Guid(20, 0, 0, new byte[8]));
    private static readonly RoleId RoleDeveloper = new("Developer");

    // ── Begin/Commit/Discard ──────────────────────────────────────────────────

    [Fact]
    public async Task BeginAndCommitWorkflow_ReturnsCommittedTransactionState()
    {
        var transaction = new ScriptedTransactionRepository(CommitResponse: CommittedCommitResponse("Freigabe"));
        await using var host = await StartWithTransactionServiceAsync(transaction);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var begin = await McpHttpToolCalls.CallAsync(client, "begin_transaction");
        using var commit = await McpHttpToolCalls.CallAsync(client, "commit_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = begin.RootElement.GetProperty("data").GetProperty("transactionId").GetString(),
            ["commitMessage"] = "Freigabe"
        });

        var beginData = begin.RootElement.GetProperty("data");
        Assert.Equal("Success", begin.RootElement.GetProperty("code").GetString());
        Assert.Equal("Open", beginData.GetProperty("state").GetString());
        Assert.Equal("41", beginData.GetProperty("baseSnapshotId").GetString());
        Assert.Equal("42", beginData.GetProperty("workingSnapshotId").GetString());

        var commitData = commit.RootElement.GetProperty("data");
        Assert.Equal("Success", commit.RootElement.GetProperty("code").GetString());
        Assert.Equal("Committed", commitData.GetProperty("state").GetString());
        Assert.Equal("Freigabe", commitData.GetProperty("commitMessage").GetString());
        Assert.Equal("NodeTooLarge", Assert.Single(commit.RootElement.GetProperty("warnings").EnumerateArray())
            .GetProperty("code").GetString());
        Assert.Equal(TransactionId, Assert.Single(transaction.CommitRequests).TransactionId);
    }

    // ── Node- und Contentmutationen ───────────────────────────────────────────

    [Fact]
    public async Task CreateNode_CreatesRootNodeInWorkingSnapshotOverHttp()
    {
        var nodes = new InMemoryNodeMutationRepository(new WorkingNodeMutationState(WorkingSnapshotId, [], [], [], []));
        await using var host = await StartWithMutationServicesAsync(nodes);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "create_node", new Dictionary<string, object?>
        {
            ["transactionId"] = TransactionId.ToString(),
            ["title"] = "Hauptkapitel"
        });

        var data = envelope.RootElement.GetProperty("data");
        Assert.Equal("Success", envelope.RootElement.GetProperty("code").GetString());
        Assert.Equal(GeneratedNodeId.ToString(), data.GetProperty("nodeId").GetString());
        Assert.Equal("Hauptkapitel", data.GetProperty("title").GetString());
        Assert.Equal(WorkingSnapshotId.ToString(), data.GetProperty("snapshotId").GetString());
        Assert.Equal(1, data.GetProperty("changeVersion").GetInt32());
        Assert.Contains(GeneratedNodeId.ToString(), data.GetProperty("affectedNodeIds").EnumerateArray()
            .Select(id => id.GetString()).ToArray());
    }

    [Fact]
    public async Task ReplaceContent_WritesExplicitRoleContent()
    {
        var content = ContentStateWithNodeAndRole();
        await using var host = await StartWithMutationServicesAsync(content: content);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "replace_content", new Dictionary<string, object?>
        {
            ["transactionId"] = TransactionId.ToString(),
            ["nodeId"] = RootNodeId.ToString(),
            ["roleId"] = "Developer",
            ["contentMode"] = "Independent",
            ["contentMd"] = "Inhalt ohne Struktur."
        });

        var data = envelope.RootElement.GetProperty("data");
        Assert.Equal("Success", envelope.RootElement.GetProperty("code").GetString());
        Assert.Equal("Developer", data.GetProperty("roleId").GetString());
        Assert.Equal("Independent", data.GetProperty("contentMode").GetString());
        Assert.Equal("Current", data.GetProperty("freshness").GetString());
        Assert.Equal(WorkingSnapshotId.ToString(), data.GetProperty("snapshotId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("contentRevisionId").GetString()));
    }

    // ── Warnungen ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReplaceContent_StandaloneTitleParagraph_SucceedsWithPossibleEmbeddedHeadingWarning()
    {
        var content = ContentStateWithNodeAndRole();
        await using var host = await StartWithMutationServicesAsync(content: content);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "replace_content", new Dictionary<string, object?>
        {
            ["transactionId"] = TransactionId.ToString(),
            ["nodeId"] = RootNodeId.ToString(),
            ["roleId"] = "Developer",
            ["contentMode"] = "Independent",
            ["contentMd"] = "Titel"
        });

        Assert.Equal("Success", envelope.RootElement.GetProperty("code").GetString());
        var warning = Assert.Single(envelope.RootElement.GetProperty("warnings").EnumerateArray());
        Assert.Equal("PossibleEmbeddedHeading", warning.GetProperty("code").GetString());
        Assert.Equal(RootNodeId.ToString(),
            envelope.RootElement.GetProperty("data").GetProperty("nodeId").GetString());
        Assert.Equal("Titel", Assert.Single(content.State.Contents).ContentMd);
    }

    // ── Historie/Diff/Release ─────────────────────────────────────────────────

    [Fact]
    public async Task HistoryTools_ReturnSnapshotDiffAndReleaseContracts()
    {
        var harness = new NavigationTestHarness(new SnapshotId(41));
        harness.AddHistoricalSnapshot(new Snapshot(
            new SnapshotId(42), new SnapshotId(41), SnapshotState.Committed, FixedTimestamp, FixedTimestamp));
        var releases = new ScriptedReleaseMutationRepository();
        await using var host = await StartWithHistoryServicesAsync(harness.CreateRepositories(), releases);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var snapshot = await McpHttpToolCalls.CallAsync(client, "get_snapshot", new Dictionary<string, object?> { ["snapshotId"] = "42" });
        using var diff = await McpHttpToolCalls.CallAsync(client, "compare_snapshots", new Dictionary<string, object?>
        {
            ["baseSnapshotId"] = "41",
            ["targetSnapshotId"] = "42"
        });
        using var release = await McpHttpToolCalls.CallAsync(client, "create_release", new Dictionary<string, object?>
        {
            ["name"] = "v1.0",
            ["snapshotId"] = "42",
            ["description"] = "Erstes Release"
        });
        using var releaseList = await McpHttpToolCalls.CallAsync(client, "list_releases");

        var snapshotData = snapshot.RootElement.GetProperty("data");
        Assert.Equal("Success", snapshot.RootElement.GetProperty("code").GetString());
        Assert.Equal("42", snapshotData.GetProperty("snapshotId").GetString());
        Assert.Equal("Committed", snapshotData.GetProperty("state").GetString());
        Assert.Equal("41", snapshotData.GetProperty("baseSnapshotId").GetString());

        var diffData = diff.RootElement.GetProperty("data");
        Assert.Equal("Success", diff.RootElement.GetProperty("code").GetString());
        Assert.Equal("41", diffData.GetProperty("baseSnapshotId").GetString());
        Assert.Equal("42", diffData.GetProperty("targetSnapshotId").GetString());
        Assert.True(diffData.TryGetProperty("items", out _));

        var releaseData = release.RootElement.GetProperty("data");
        Assert.Equal("Success", release.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(releaseData.GetProperty("releaseId").GetString()));
        Assert.Equal("v1.0", releaseData.GetProperty("name").GetString());
        Assert.Equal("42", releaseData.GetProperty("snapshotId").GetString());

        var releaseListData = releaseList.RootElement.GetProperty("data");
        Assert.Equal("Success", releaseList.RootElement.GetProperty("code").GetString());
        Assert.Equal("v1.0", releaseListData.GetProperty("items")[0].GetProperty("name").GetString());
        Assert.Equal("42", releaseListData.GetProperty("items")[0].GetProperty("snapshotId").GetString());
    }

    // ── Stabile Fehlercodes: Zustand und Konflikt ─────────────────────────────

    [Fact]
    public async Task GetRoot_WithClosedTransaction_ReturnsStableTransactionClosed()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        harness.SetTransaction(OpenTransaction() with { State = TransactionState.Committed });
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(harness.CreateService());
        });
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "get_root", new Dictionary<string, object?>
        {
            ["roleId"] = "Developer",
            ["transactionId"] = TransactionId.ToString()
        });

        Assert.Equal("TransactionClosed", envelope.RootElement.GetProperty("code").GetString());
        Assert.Equal(TransactionId.ToString(),
            envelope.RootElement.GetProperty("details").GetProperty("transactionId").GetString());
        Assert.False(envelope.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task CommitTransaction_AgainstAdvancedCurrentSnapshot_ReturnsStableSnapshotConflict()
    {
        var transaction = new ScriptedTransactionRepository(CommitResponse: ConflictingCommitResponse());
        await using var host = await StartWithTransactionServiceAsync(transaction);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "commit_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = TransactionId.ToString()
        });

        Assert.Equal("SnapshotConflict", envelope.RootElement.GetProperty("code").GetString());
        Assert.Equal("100", envelope.RootElement.GetProperty("details").GetProperty("baseSnapshotId").GetString());
        Assert.Equal("101", envelope.RootElement.GetProperty("details").GetProperty("currentSnapshotId").GetString());
        Assert.False(envelope.RootElement.TryGetProperty("data", out _));
    }

    // ── Hilfsfabriken und Test-Doubles ────────────────────────────────────────

    private static async Task<McpHttpHost> StartWithTransactionServiceAsync(ITransactionRepository transactionRepository) =>
        await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<TransactionService>();
            services.AddSingleton(new TransactionService(
                transactionRepository,
                new ValidatingWorkingSnapshotRepository(),
                new FixedIdentifierGenerator(),
                new ValidationPolicy
                {
                    ContentSizeWarningBytes = 4096,
                    ChildCountWarning = 30,
                    HierarchyDepthWarning = 10,
                    PossibleEmbeddedHeadingWarning = true
                }));
        });

    private static async Task<McpHttpHost> StartWithMutationServicesAsync(
        InMemoryNodeMutationRepository? nodes = null,
        InMemoryContentMutationRepository? content = null)
    {
        nodes ??= new InMemoryNodeMutationRepository(new WorkingNodeMutationState(WorkingSnapshotId, [], [], [], []));
        content ??= new InMemoryContentMutationRepository(
            new WorkingContentMutationState(WorkingSnapshotId, [], [], [], []));
        return await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NodeMutationApplicationService>();
            services.AddSingleton(new NodeMutationApplicationService(
                nodes,
                new NodeMutationService(new FixedIdentifierGenerator()),
                new ValidationPolicy
                {
                    ContentSizeWarningBytes = 4096,
                    ChildCountWarning = 30,
                    HierarchyDepthWarning = 10,
                    PossibleEmbeddedHeadingWarning = true
                }));
            services.RemoveAll<ContentMutationApplicationService>();
            services.AddSingleton(new ContentMutationApplicationService(
                content,
                new ContentMutationService(new ContentRevisionService(new FixedIdentifierGenerator())),
                new ValidationPolicy
                {
                    ContentSizeWarningBytes = 4096,
                    ChildCountWarning = 30,
                    HierarchyDepthWarning = 10,
                    PossibleEmbeddedHeadingWarning = true
                }));
        });
    }

    private static async Task<McpHttpHost> StartWithHistoryServicesAsync(
        SnapshotReadRepositories repositories,
        ScriptedReleaseMutationRepository releases) =>
        await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<HistoryService>();
            services.AddSingleton(new HistoryService(repositories, CreateRetrievalPolicy()));
            services.RemoveAll<ReleaseService>();
            services.AddSingleton(new ReleaseService(
                repositories,
                releases,
                new FixedClock(FixedTimestamp),
                CreateRetrievalPolicy(),
                new ValidationPolicy
                {
                    ContentSizeWarningBytes = 4096,
                    ChildCountWarning = 30,
                    HierarchyDepthWarning = 10,
                    PossibleEmbeddedHeadingWarning = true
                }));
        });

    private static RetrievalPolicy CreateRetrievalPolicy() => new()
    {
        DefaultPageSize = 5,
        MaximumPageSize = 50,
        SearchPageSize = 5,
        SearchMaximumPageSize = 25,
        SnippetMaximumCharacters = 80
    };

    private static CommitTransactionResult CommittedCommitResponse(string commitMessage) => new(
        OpenTransaction() with
        {
            State = TransactionState.Committed,
            ChangeVersion = 1,
            CommittedAtUtc = FixedTimestamp,
            CommitMessage = commitMessage
        },
        new TransactionValidationReport(
            [],
            [new DomainWarning("NodeTooLarge", "Content übersteigt die Warnschwelle.")],
            [],
            []),
        null);

    private static CommitTransactionResult ConflictingCommitResponse() => new(
        null,
        null,
        new DomainError(
            "SnapshotConflict",
            "Der Basis-Snapshot ist nicht mehr aktuell.",
            new Dictionary<string, string> { ["baseSnapshotId"] = "100", ["currentSnapshotId"] = "101" }));

    private static KnowledgeTransaction OpenTransaction() => new(
        TransactionId,
        new SnapshotId(41),
        WorkingSnapshotId,
        TransactionState.Open,
        ChangeVersion: 0,
        CreatedAtUtc: FixedTimestamp,
        CommittedAtUtc: null,
        Purpose: null,
        Actor: null,
        Client: null,
        CommitMessage: null);

    private static InMemoryContentMutationRepository ContentStateWithNodeAndRole() => new(
        new WorkingContentMutationState(
            WorkingSnapshotId,
            [new Node(WorkingSnapshotId, RootNodeId, null, "Titel", null, 0, false)],
            [new Role(WorkingSnapshotId, RoleDeveloper, "Developer", null, false)],
            [],
            []));

    /// <summary>
    /// Skriptbares Transaction-Repository-Double: liefert eine offene Transaction
    /// (Basis-Snapshot 41, Working Snapshot 42) und antwortet auf Commit mit dem
    /// konfigurierten Ergebnis.
    /// </summary>
    private sealed class ScriptedTransactionRepository(CommitTransactionResult CommitResponse) : ITransactionRepository
    {
        public List<CommitTransactionRequest> CommitRequests { get; } = [];

        public Task<KnowledgeTransaction> BeginAsync(
            BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(OpenTransaction());

        public Task<KnowledgeTransaction?> FindAsync(
            TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeTransaction?>(OpenTransaction());

        public Task<CommitTransactionResult> CommitAsync(
            CommitTransactionRequest request, CancellationToken cancellationToken = default)
        {
            CommitRequests.Add(request);
            return Task.FromResult(CommitResponse);
        }

        public Task<Result<KnowledgeTransaction>> DiscardAsync(
            TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<KnowledgeTransaction>.Failure(new DomainError(
                "TransactionClosed",
                "Die Transaction ist bereits geschlossen.",
                new Dictionary<string, string> { ["transactionId"] = transactionId.ToString() })));
    }

    private sealed class ScriptedReleaseMutationRepository : IReleaseMutationRepository
    {
        private readonly List<Release> _releases = [];

        public Task<Result<Release>> CreateAsync(
            CreateReleaseRecord request, CancellationToken cancellationToken = default)
        {
            var release = new Release(
                new ReleaseId(_releases.Count + 1),
                request.SnapshotId,
                request.Name,
                request.Description,
                FixedTimestamp);
            _releases.Add(release);
            return Task.FromResult(Result<Release>.Success(release));
        }

        public Task<IReadOnlyList<Release>> ListAsync(
            int limit, long? afterReleaseId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Release>>(_releases);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => TransactionId;

        public NodeId CreateNodeId() => GeneratedNodeId;

        public ContentRevisionId CreateContentRevisionId() => GeneratedRevisionId;
    }
}
