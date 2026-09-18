using KnowHowToAI.Server;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Gemeinsames MCP-HTTP-Test-Fixture: startet einen realen Kestrel-Host auf
/// dynamischem Loopback-Port (M1.3-T1) und stellt die Transport-Optionen für den
/// offiziellen SDK-McpClient mit HttpClientTransport (StreamableHttp) bereit.
/// </summary>
internal sealed class McpHttpHost : IAsyncDisposable
{
    private readonly Microsoft.AspNetCore.Builder.WebApplication _application;

    private McpHttpHost(Microsoft.AspNetCore.Builder.WebApplication application, string address)
    {
        _application = application;
        Address = address;
    }

    public string Address { get; }

    public static async Task<McpHttpHost> StartAsync(Action<IServiceCollection>? configureServices = null)
    {
        var application = Program.CreateApplication(
        [
            "--urls", "http://127.0.0.1:0",
            "--KnowHowToAI:Migrations:ApplyOnStartup=false"
        ], configureServices);
        await application.StartAsync();

        return new McpHttpHost(application, application.Urls.Single());
    }

    public static HttpClientTransportOptions CreateTransportOptions(string address) => new()
    {
        Endpoint = new Uri($"{address}/mcp"),
        TransportMode = HttpTransportMode.StreamableHttp
    };

    public static HttpClientTransport CreateTransport(string address) => new(CreateTransportOptions(address));

    public async ValueTask DisposeAsync()
    {
        await _application.StopAsync();
        await _application.DisposeAsync();
    }
}
