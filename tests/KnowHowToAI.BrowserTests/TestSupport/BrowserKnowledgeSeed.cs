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

    private const string DefaultAudienceId = "Default";
    private const string ReaderAudienceId = "BrowserDownloadAudience";
    private const string HistorySourceTitle = "Browser-History-Quelle";
    private const string HistoryDerivedTitle = "Browser-History-Diff-Knoten";
    private const string HistoryAudienceId = "BrowserHistoryDiffAudience";
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

            var audiences = await CallAsync(client, "list_audiences");
            var hasDefaultAudience = ContainsAudience(audiences, DefaultAudienceId);
            var hasReaderAudience = ContainsAudience(audiences, ReaderAudienceId);
            var rootNodeId = hasDefaultAudience
                ? TryGetDataString(
                    await CallAsync(client, "get_root", new Dictionary<string, object?> { ["audienceId"] = DefaultAudienceId }),
                    "nodeId")
                : null;

            if (hasReaderAudience && rootNodeId is not null
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
                await EnsureWorkflowAudiencesAsync(client, transactionId, hasDefaultAudience, hasReaderAudience);

                if (rootNodeId is null)
                {
                    var rootNode = await CallAsync(client, "create_node", new Dictionary<string, object?>
                    {
                        ["transactionId"] = transactionId,
                        ["title"] = "Browser-Testwissen",
                        ["contentMd"] = "Wurzelinhalt der Browser-Testdaten.",
                        ["audienceId"] = DefaultAudienceId
                    });
                    rootNodeId = RequireDataString(rootNode, "nodeId", "create_node");
                }

                await RequireSuccessAsync(client, "create_node", new Dictionary<string, object?>
                {
                    ["transactionId"] = transactionId,
                    ["title"] = ExportNodeTitle,
                    ["parentNodeId"] = rootNodeId,
                    ["contentMd"] = ExportContent,
                    ["audienceId"] = DefaultAudienceId
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

    private static async Task EnsureWorkflowAudiencesAsync(
        McpClient client,
        string transactionId,
        bool hasDefaultAudience,
        bool hasReaderAudience)
    {
        var transactionChangeVersion = 0L;
        if (!hasDefaultAudience)
        {
            var createdDefaultAudience = await RequireSuccessAsync(client, "create_audience", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId,
                ["name"] = DefaultAudienceId,
                ["expectedChangeVersion"] = transactionChangeVersion,
                ["description"] = "Browser-Testzielgruppe"
            });
            transactionChangeVersion = TryGetDataLong(createdDefaultAudience, "changeVersion") ?? transactionChangeVersion + 1;
        }

        if (!hasReaderAudience)
        {
            var createdReaderAudience = await RequireSuccessAsync(client, "create_audience", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId,
                ["name"] = ReaderAudienceId,
                ["expectedChangeVersion"] = transactionChangeVersion,
                ["description"] = "Browser-Testzielgruppe mit Fallback"
            });
            transactionChangeVersion = TryGetDataLong(createdReaderAudience, "changeVersion") ?? transactionChangeVersion + 1;
            await RequireSuccessAsync(client, "set_audience_resolution", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId,
                ["audienceId"] = ReaderAudienceId,
                ["candidateAudienceIds"] = new[] { ReaderAudienceId, DefaultAudienceId },
                ["expectedChangeVersion"] = transactionChangeVersion
            });
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
                ["audienceId"] = DefaultAudienceId
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
                await EnsureHistoryAudienceAsync(client, diffTransactionId);
                await RequireSuccessAsync(client, "create_node", new Dictionary<string, object?>
                {
                    ["transactionId"] = diffTransactionId,
                    ["title"] = HistoryDerivedTitle,
                    ["parentNodeId"] = rootNodeId,
                    ["contentMd"] = "Abgeleiteter Browser-History-Inhalt.",
                    ["audienceId"] = DefaultAudienceId,
                    ["contentMode"] = "Derived",
                    ["sources"] = new[]
                    {
                        new Dictionary<string, string>
                        {
                            ["nodeId"] = sourceNodeId,
                            ["audienceId"] = DefaultAudienceId,
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

    private static async Task EnsureHistoryAudienceAsync(McpClient client, string transactionId)
    {
        var createdAudience = await RequireSuccessAsync(client, "create_audience", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId,
            ["name"] = HistoryAudienceId,
            ["expectedChangeVersion"] = 0L,
            ["description"] = "Zielgruppe für den Browser-History-Diff"
        });
        var changeVersion = TryGetDataLong(createdAudience, "changeVersion") ?? 1L;
        await RequireSuccessAsync(client, "set_audience_resolution", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId,
            ["audienceId"] = HistoryAudienceId,
            ["candidateAudienceIds"] = new[] { HistoryAudienceId, DefaultAudienceId },
            ["expectedChangeVersion"] = changeVersion
        });
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
            ["audienceId"] = DefaultAudienceId,
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

    private static bool ContainsAudience(JsonElement audiences, string audienceId) =>
        audiences.GetProperty("data").GetProperty("items").EnumerateArray()
            .Any(item => string.Equals(item.GetProperty("audienceId").GetString(), audienceId, StringComparison.Ordinal));

    private static async Task<JsonElement> RequireSuccessAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?> arguments)
    {
        var response = await CallRawAsync(client, toolName, arguments);
        if (!IsSuccess(response)
            && string.Equals(response.GetProperty("code").GetString(), "ChangeVersionConflict", StringComparison.Ordinal)
            && arguments.ContainsKey("expectedChangeVersion")
            && TryGetDetailsLong(response, "actualChangeVersion", out var actualChangeVersion))
        {
            arguments["expectedChangeVersion"] = actualChangeVersion;
            response = await CallRawAsync(client, toolName, arguments);
        }

        EnsureSuccess(response, toolName);
        return response;
    }

    private static async Task<JsonElement> CallAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var response = await CallRawAsync(client, toolName, arguments);
        EnsureSuccess(response, toolName);
        return response;
    }

    private static async Task<JsonElement> CallRawAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(toolName, arguments);
        using var document = JsonDocument.Parse(result.Content.Single().ToString()!);
        return document.RootElement.Clone();
    }

    private static bool IsSuccess(JsonElement response) =>
        string.Equals(response.GetProperty("code").GetString(), "Success", StringComparison.Ordinal);

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

    private static long? TryGetDataLong(JsonElement response, string propertyName) =>
        response.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Object
        && data.TryGetProperty(propertyName, out var value)
        && value.TryGetInt64(out var parsed)
            ? parsed
            : null;

    private static bool TryGetDetailsLong(JsonElement response, string propertyName, out long value)
    {
        value = 0;
        return response.TryGetProperty("details", out var details)
        && details.ValueKind == JsonValueKind.Object
        && details.TryGetProperty(propertyName, out var raw)
        && long.TryParse(raw.GetString(), out value);
    }
}
