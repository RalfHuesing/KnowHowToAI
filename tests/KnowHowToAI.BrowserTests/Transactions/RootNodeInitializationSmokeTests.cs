using KnowHowToAI.BrowserTests.TestSupport;
using ModelContextProtocol.Client;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Transactions;

[Collection("RootNode-Host")]
[Trait("Category", "Integration")]
public sealed class RootNodeInitializationSmokeTests
{
    private readonly PublishedServerHost _host;

    public RootNodeInitializationSmokeTests(RootNodeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task KnowledgePage_EmptyWorkingTree_CreatesEditsAndSelectsItsInitialRootNode()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Guid? transactionId = null;
        try
        {
            await DeleteCurrentRootAsync(_host.Address);
            await page.GotoAsync($"{_host.Address}/knowledge?audienceId=Default", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();

            await Assertions.Expect(page.GetByTestId("tree-empty")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("root-node-editor")).ToBeVisibleAsync();
            await page.GetByTestId("root-node-title").FillAsync("Erstes Browser-Wissen");
            await page.GetByTestId("root-node-description").FillAsync("Initial über die Weboberfläche angelegt.");
            await page.GetByTestId("create-root-node").ClickAsync();
            var root = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
            await Assertions.Expect(root).ToContainTextAsync("Erstes Browser-Wissen");
            var transactionIdValue = new Uri(page.Url).Query
                .TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(parameter => parameter.Split('=', 2))
                .Where(parts => parts.Length == 2 && parts[0] == "transactionId")
                .Select(parts => Uri.UnescapeDataString(parts[1]))
                .SingleOrDefault();
            Assert.False(string.IsNullOrWhiteSpace(transactionIdValue), page.Url);
            Assert.True(Guid.TryParse(transactionIdValue, out var parsedTransactionId), page.Url);
            transactionId = parsedTransactionId;
            await Assertions.Expect(root).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Erstes Browser-Wissen", Exact = true, Level = 1 })).ToBeVisibleAsync();
            await page.GetByTestId("tab-Metadata").ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-metadata-description")).ToHaveValueAsync("Initial über die Weboberfläche angelegt.");

            await page.GetByTestId("node-metadata-title").FillAsync("Aktualisiertes Browser-Wissen");
            await page.GetByTestId("node-metadata-description").FillAsync("Über die Weboberfläche aktualisiert.");
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "true");

            await page.GetByTestId("link-drafts").ClickAsync();
            var navigationConfirmation = page.GetByRole(AriaRole.Dialog, new() { Name = "Ungespeicherte Änderungen" });
            await Assertions.Expect(navigationConfirmation).ToBeVisibleAsync();
            await navigationConfirmation.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-metadata-title")).ToHaveValueAsync("Aktualisiertes Browser-Wissen");
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "true");

            await page.GetByTestId("save-node-metadata").ClickAsync();

            await Assertions.Expect(root).ToContainTextAsync("Aktualisiertes Browser-Wissen");
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Aktualisiertes Browser-Wissen", Exact = true, Level = 1 })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("node-metadata-description")).ToHaveValueAsync("Über die Weboberfläche aktualisiert.");
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "false");
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId.Value);
        }
    }

    private static async Task DeleteCurrentRootAsync(string address)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var root = await BrowserMcpAssertions.CallAsync(client, "get_root", new Dictionary<string, object?>
        {
            ["audienceId"] = "Default"
        });
        var rootNodeId = BrowserMcpAssertions.RequiredString(root, "nodeId");
        var transaction = await BrowserMcpAssertions.CallAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "Browser-Test: leere Wissensbasis vorbereiten",
            ["client"] = "KnowHowToAI.BrowserTests"
        });
        var transactionId = BrowserMcpAssertions.RequiredString(transaction, "transactionId");
        await BrowserMcpAssertions.CallAsync(client, "delete_node", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId,
            ["nodeId"] = rootNodeId,
            ["deleteSubtree"] = true
        });
        await BrowserMcpAssertions.CallAsync(client, "commit_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId
        });
    }
}
