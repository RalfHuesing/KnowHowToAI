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
    private const string HistorySourceTitle = "Browser-History-Quelle";
    private const string HistoryDerivedTitle = "Browser-History-Diff-Knoten";
    private const string HistoryRoleId = "BrowserHistoryDiffRole";
    private const string HistoryReleaseName = "Browser History Release";
    private const int HistoryPagingSnapshotCount = 20;
    private const string ExportContent = "Browser-Testinhalt für den Markdown-Download.";
    private static readonly SemaphoreSlim SeedGate = new(1, 1);

    public static async Task EnsureWorkflowAsync(string address)
    {
        using var lease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await EnsureAsync(address, includeHistoryEvidence: true);
    }

    public static Task EnsureVisualShellAsync(string address) =>
        EnsureAsync(address, includeHistoryEvidence: false);

    private static async Task EnsureAsync(string address, bool includeHistoryEvidence)
    {
        await SeedGate.WaitAsync();
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
                if (includeHistoryEvidence)
                    await EnsureHistoryEvidenceAsync(client, rootNodeId);

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
                if (includeHistoryEvidence)
                    await EnsureHistoryEvidenceAsync(client, rootNodeId);
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
            SeedGate.Release();
        }
    }

    private static async Task EnsureHistoryEvidenceAsync(McpClient client, string rootNodeId)
    {
        if (await ContainsChildAsync(client, rootNodeId, HistoryDerivedTitle))
            return;

        var baseTransaction = await CallAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "Browser-History-Basis",
            ["actor"] = "Browser History Actor",
            ["client"] = "KnowHowToAI.BrowserTests"
        });
        var baseTransactionId = RequireDataString(baseTransaction, "transactionId", "begin_transaction");
        try
        {
            var source = await CallAsync(client, "create_node", new Dictionary<string, object?>
            {
                ["transactionId"] = baseTransactionId,
                ["title"] = HistorySourceTitle,
                ["parentNodeId"] = rootNodeId,
                ["contentMd"] = "Quelle für den Browser-History-Diff.",
                ["roleId"] = DefaultRoleId
            });
            var sourceNodeId = RequireDataString(source, "nodeId", "create_node");
            var sourceRevisionId = RequireDataString(source, "contentRevisionId", "create_node");
            var baseCommit = await CallAsync(client, "commit_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = baseTransactionId,
                ["commitMessage"] = "Browser-History-Basis commit"
            });
            var baseSnapshotId = RequireDataString(baseCommit, "workingSnapshotId", "commit_transaction");

            var diffTransaction = await CallAsync(client, "begin_transaction", new Dictionary<string, object?>
            {
                ["purpose"] = "Browser-History-Diff",
                ["actor"] = "Browser History Actor",
                ["client"] = "KnowHowToAI.BrowserTests"
            });
            var diffTransactionId = RequireDataString(diffTransaction, "transactionId", "begin_transaction");
            try
            {
                await RequireSuccessAsync(client, "create_role", new Dictionary<string, object?>
                {
                    ["transactionId"] = diffTransactionId,
                    ["name"] = HistoryRoleId,
                    ["description"] = "Rolle für den Browser-History-Diff"
                });
                await RequireSuccessAsync(client, "set_role_resolution", new Dictionary<string, object?>
                {
                    ["transactionId"] = diffTransactionId,
                    ["roleId"] = HistoryRoleId,
                    ["candidateRoleIds"] = new[] { HistoryRoleId, DefaultRoleId }
                });
                await RequireSuccessAsync(client, "create_node", new Dictionary<string, object?>
                {
                    ["transactionId"] = diffTransactionId,
                    ["title"] = HistoryDerivedTitle,
                    ["parentNodeId"] = rootNodeId,
                    ["contentMd"] = "Abgeleiteter Browser-History-Inhalt.",
                    ["roleId"] = DefaultRoleId,
                    ["contentMode"] = "Derived",
                    ["sources"] = new[]
                    {
                        new Dictionary<string, string>
                        {
                            ["nodeId"] = sourceNodeId,
                            ["roleId"] = DefaultRoleId,
                            ["contentRevisionId"] = sourceRevisionId
                        }
                    }
                });
                for (var index = 0; index < HistoryPagingSnapshotCount; index++)
                {
                    await RequireSuccessAsync(client, "create_node", new Dictionary<string, object?>
                    {
                        ["transactionId"] = diffTransactionId,
                        ["title"] = $"Browser-History-Diff-Seite-{index + 1}",
                        ["parentNodeId"] = rootNodeId
                    });
                }

                var targetCommit = await CallAsync(client, "commit_transaction", new Dictionary<string, object?>
                {
                    ["transactionId"] = diffTransactionId,
                    ["commitMessage"] = "Browser-History-Diff commit"
                });
                var targetSnapshotId = RequireDataString(targetCommit, "workingSnapshotId", "commit_transaction");
                await CreateHistoryReleaseAsync(client, targetSnapshotId);
            }
            catch
            {
                await DiscardAsync(client, diffTransactionId);
                throw;
            }

            await CreateHistoryPagingSnapshotsAsync(client, rootNodeId);
        }
        catch
        {
            await DiscardAsync(client, baseTransactionId);
            throw;
        }
    }

    private static Task CreateHistoryReleaseAsync(McpClient client, string targetSnapshotId) =>
        RequireSuccessAsync(client, "create_release", new Dictionary<string, object?>
        {
            ["name"] = HistoryReleaseName,
            ["snapshotId"] = targetSnapshotId,
            ["description"] = "Release für den Browser-History-Diff"
        });

    private static async Task CreateHistoryPagingSnapshotsAsync(McpClient client, string rootNodeId)
    {
        for (var index = 0; index < HistoryPagingSnapshotCount; index++)
        {
            var pagingTransaction = await CallAsync(client, "begin_transaction", new Dictionary<string, object?>
            {
                ["purpose"] = $"Browser-History-Paging-{index + 1}",
                ["actor"] = "Browser History Actor",
                ["client"] = "KnowHowToAI.BrowserTests"
            });
            var pagingTransactionId = RequireDataString(pagingTransaction, "transactionId", "begin_transaction");
            try
            {
                await RequireSuccessAsync(client, "create_node", new Dictionary<string, object?>
                {
                    ["transactionId"] = pagingTransactionId,
                    ["title"] = $"Browser-History-Listen-Seite-{index + 1}",
                    ["parentNodeId"] = rootNodeId
                });
                await RequireSuccessAsync(client, "commit_transaction", new Dictionary<string, object?>
                {
                    ["transactionId"] = pagingTransactionId,
                    ["commitMessage"] = $"Browser-History-Paging commit {index + 1}"
                });
            }
            catch
            {
                await DiscardAsync(client, pagingTransactionId);
                throw;
            }
        }
    }

    private static async Task<bool> ContainsExportNodeAsync(McpClient client, string rootNodeId)
        => await ContainsChildAsync(client, rootNodeId, ExportNodeTitle);

    private static async Task<bool> ContainsChildAsync(McpClient client, string rootNodeId, string title)
    {
        var children = await CallAsync(client, "list_children", new Dictionary<string, object?>
        {
            ["roleId"] = DefaultRoleId,
            ["parentNodeId"] = rootNodeId,
            ["limit"] = 100
        });
        return children.GetProperty("data").GetProperty("items").EnumerateArray()
            .Any(item => string.Equals(item.GetProperty("title").GetString(), title, StringComparison.Ordinal));
    }

    private static async Task DiscardAsync(McpClient client, string transactionId) =>
        await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId
        });

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
