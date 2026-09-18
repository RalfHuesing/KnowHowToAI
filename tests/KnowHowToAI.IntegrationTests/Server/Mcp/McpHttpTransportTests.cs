using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Client;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

public sealed class McpHttpTransportTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task StreamableHttpClient_DiscoversEveryRegisteredToolWithAnInputSchema()
    {
        await using var host = await McpHttpHost.StartAsync();
        await using var client = await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri($"{host.Address}/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp
                }));

        var tools = await client.ListToolsAsync();

        var names = tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);
        var expectedNames = new[]
        {
            "begin_transaction", "get_transaction", "validate_transaction", "commit_transaction", "discard_transaction",
            "get_root", "get_node", "list_children", "list_roles", "search", "export_tree",
            "create_node", "update_node", "move_node", "reorder_node", "delete_node",
            "create_role", "update_role", "delete_role", "set_role_resolution",
            "replace_content", "replace_text", "delete_content",
            "get_snapshot", "list_releases", "compare_snapshots", "get_transaction_changes", "create_release"
        };

        Assert.Subset(names, expectedNames.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(expectedNames.ToHashSet(StringComparer.Ordinal), names);
        Assert.All(tools, tool => Assert.NotNull(tool.JsonSchema));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StreamableHttpClient_CallsListRolesThroughTheRealHttpBoundary()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        harness.AddRole(new Role(new SnapshotId(1), new RoleId("Developer"), "Entwicklung", null, false));
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(harness.CreateService());
        });
        await using var client = await McpClient.CreateAsync(CreateTransport(host.Address));

        var result = await client.CallToolAsync("list_roles");

        Assert.Null(result.IsError);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StreamableHttpClients_CompleteParallelReadRequestsIndependently()
    {
        await using var host = await McpHttpHost.StartAsync();
        await using var firstClient = await McpClient.CreateAsync(CreateTransport(host.Address));
        await using var secondClient = await McpClient.CreateAsync(CreateTransport(host.Address));

        var results = await Task.WhenAll(
            firstClient.CallToolAsync("get_transaction", new Dictionary<string, object?> { ["transactionId"] = "not-a-guid" }).AsTask(),
            secondClient.CallToolAsync("get_transaction", new Dictionary<string, object?> { ["transactionId"] = "not-a-guid" }).AsTask());

        Assert.All(results, result =>
            Assert.Contains("TransactionNotFound", result.Content.Single().ToString(), StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StreamableHttpClient_ReportsParameterAndDomainErrorsWithStableCodesAndDetails()
    {
        await using var host = await McpHttpHost.StartAsync();
        await using var client = await McpClient.CreateAsync(CreateTransport(host.Address));

        var parameterError = await client.CallToolAsync("get_node", new Dictionary<string, object?>
        {
            ["nodeId"] = "not-a-guid",
            ["roleId"] = "Developer"
        });
        var domainError = await client.CallToolAsync("get_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = "not-a-guid"
        });

        using var parameterEnvelope = JsonDocument.Parse(parameterError.Content.Single().ToString()!);
        Assert.Equal("InvalidNodeId", parameterEnvelope.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(parameterEnvelope.RootElement.GetProperty("message").GetString()));
        Assert.Equal("nodeId", Assert.Single(parameterEnvelope.RootElement.GetProperty("details").EnumerateObject()).Name);
        Assert.Equal("not-a-guid", parameterEnvelope.RootElement.GetProperty("details").GetProperty("nodeId").GetString());
        Assert.False(parameterEnvelope.RootElement.TryGetProperty("data", out _));
        Assert.False(parameterEnvelope.RootElement.TryGetProperty("warnings", out _));

        using var domainEnvelope = JsonDocument.Parse(domainError.Content.Single().ToString()!);
        Assert.Equal("TransactionNotFound", domainEnvelope.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(domainEnvelope.RootElement.GetProperty("message").GetString()));
        Assert.Equal("not-a-guid", domainEnvelope.RootElement.GetProperty("details").GetProperty("transactionId").GetString());
        Assert.False(domainEnvelope.RootElement.TryGetProperty("data", out _));
        Assert.False(domainEnvelope.RootElement.TryGetProperty("warnings", out _));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StreamableHttpClient_ObservesTheFirstResponseByteBeforeTheToolCompletes()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        var observingHandler = new FirstByteObservingHandler();
        var streaming = new StreamingRoleRepository(observingHandler.FirstResponseByteObserved.Task);
        var navigation = new NavigationService(
            harness.CreateRepositories() with { Roles = streaming },
            CreateRetrievalPolicy());
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(navigation);
        });
        using var httpClient = new HttpClient(observingHandler);
        await using var client = await McpClient.CreateAsync(
            new HttpClientTransport(CreateOptions(host.Address), httpClient));

        var request = client.CallToolAsync("list_roles").AsTask();
        await streaming.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Der erste Response-Byte erreicht den Client, waehrend der Tool-Aufruf
        // noch auf den Blocking-Repository-Port wartet; nur ein gestreamter,
        // nicht gepufferter Response liefert Bytes vor dem Toolabschluss.
        await observingHandler.FirstResponseByteObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var result = await request;

        Assert.Null(result.IsError);
        Assert.True(streaming.ObservedFirstResponseByteBeforeCompletion);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ClientAbort_CancelsTheBlockedCallWithoutEndingTheParallelSuccessCall()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        harness.AddRole(new Role(new SnapshotId(1), new RoleId("Developer"), "Entwicklung", null, false));
        var roles = new FirstCallBlockingRoleRepository(harness.CreateRepositories().Roles);
        var navigation = new NavigationService(
            harness.CreateRepositories() with { Roles = roles },
            CreateRetrievalPolicy());
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(navigation);
        });
        await using var abortedClient = await McpClient.CreateAsync(CreateTransport(host.Address));
        await using var successClient = await McpClient.CreateAsync(CreateTransport(host.Address));
        using var cancellation = new CancellationTokenSource();

        var abortedRequest = abortedClient
            .CallToolAsync("list_roles", cancellationToken: cancellation.Token)
            .AsTask();
        await roles.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var successCall = successClient.CallToolAsync("list_roles").AsTask();
        var successResult = await successCall.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Null(successResult.IsError);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abortedRequest);
        await roles.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StreamableHttpClient_CompletesBeginMutationAndDiscardWorkflow()
    {
        var transaction = new WorkflowTransactionRepository();
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<TransactionService>();
            services.AddSingleton(new TransactionService(
                transaction,
                new ValidatingWorkingSnapshotRepository(),
                new WorkflowIdentifierGenerator(),
                new ValidationPolicy { ContentSizeWarningBytes = 4096, ChildCountWarning = 25, HierarchyDepthWarning = 8, PossibleEmbeddedHeadingWarning = true }));
            services.RemoveAll<RoleMutationService>();
            services.AddSingleton(new RoleMutationService(new InMemoryRoleMutationRepository(
                new WorkingRoleMutationState(new SnapshotId(2), [], [], [], []))));
        });
        await using var client = await McpClient.CreateAsync(CreateTransport(host.Address));

        var begin = await client.CallToolAsync("begin_transaction");
        var mutation = await client.CallToolAsync("create_role", new Dictionary<string, object?>
        {
            ["transactionId"] = WorkflowTransactionRepository.Id.ToString(), ["name"] = "Reviewer"
        });
        var discard = await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = WorkflowTransactionRepository.Id.ToString()
        });

        Assert.Null(begin.IsError);
        Assert.Null(mutation.IsError);
        Assert.Null(discard.IsError);
        Assert.Equal(WorkflowTransactionRepository.Id, Assert.Single(transaction.Discarded));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMcp_IsRejectedOutsideTheStreamableHttpMapping()
    {
        await using var host = await McpHttpHost.StartAsync();
        using var client = new HttpClient();

        using var response = await client.GetAsync($"{host.Address}/mcp");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LegacySseRoute_IsNotAnMcpEndpoint()
    {
        await using var host = await McpHttpHost.StartAsync();
        using var client = new HttpClient();

        using var response = await client.GetAsync($"{host.Address}/mcp/sse");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private static RetrievalPolicy CreateRetrievalPolicy() => new()
    {
        DefaultPageSize = 10,
        MaximumPageSize = 100,
        SearchPageSize = 10,
        SearchMaximumPageSize = 100,
        SnippetMaximumCharacters = 100
    };

    private static HttpClientTransportOptions CreateOptions(string address) =>
        McpHttpHost.CreateTransportOptions(address);

    private static HttpClientTransport CreateTransport(string address) => new(CreateOptions(address));

    private sealed class FirstByteObservingHandler : DelegatingHandler
    {
        public FirstByteObservingHandler()
            : base(new HttpClientHandler())
        {
        }

        public TaskCompletionSource FirstResponseByteObserved { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (body.Contains("tools/call", StringComparison.Ordinal))
                {
                    // Das Lesen verbraucht den urspruenglichen Content; die Kopie
                    // erhaelt alle Header und den unveranderten Body.
                    var bufferedContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
                    foreach (var header in request.Content.Headers)
                        bufferedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    request.Content = bufferedContent;

                    var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    FirstResponseByteObserved.TrySetResult();
                    return response;
                }
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class StreamingRoleRepository(Task firstResponseByteObserved) : IRoleRepository
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ObservedFirstResponseByteBeforeCompletion { get; private set; }

        public async Task<IReadOnlyList<Role>> ListBySnapshotAsync(
            SnapshotId snapshotId,
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            await firstResponseByteObserved
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                .ConfigureAwait(false);
            ObservedFirstResponseByteBeforeCompletion = true;
            return [];
        }

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(
            SnapshotId snapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleResolution>>([]);
    }

    private sealed class FirstCallBlockingRoleRepository(IRoleRepository inner) : IRoleRepository
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _blockingCallPending = 1;

        public async Task<IReadOnlyList<Role>> ListBySnapshotAsync(
            SnapshotId snapshotId,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _blockingCallPending, 0) == 1)
            {
                Started.SetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Cancelled.SetResult();
                    throw;
                }
            }

            return await inner.ListBySnapshotAsync(snapshotId, cancellationToken);
        }

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(
            SnapshotId snapshotId,
            CancellationToken cancellationToken = default) =>
            inner.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken);
    }

    private sealed class WorkflowTransactionRepository : ITransactionRepository
    {
        public static TransactionId Id { get; } = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        public List<TransactionId> Discarded { get; } = [];

        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Open());

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeTransaction?>(Open());

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CommitTransactionResult(null, null, null));

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
        {
            Discarded.Add(transactionId);
            return Task.FromResult(Result<KnowledgeTransaction>.Success(Open()));
        }

        private static KnowledgeTransaction Open() => new(Id, new SnapshotId(1), new SnapshotId(2), TransactionState.Open, 0, DateTimeOffset.UnixEpoch, null, null, null, null, null);
    }

    private sealed class WorkflowIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => WorkflowTransactionRepository.Id;
        public NodeId CreateNodeId() => throw new NotSupportedException();
        public ContentRevisionId CreateContentRevisionId() => throw new NotSupportedException();
    }
}
