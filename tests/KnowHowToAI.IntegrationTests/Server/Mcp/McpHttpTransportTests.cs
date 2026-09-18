using System.Net;
using System.Net.Http;
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
using KnowHowToAI.Core.Tests.Application.Navigation;
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
    public async Task ClientAbort_CancelsTheTokenReceivedByTheListRolesPort()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        var roles = new BlockingRoleRepository();
        var navigation = new NavigationService(
            harness.CreateRepositories() with { Roles = roles },
            new RetrievalPolicy
            {
                DefaultPageSize = 10,
                MaximumPageSize = 100,
                SearchPageSize = 10,
                SearchMaximumPageSize = 100,
                SnippetMaximumCharacters = 100
            });
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(navigation);
        });
        await using var client = await McpClient.CreateAsync(CreateTransport(host.Address));
        using var cancellation = new CancellationTokenSource();

        var request = client.CallToolAsync("list_roles", cancellationToken: cancellation.Token).AsTask();
        await roles.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
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
                new ValidatingRepository(),
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

    private sealed class McpHttpHost : IAsyncDisposable
    {
        private McpHttpHost(Microsoft.AspNetCore.Builder.WebApplication application, string address)
        {
            Application = application;
            Address = address;
        }

        private Microsoft.AspNetCore.Builder.WebApplication Application { get; }

        public string Address { get; }

        public static async Task<McpHttpHost> StartAsync(Action<IServiceCollection>? configureServices = null)
        {
            var application = Program.CreateApplication(
            [
                "--urls", "http://127.0.0.1:0",
                "--KnowHowToAI:Migrations:ApplyOnStartup=false"
            ], configureServices);
            await application.StartAsync();

            var address = application.Urls.Single();
            return new McpHttpHost(application, address);
        }

        public async ValueTask DisposeAsync()
        {
            await Application.StopAsync();
            await Application.DisposeAsync();
        }
    }

    private static HttpClientTransport CreateTransport(string address) =>
        new(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            });

    private sealed class BlockingRoleRepository : IRoleRepository
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<Role>> ListBySnapshotAsync(
            SnapshotId snapshotId,
            CancellationToken cancellationToken = default)
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

            return [];
        }

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(
            SnapshotId snapshotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleResolution>>([]);
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

    private sealed class ValidatingRepository : IWorkingSnapshotValidationDataRepository
    {
        public Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<WorkingSnapshotValidationData>.Success(new WorkingSnapshotValidationData([], [], [], [], [])));
    }
}
