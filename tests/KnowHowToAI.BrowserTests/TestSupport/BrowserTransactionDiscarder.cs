using System.Text.Json;
using ModelContextProtocol.Client;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Räumt von Browser-Smokes erzeugte Working Transactions über den echten
/// MCP-Produktpfad auch dann auf, wenn ihre Assertions fehlschlagen.
/// </summary>
internal static class BrowserTransactionDiscarder
{
    public static async Task DiscardAsync(string address, Guid transactionId)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var result = await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId.ToString("D")
        });
        using var document = JsonDocument.Parse(result.Content.Single().ToString()!);
        var code = document.RootElement.GetProperty("code").GetString();
        if (!string.Equals(code, "Success", StringComparison.Ordinal))
            throw new InvalidOperationException($"Die Browser-Testtransaction konnte nicht verworfen werden (Code: {code}).");
    }
}
