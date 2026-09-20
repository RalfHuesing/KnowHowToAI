using System.Text.Json;
using ModelContextProtocol.Client;

namespace KnowHowToAI.BrowserTests.TestSupport;

internal static class BrowserMcpAssertions
{
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
