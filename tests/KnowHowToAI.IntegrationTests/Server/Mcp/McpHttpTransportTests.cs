using System.Net;
using System.Net.Http;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
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
}
