using System.Text.Json;
using ModelContextProtocol.Client;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Stellt ausschließlich in der dedizierten Browser-Testdatenbank einen kleinen,
/// über den echten MCP-Transport angelegten Lesebestand für Browserabläufe bereit.
/// </summary>
internal static class BrowserKnowledgeSeed
{
    internal const string ExportNodeTitle = "Browser-Export-Teilbaum";

    private const string DefaultRoleId = "Default";
    private const string ReaderRoleId = "BrowserDownloadReader";
    private const string ExportContent = "Browser-Testinhalt für den Markdown-Download.";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task EnsureAsync(string address)
    {
        await Gate.WaitAsync();
        try
        {
            await using var client = await McpClient.CreateAsync(new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri($"{address}/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp
                }));

            var roles = await CallAsync(client, "list_roles");
            var hasDefaultRole = ContainsRole(roles, DefaultRoleId);
            var hasReaderRole = ContainsRole(roles, ReaderRoleId);
            var rootNodeId = hasDefaultRole
                ? TryGetDataString(
                    await CallAsync(client, "get_root", new Dictionary<string, object?> { ["roleId"] = DefaultRoleId }),
                    "nodeId")
                : null;

            if (hasReaderRole && rootNodeId is not null
                && await ContainsExportNodeAsync(client, rootNodeId))
            {
                return;
            }

            var transaction = await CallAsync(client, "begin_transaction", new Dictionary<string, object?>
            {
                ["purpose"] = "Browser-Testbestand für Markdown-Download",
                ["client"] = "KnowHowToAI.BrowserTests"
            });
            var transactionId = RequireDataString(transaction, "transactionId", "begin_transaction");
            try
            {
                if (!hasDefaultRole)
                {
                    await RequireSuccessAsync(client, "create_role", new Dictionary<string, object?>
                    {
                        ["transactionId"] = transactionId,
                        ["name"] = DefaultRoleId,
                        ["description"] = "Browser-Testrolle"
                    });
                }

                if (!hasReaderRole)
                {
                    await RequireSuccessAsync(client, "create_role", new Dictionary<string, object?>
                    {
                        ["transactionId"] = transactionId,
                        ["name"] = ReaderRoleId,
                        ["description"] = "Browser-Testrolle mit Fallback"
                    });
                    await RequireSuccessAsync(client, "set_role_resolution", new Dictionary<string, object?>
                    {
                        ["transactionId"] = transactionId,
                        ["roleId"] = ReaderRoleId,
                        ["candidateRoleIds"] = new[] { ReaderRoleId, DefaultRoleId }
                    });
                }

                if (rootNodeId is null)
                {
                    var rootNode = await CallAsync(client, "create_node", new Dictionary<string, object?>
                    {
                        ["transactionId"] = transactionId,
                        ["title"] = "Browser-Testwissen",
                        ["contentMd"] = "Wurzelinhalt der Browser-Testdaten.",
                        ["roleId"] = DefaultRoleId
                    });
                    rootNodeId = RequireDataString(rootNode, "nodeId", "create_node");
                }

                await RequireSuccessAsync(client, "create_node", new Dictionary<string, object?>
                {
                    ["transactionId"] = transactionId,
                    ["title"] = ExportNodeTitle,
                    ["parentNodeId"] = rootNodeId,
                    ["contentMd"] = ExportContent,
                    ["roleId"] = DefaultRoleId
                });
                await RequireSuccessAsync(client, "commit_transaction", new Dictionary<string, object?>
                {
                    ["transactionId"] = transactionId,
                    ["commitMessage"] = "Browser-Testbestand für Markdown-Download"
                });
            }
            catch
            {
                await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
                {
                    ["transactionId"] = transactionId
                });
                throw;
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<bool> ContainsExportNodeAsync(McpClient client, string rootNodeId)
    {
        var children = await CallAsync(client, "list_children", new Dictionary<string, object?>
        {
            ["roleId"] = DefaultRoleId,
            ["parentNodeId"] = rootNodeId,
            ["limit"] = 100
        });
        return children.GetProperty("data").GetProperty("items").EnumerateArray()
            .Any(item => string.Equals(item.GetProperty("title").GetString(), ExportNodeTitle, StringComparison.Ordinal));
    }

    private static bool ContainsRole(JsonElement roles, string roleId) =>
        roles.GetProperty("data").GetProperty("items").EnumerateArray()
            .Any(item => string.Equals(item.GetProperty("roleId").GetString(), roleId, StringComparison.Ordinal));

    private static async Task RequireSuccessAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?> arguments) =>
        EnsureSuccess(await CallAsync(client, toolName, arguments), toolName);

    private static async Task<JsonElement> CallAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(toolName, arguments);
        using var document = JsonDocument.Parse(result.Content.Single().ToString()!);
        var response = document.RootElement.Clone();
        EnsureSuccess(response, toolName);
        return response;
    }

    private static void EnsureSuccess(JsonElement response, string toolName)
    {
        if (string.Equals(response.GetProperty("code").GetString(), "Success", StringComparison.Ordinal))
            return;

        throw new InvalidOperationException(
            $"Der Browser-Testseed konnte MCP-Tool '{toolName}' nicht ausführen (Code: {response.GetProperty("code").GetString()}).");
    }

    private static string RequireDataString(JsonElement response, string propertyName, string toolName) =>
        TryGetDataString(response, propertyName)
        ?? throw new InvalidOperationException(
            $"Der Browser-Testseed erhielt von MCP-Tool '{toolName}' kein Feld '{propertyName}'.");

    private static string? TryGetDataString(JsonElement response, string propertyName) =>
        response.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Object
        && data.TryGetProperty(propertyName, out var value)
            ? value.GetString()
            : null;
}
