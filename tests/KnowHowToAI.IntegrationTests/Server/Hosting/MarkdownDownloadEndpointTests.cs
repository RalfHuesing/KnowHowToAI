using System.Net;
using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.Server.Mcp;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

[Trait("Category", "Integration")]
public sealed class MarkdownDownloadEndpointTests
{
    private const int LargeContentLength = 131_072;
    private static readonly SnapshotId CurrentSnapshotId = new(1);
    private static readonly SnapshotId HistoricalSnapshotId = new(2);
    private static readonly NodeId RootNodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000000"));
    private static readonly NodeId ChildNodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000001"));
    private static readonly AudienceId DeveloperRoleId = new("Developer");
    private static readonly AudienceId EndUserRoleId = new("EndUser");
    private const string TechnicalErrorCode = "MarkdownDownloadTechnicalError";
    private const string InternalFailureDetail = "Interne Details dürfen nicht in der Antwort erscheinen.";

    [Fact]
    public async Task DownloadMarkdown_ExportsRequestedSubtreeWithFallbackAndAttachmentHeaders()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddAudience(new Audience(CurrentSnapshotId, EndUserRoleId, "Endanwender", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, EndUserRoleId, DeveloperRoleId, 1));
        harness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Wissensbasis", null, 0, false));
        harness.AddNode(new Node(CurrentSnapshotId, ChildNodeId, RootNodeId, "Teilbaum:/--", null, 1, false));
        harness.AddContent(Content(CurrentSnapshotId, ChildNodeId, "Fallback-Inhalt"));

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={ChildNodeId}&roleId={EndUserRoleId.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/markdown", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var disposition = response.Content.Headers.ContentDisposition
            ?? throw new InvalidOperationException("Der Download enthält keinen Content-Disposition-Header.");
        Assert.Equal("attachment", disposition.DispositionType);
        Assert.Contains("Teilbaum-EndUser.md", disposition.ToString(), StringComparison.Ordinal);
        Assert.Equal("# Teilbaum:/--\n\nFallback-Inhalt\n", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DownloadMarkdown_UsesExplicitSnapshotContext()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Aktuell", null, 0, false));
        harness.AddContent(Content(CurrentSnapshotId, RootNodeId, "Aktueller Inhalt"));
        harness.AddHistoricalSnapshot(new Snapshot(
            HistoricalSnapshotId,
            null,
            SnapshotState.Committed,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch));
        harness.AddNode(new Node(HistoricalSnapshotId, RootNodeId, null, "Historisch", null, 0, false));
        harness.AddContent(Content(HistoricalSnapshotId, RootNodeId, "Historischer Inhalt"));

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}&snapshotId={HistoricalSnapshotId.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Historisch-Developer.md", response.Content.Headers.ContentDisposition!.ToString(), StringComparison.Ordinal);
        Assert.Equal("# Historisch\n\nHistorischer Inhalt\n", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DownloadMarkdown_DoesNotTruncateLargeMarkdown()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var largeContent = new string('x', LargeContentLength);
        harness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Großer Export", null, 0, false));
        harness.AddContent(Content(CurrentSnapshotId, RootNodeId, largeContent));

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var markdown = await response.Content.ReadAsStringAsync();
        Assert.Contains(largeContent, markdown, StringComparison.Ordinal);
        Assert.True(markdown.Length > LargeContentLength);
    }

    [Fact]
    public async Task DownloadMarkdown_UnknownNodeReturnsProblemDetailsWithoutAttachment()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var unknownNodeId = Guid.Parse("40000000-0000-0000-0000-000000009999");

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={unknownNodeId:D}&roleId={DeveloperRoleId.Value}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Null(response.Content.Headers.ContentDisposition);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(NavigationErrorCodes.NodeNotFound, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task DownloadMarkdown_InvalidNodeIdReturnsBadRequestProblemDetailsWithoutAttachment()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync($"{host.Address}/downloads/markdown?nodeId=ungültig&roleId={DeveloperRoleId.Value}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, NavigationErrorCodes.InvalidNodeId);
    }

    [Fact]
    public async Task DownloadMarkdown_ClosedTransactionReturnsConflictProblemDetailsWithoutAttachment()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var resolver = new FailingReadContextResolver(new DomainError(
            ReadContextErrorCodes.TransactionClosed,
            "Die angefragte Transaktion ist bereits geschlossen."));

        await using var host = await StartWithHarnessAsync(harness, readContextResolver: resolver);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}&transactionId={Guid.NewGuid():D}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, ReadContextErrorCodes.TransactionClosed);
    }

    [Fact]
    public async Task DownloadMarkdown_UnknownDomainCodeReturnsNeutralTechnicalProblemDetails()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var resolver = new FailingReadContextResolver(new DomainError("UnknownDomainCode", InternalFailureDetail));

        await using var host = await StartWithHarnessAsync(harness, readContextResolver: resolver);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}");

        await AssertTechnicalProblemAsync(response);
    }

    [Fact]
    public async Task DownloadMarkdown_NavigationExceptionReturnsNeutralTechnicalProblemDetails()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);

        await using var host = await StartWithHarnessAsync(
            harness,
            navigationService: new NavigationService(CreateThrowingRepositories(), new RetrievalPolicy()));
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}");

        await AssertTechnicalProblemAsync(response);
    }

    [Fact]
    public async Task DownloadMarkdown_ExportExceptionReturnsNeutralTechnicalProblemDetails()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Wissensbasis", null, 0, false));

        await using var host = await StartWithHarnessAsync(
            harness,
            markdownExportService: new MarkdownExportService(CreateThrowingRepositories()));
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}");

        await AssertTechnicalProblemAsync(response);
    }

    [Fact]
    public async Task DownloadMarkdown_RequestAbortionPropagatesWithoutProblemDetailsResponse()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var cancellationRepository = new CancellationSnapshotRepository();
        var navigationService = new NavigationService(
            CreateRepositories(cancellationRepository),
            new RetrievalPolicy());

        await using var host = await StartWithHarnessAsync(harness, navigationService: navigationService);
        using var client = new HttpClient();
        using var cancellation = new CancellationTokenSource();
        var requestTask = client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}",
            cancellation.Token);

        await cancellationRepository.RequestStarted.Task;
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode, string code)
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Null(response.Content.Headers.ContentDisposition);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    private static async Task AssertTechnicalProblemAsync(HttpResponseMessage response)
    {
        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, TechnicalErrorCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(InternalFailureDetail, responseBody, StringComparison.Ordinal);
    }

    private static SnapshotReadRepositories CreateThrowingRepositories() =>
        CreateRepositories(new ThrowingSnapshotRepository());

    private static SnapshotReadRepositories CreateRepositories(ISnapshotRepository snapshots)
    {
        var store = new InMemoryKnowledgeStore();
        return new SnapshotReadRepositories(
            snapshots,
            new InMemoryTransactionRepository(store),
            new InMemoryHierarchyRepository(store),
            new InMemoryContentRepository(store),
            new InMemoryAudienceRepository(store),
            new InMemoryDependencyRepository(store),
            new InMemoryWorkingSnapshotReadRepository(store));
    }

    private static async Task<McpHttpHost> StartWithHarnessAsync(
        NavigationTestHarness harness,
        NavigationService? navigationService = null,
        MarkdownExportService? markdownExportService = null,
        IWebReadContextResolver? readContextResolver = null) =>
        await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(navigationService ?? harness.CreateService());
            services.RemoveAll<MarkdownExportService>();
            services.AddSingleton(markdownExportService ?? harness.CreateExportService());

            if (readContextResolver is not null)
            {
                services.RemoveAll<IWebReadContextResolver>();
                services.AddSingleton(readContextResolver);
            }
        });

    private sealed class FailingReadContextResolver(DomainError error) : IWebReadContextResolver
    {
        public Task<Result<WebReadContextResolution>> ResolveAsync(
            string? transactionIdRaw,
            string? snapshotIdRaw,
            string? releaseIdRaw,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<WebReadContextResolution>.Failure(error));
    }

    private sealed class ThrowingSnapshotRepository : ISnapshotRepository
    {
        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromException<Snapshot?>(new InvalidOperationException(InternalFailureDetail));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<Snapshot>(new InvalidOperationException(InternalFailureDetail));

        public Task<IReadOnlyList<Snapshot>> ListCommittedAsync(
            int limit,
            SnapshotId? beforeSnapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<Snapshot>>(new InvalidOperationException(InternalFailureDetail));
    }

    private sealed class CancellationSnapshotRepository : ISnapshotRepository
    {
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Snapshot?>(null);

        public async Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            RequestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Der Abbruch muss den Delay beenden.");
        }

        public Task<IReadOnlyList<Snapshot>> ListCommittedAsync(
            int limit,
            SnapshotId? beforeSnapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Snapshot>>([]);
    }

    private static NodeContent Content(SnapshotId snapshotId, NodeId nodeId, string markdown) => new(
        snapshotId,
        nodeId,
        DeveloperRoleId,
        new ContentRevisionId(Guid.NewGuid()),
        ContentMode.Independent,
        markdown,
        IsDeleted: false);
}
