using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace KnowHowToAI.BrowserTests.TestSupport;

internal static class BrowserMcpAssertions
{
    public static async Task<Guid> BeginTransactionAsync(string address, string purpose)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var response = await CallAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = purpose,
            ["client"] = "KnowHowToAI.BrowserTests"
        });
        var value = RequiredString(response, "transactionId");
        return Guid.TryParseExact(value, "D", out var transactionId)
            ? transactionId
            : throw new InvalidOperationException("MCP lieferte keine gültige Transaction-ID.");
    }

    public static async Task<JsonElement> CallAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(toolName, arguments);
        using var document = JsonDocument.Parse(result.Content.Single().ToString()!);
        var response = document.RootElement.Clone();
        Assert.Equal("Success", response.GetProperty("code").GetString());
        return response;
    }

    public static string RequiredString(JsonElement response, string propertyName) =>
        response.GetProperty("data").GetProperty(propertyName).GetString()
        ?? throw new InvalidOperationException($"MCP-Feld '{propertyName}' fehlt.");
}
