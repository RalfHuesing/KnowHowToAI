using System.Text.Json;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;
using ModelContextProtocol.Client;

namespace KnowHowToAI.BrowserTests.Transactions;

[Trait("Category", "Integration")]
public sealed class SnapshotConflictSmokeTests
{
    [Fact]
    public async Task SnapshotConflict_ShowsComparison_StartsManualReapply_AndAllowsDiscard()
    {
        await using var host = await PublishedServerHost.StartAsync();
        await BrowserKnowledgeSeed.EnsureWorkflowAsync(host.Address);
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Guid? conflictingTransactionId = null;
        Guid? reapplyTransactionId = null;
        var conflictingTransactionDiscarded = false;
        try
        {
            conflictingTransactionId = await BrowserMcpAssertions.BeginTransactionAsync(host.Address, "Konflikt-Reapply-Browsertest");
            var baseSnapshotId = (await ReadTransactionChangesAsync(host.Address, conflictingTransactionId.Value)).BaseSnapshotId;
            await page.GotoAsync($"{host.Address}/knowledge?audienceId=Default&transactionId={conflictingTransactionId:D}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });

            var uiTitle = $"UI-Konflikt-Änderung-{Guid.NewGuid():N}";
            var uiDescription = "Explizite Working-Änderung aus der Browseroberfläche.";
            var uiNodeId = await CreateUiChildAsync(page, uiTitle, uiDescription);

            var changesBeforeConflict = await ReadTransactionChangesAsync(host.Address, conflictingTransactionId.Value);
            Assert.Equal("Open", changesBeforeConflict.State);
            Assert.Contains(changesBeforeConflict.Items, item => item.EntityType == "node" && item.Id == uiNodeId.ToString());

            var externalTitle = $"MCP-Konflikt-Änderung-{Guid.NewGuid():N}";
            var externalCommit = await CommitFromMcpAsync(host.Address, externalTitle);
            var changesAfterExternalCommit = await ReadTransactionChangesAsync(host.Address, conflictingTransactionId.Value);
            AssertTransactionChangesEqual(changesBeforeConflict, changesAfterExternalCommit);
            Assert.Equal("Open", changesAfterExternalCommit.State);
            var currentBeforeReapply = await ReadCurrentChildrenAsync(host.Address);
            Assert.Contains(currentBeforeReapply, child => child.NodeId == externalCommit.NodeId && child.Title == externalTitle);
            Assert.DoesNotContain(currentBeforeReapply, child => child.NodeId == uiNodeId && child.Title == uiTitle);
            Assert.Equal("NodeNotFound", (await ReadNodeAsync(host.Address, uiNodeId, transactionId: null)).Code);
            Assert.Equal(uiTitle, (await ReadNodeAsync(host.Address, uiNodeId, conflictingTransactionId)).Title);

            reapplyTransactionId = await TriggerConflictAndStartReapplyAsync(
                page, host.Address, conflictingTransactionId.Value, baseSnapshotId!, externalCommit.CurrentSnapshotId);
            Assert.NotEqual(conflictingTransactionId, reapplyTransactionId);
            var emptyReapplyChanges = await ReadTransactionChangesAsync(host.Address, reapplyTransactionId.Value);
            Assert.Equal("Open", emptyReapplyChanges.State);
            Assert.Equal(externalCommit.CurrentSnapshotId.ToString(), emptyReapplyChanges.BaseSnapshotId);
            Assert.Equal(0, emptyReapplyChanges.TotalCount);
            Assert.Equal("NodeNotFound", (await ReadNodeAsync(host.Address, uiNodeId, reapplyTransactionId)).Code);

            var changesAfterConflict = await ReadTransactionChangesAsync(host.Address, conflictingTransactionId.Value);
            AssertTransactionChangesEqual(changesBeforeConflict, changesAfterConflict);
            Assert.Equal("Open", changesAfterConflict.State);

            var reappliedNodeId = await ReapplyAsync(
                page, uiTitle, uiDescription);
            Assert.NotEqual(uiNodeId, reappliedNodeId);

            var nonEmptyReapplyChanges = await ReadTransactionChangesAsync(host.Address, reapplyTransactionId.Value);
            Assert.Equal("Open", nonEmptyReapplyChanges.State);
            Assert.Contains(nonEmptyReapplyChanges.Items, item => item.EntityType == "node" && item.Id == reappliedNodeId.ToString());

            await CommitReapplyAsync(page, host.Address, reapplyTransactionId.Value);

            var currentAfterReapply = await ReadCurrentChildrenAsync(host.Address);
            Assert.Contains(currentAfterReapply, child => child.NodeId == externalCommit.NodeId && child.Title == externalTitle);
            Assert.Contains(currentAfterReapply, child => child.NodeId == reappliedNodeId && child.Title == uiTitle);
            Assert.DoesNotContain(currentAfterReapply, child => child.NodeId == uiNodeId);

            await BrowserTransactionDiscarder.DiscardAsync(host.Address, conflictingTransactionId.Value);
            conflictingTransactionDiscarded = true;
            var discarded = await ReadRawAsync(host.Address, "get_transaction_changes", new Dictionary<string, object?>
            {
                ["transactionId"] = conflictingTransactionId.Value.ToString("D")
            });
            Assert.Equal("TransactionDiscarded", discarded.GetProperty("code").GetString());

        }
        finally
        {
            if (conflictingTransactionId is not null && !conflictingTransactionDiscarded)
                await BrowserTransactionDiscarder.DiscardAsync(host.Address, conflictingTransactionId.Value);
            if (reapplyTransactionId is not null)
            {
                try
                {
                    await BrowserTransactionDiscarder.DiscardAsync(host.Address, reapplyTransactionId.Value);
                }
                catch (InvalidOperationException)
                {
                    // Die Reapply-Transaction ist nach dem erfolgreichen Commit bereits geschlossen.
                }
            }
        }
    }

    private static async Task<Guid> CreateUiChildAsync(
        IPage page,
        string title,
        string description)
    {
        if (!new Uri(page.Url).AbsolutePath.Equals("/knowledge", StringComparison.OrdinalIgnoreCase))
        {
            var transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
            await page.GotoAsync($"{new Uri(page.Url).GetLeftPart(UriPartial.Authority)}/knowledge?audienceId=Default&transactionId={transactionId:D}");
            await CircuitProbe.WaitForInteractivityAsync(page);
        }
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        var rootItem = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
        await rootItem.ClickAsync();
        await page.GetByTestId("tree-toolbar-create-child").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-metadata-title")).ToBeVisibleAsync();
        await page.GetByTestId("node-metadata-title").FillAsync(title);
        await page.GetByTestId("node-metadata-description").FillAsync(description);
        await page.GetByTestId("save-node-metadata").ClickAsync();
        await Assertions.Expect(page.GetByTestId("knowledge-page").Locator("h1")).ToHaveTextAsync(title);
        return await ReadTreeNodeIdAsync(page, title);
    }

    private static async Task<Guid> TriggerConflictAndStartReapplyAsync(
        IPage page,
        string address,
        Guid conflictingTransactionId,
        string baseSnapshotId,
        long currentSnapshotId)
    {
        await page.GotoAsync($"{address}/drafts/{conflictingTransactionId:D}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("draft-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await page.GetByTestId("commit-transaction-button").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Übernehmen", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("snapshot-conflict")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("snapshot-conflict-explanation")).ToContainTextAsync(
            $"Basis-Snapshot {baseSnapshotId}");
        await Assertions.Expect(page.GetByTestId("snapshot-conflict-explanation")).ToContainTextAsync(
            $"Current Snapshot {currentSnapshotId}");
        await Assertions.Expect(page.GetByTestId("snapshot-diff")).ToBeVisibleAsync();
        var conflictedDraftUrl = page.Url;
        await page.GetByTestId("snapshot-conflict-start-reapply").ClickAsync();
        await page.WaitForFunctionAsync("previousUrl => location.href !== previousUrl", conflictedDraftUrl);
        await Assertions.Expect(page.GetByTestId("draft-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        var transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
        var transactionChanges = await ReadTransactionChangesAsync(address, transactionId);
        Assert.Equal(currentSnapshotId.ToString(), transactionChanges.BaseSnapshotId);
        return transactionId;
    }

    private static Task<Guid> ReapplyAsync(
        IPage page,
        string title,
        string description)
        => CreateUiChildAsync(page, title, description);

    private static async Task CommitReapplyAsync(IPage page, string address, Guid reapplyTransactionId)
    {
        await page.GotoAsync($"{address}/drafts/{reapplyTransactionId:D}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("draft-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await page.GetByTestId("commit-transaction-button").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Übernehmen", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    private static async Task<Guid> ReadTreeNodeIdAsync(IPage page, string title)
    {
        var titleLocator = page.GetByTestId("knowledge-tree")
            .Locator("[data-testid^='tree-title-']")
            .Filter(new LocatorFilterOptions { HasText = title });
        await Assertions.Expect(titleLocator).ToHaveCountAsync(1, new() { Timeout = 15_000 });
        var testId = await titleLocator.GetAttributeAsync("data-testid");
        return testId is not null && Guid.TryParse(testId["tree-title-".Length..], out var nodeId)
            ? nodeId
            : throw new InvalidOperationException($"Die Browseransicht enthält keine Node-ID für '{title}'.");
    }

    private static async Task<ExternalCommit> CommitFromMcpAsync(string address, string title)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var root = await CallSuccessAsync(client, "get_root", new Dictionary<string, object?>
        {
            ["audienceId"] = "Default"
        });
        var rootNodeId = root.GetProperty("data").GetProperty("nodeId").GetString()
            ?? throw new InvalidOperationException("Der MCP-Read des Root-Nodes lieferte keine Node-ID.");
        var begin = await CallSuccessAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "Paralleler MCP-Commit",
            ["actor"] = "Browser-Test",
            ["client"] = "KnowHowToAI.BrowserTests"
        });
        var transactionId = Guid.Parse(begin.GetProperty("data").GetProperty("transactionId").GetString()!);
        try
        {
            var created = await CallSuccessAsync(client, "create_node", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId.ToString("D"),
                ["title"] = title,
                ["description"] = "Fachlich unabhängige parallele MCP-Änderung.",
                ["parentNodeId"] = rootNodeId
            });
            var nodeId = Guid.Parse(created.GetProperty("data").GetProperty("nodeId").GetString()!);
            var commit = await CallSuccessAsync(client, "commit_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId.ToString("D")
            });
            var currentSnapshotId = long.Parse(commit.GetProperty("data").GetProperty("workingSnapshotId").GetString()!);
            return new ExternalCommit(currentSnapshotId, nodeId);
        }
        catch
        {
            await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId.ToString("D")
            });
            throw;
        }
    }

    private static async Task<IReadOnlyList<CurrentChild>> ReadCurrentChildrenAsync(string address)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var root = await CallSuccessAsync(client, "get_root", new Dictionary<string, object?>
        {
            ["audienceId"] = "Default"
        });
        var rootNodeId = root.GetProperty("data").GetProperty("nodeId").GetString()!;
        var children = await CallSuccessAsync(client, "list_children", new Dictionary<string, object?>
        {
            ["audienceId"] = "Default",
            ["parentNodeId"] = rootNodeId,
            ["limit"] = 100
        });
        return children.GetProperty("data").GetProperty("items").EnumerateArray()
            .Select(item => new CurrentChild(
                Guid.Parse(item.GetProperty("nodeId").GetString()!),
                item.GetProperty("title").GetString()!))
            .ToArray();
    }

    private static async Task<NodeRead> ReadNodeAsync(string address, Guid nodeId, Guid? transactionId)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var arguments = new Dictionary<string, object?>
        {
            ["nodeId"] = nodeId.ToString("D"),
            ["audienceId"] = "Default"
        };
        if (transactionId is not null)
            arguments["transactionId"] = transactionId.Value.ToString("D");

        var response = await ReadRawAsync(client, "get_node", arguments);
        var code = response.GetProperty("code").GetString()!;
        if (!string.Equals(code, "Success", StringComparison.Ordinal))
            return new NodeRead(code, null);

        return new NodeRead(code, response.GetProperty("data").GetProperty("title").GetString());
    }

    private static async Task<TransactionChanges> ReadTransactionChangesAsync(string address, Guid transactionId)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var response = await CallSuccessAsync(client, "get_transaction_changes", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId.ToString("D"),
            ["limit"] = 100
        });
        var data = response.GetProperty("data");
        var changes = data.GetProperty("changes");
        return new TransactionChanges(
            data.GetProperty("state").GetString()!,
            data.GetProperty("baseSnapshotId").GetString()!,
            data.GetProperty("workingSnapshotId").GetString()!,
            changes.GetProperty("totalCount").GetInt32(),
            changes.GetProperty("items").EnumerateArray()
                .Select(item => new DiffItem(
                    item.GetProperty("kind").GetString()!,
                    item.GetProperty("entityType").GetString()!,
                    item.GetProperty("id").GetString()!))
                .ToArray());
    }

    private static void AssertTransactionChangesEqual(TransactionChanges expected, TransactionChanges actual)
    {
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.BaseSnapshotId, actual.BaseSnapshotId);
        Assert.Equal(expected.WorkingSnapshotId, actual.WorkingSnapshotId);
        Assert.Equal(expected.TotalCount, actual.TotalCount);
        Assert.Equal(expected.Items, actual.Items);
    }

    private static async Task<JsonElement> CallSuccessAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?> arguments)
    {
        var response = await ReadRawAsync(client, toolName, arguments);
        if (!string.Equals(response.GetProperty("code").GetString(), "Success", StringComparison.Ordinal))
            throw new InvalidOperationException($"MCP-Tool '{toolName}' lieferte Code '{response.GetProperty("code").GetString()}'.");
        return response;
    }

    private static async Task<JsonElement> ReadRawAsync(
        string address,
        string toolName,
        Dictionary<string, object?> arguments)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        return await ReadRawAsync(client, toolName, arguments);
    }

    private static async Task<JsonElement> ReadRawAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(toolName, arguments);
        var payload = result.Content.FirstOrDefault()?.ToString()
            ?? throw new InvalidOperationException($"MCP-Tool '{toolName}' lieferte keine Antwort.");
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.Clone();
    }

    private sealed record ExternalCommit(long CurrentSnapshotId, Guid NodeId);

    private sealed record CurrentChild(Guid NodeId, string Title);

    private sealed record NodeRead(string Code, string? Title);

    private sealed record TransactionChanges(
        string State,
        string BaseSnapshotId,
        string WorkingSnapshotId,
        int TotalCount,
        IReadOnlyList<DiffItem> Items);

    private sealed record DiffItem(string Kind, string EntityType, string Id);
}
