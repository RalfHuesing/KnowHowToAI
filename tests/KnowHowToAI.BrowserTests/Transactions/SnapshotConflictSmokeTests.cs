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
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Guid? conflictingTransactionId = null;
        try
        {
            await page.GotoAsync($"{host.Address}/transactions", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await page.GetByTestId("tx-purpose-input").FillAsync("Konflikt-Reapply-Browsertest");
            await page.GetByTestId("begin-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
            conflictingTransactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
            var baseSnapshotId = await page.GetByTestId("tx-base-snapshot").TextContentAsync();

            var externalCommit = await CommitFromMcpAsync(host.Address);

            await page.GetByTestId("commit-transaction-button").ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Commit ausführen", Exact = true }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("snapshot-conflict")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("snapshot-conflict-explanation")).ToContainTextAsync(
                $"Base-Snapshot {baseSnapshotId}");
            await Assertions.Expect(page.GetByTestId("snapshot-conflict-explanation")).ToContainTextAsync(
                $"Current Snapshot {externalCommit.CurrentSnapshotId}");
            await Assertions.Expect(page.GetByTestId("snapshot-diff")).ToBeVisibleAsync();

            await page.GetByTestId("snapshot-conflict-start-reapply").ClickAsync();
            await Assertions.Expect(page.GetByTestId("tx-base-snapshot")).ToHaveTextAsync(
                externalCommit.CurrentSnapshotId.ToString(), new() { Timeout = 15_000 });
            var reapplyTransactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
            Assert.NotEqual(conflictingTransactionId, reapplyTransactionId);

            await page.GetByTestId("discard-transaction-button").ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Transaction verwerfen", Exact = true }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally
        {
            if (conflictingTransactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(host.Address, conflictingTransactionId.Value);
        }
    }

    private static async Task<ExternalCommit> CommitFromMcpAsync(string address)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var begin = await client.CallToolAsync("begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "Paralleler MCP-Commit",
            ["actor"] = "Browser-Test",
            ["client"] = "KnowHowToAI.BrowserTests"
        });
        using var beginDocument = JsonDocument.Parse(begin.Content.Single().ToString()!);
        var transactionId = Guid.Parse(beginDocument.RootElement.GetProperty("data").GetProperty("transactionId").GetString()!);

        var commit = await client.CallToolAsync("commit_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId.ToString("D")
        });
        using var commitDocument = JsonDocument.Parse(commit.Content.Single().ToString()!);
        var currentSnapshotId = long.Parse(commitDocument.RootElement.GetProperty("data").GetProperty("workingSnapshotId").GetString()!);
        return new ExternalCommit(currentSnapshotId);
    }

    private sealed record ExternalCommit(long CurrentSnapshotId);
}
