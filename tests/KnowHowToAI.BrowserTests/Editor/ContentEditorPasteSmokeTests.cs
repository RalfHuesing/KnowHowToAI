using KnowHowToAI.BrowserTests.TestSupport;
using Markdig;
using Microsoft.Playwright;
using ModelContextProtocol.Client;

namespace KnowHowToAI.BrowserTests.Editor;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class ContentEditorPasteSmokeTests
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
    private readonly SmokeHostFixture _fixture;

    public ContentEditorPasteSmokeTests(SmokeHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ContentEditor_ReducesUnsafePasteAndNeverRequestsExternalImage()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{_fixture.Host.Address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));

        var transaction = await BrowserMcpAssertions.CallAsync(client, "begin_transaction");
        var transactionId = BrowserMcpAssertions.RequiredString(transaction, "transactionId");
        try
        {
            var root = await BrowserMcpAssertions.CallAsync(client, "get_root", new Dictionary<string, object?> { ["audienceId"] = "Default" });
            var rootNodeId = BrowserMcpAssertions.RequiredString(root, "nodeId");
            await using var page = await browser.NewPageAsync();
            var externalRequestObserved = false;
            await page.RouteAsync("https://example.test/**", route =>
            {
                externalRequestObserved = true;
                return route.AbortAsync();
            });
            await page.GotoAsync(
                $"{_fixture.Host.Address}/knowledge/{rootNodeId}?audienceId=Default&transactionId={transactionId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await page.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten", Exact = true }).ClickAsync();
            var editor = page.GetByTestId("content-editor");
            await Assertions.Expect(editor).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("content-editor-surface"))
                .ToHaveAttributeAsync("data-paste-policy", "active", new() { Timeout = 15_000 });

            await page.EvaluateAsync(
                """
                () => {
                    const surface = document.querySelector('[data-testid="content-editor-surface"]');
                    const data = new DataTransfer();
                    data.setData('text/html', '<h2>Überschrift</h2><p><strong>Erlaubt</strong></p><img src="https://example.test/remote.png"><a href="javascript:alert(1)">Link</a>');
                    data.setData('text/plain', 'Überschrift Erlaubt Link');
                    surface.dispatchEvent(new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: data }));
                }
                """);

            await Assertions.Expect(editor.GetByTestId("content-editor-paste-warning"))
                .ToContainTextAsync("reduziert");
            Assert.False(externalRequestObserved);
        }
        finally
        {
            await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId
            });
        }
    }

    [Fact]
    public async Task ContentEditor_RoundTripsGoldenMasterThroughRealCrepeForFiveCycles()
    {
        var markdown = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "GoldenMaster.md"));
        Assert.True(markdown.Length >= 4096);
        var baseline = SemanticProjection(markdown);

        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{_fixture.Host.Address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));

        var transaction = await BrowserMcpAssertions.CallAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "M5.2-T2 Golden-Master-Roundtrip"
        });
        var transactionId = BrowserMcpAssertions.RequiredString(transaction, "transactionId");
        try
        {
            var root = await BrowserMcpAssertions.CallAsync(client, "get_root", new Dictionary<string, object?> { ["audienceId"] = "Default" });
            var rootNodeId = BrowserMcpAssertions.RequiredString(root, "nodeId");
            var created = await BrowserMcpAssertions.CallAsync(client, "create_node", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId,
                ["title"] = "Golden-Master Browser Roundtrip",
                ["parentNodeId"] = rootNodeId,
                ["contentMd"] = markdown,
                ["audienceId"] = "Default"
            });
            var nodeId = BrowserMcpAssertions.RequiredString(created, "nodeId");
            var url = $"{_fixture.Host.Address}/knowledge/{nodeId}?audienceId=Default&transactionId={transactionId}";
            await using var page = await browser.NewPageAsync();

            for (var cycle = 1; cycle <= 5; cycle++)
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30_000
                });
                await CircuitProbe.WaitForInteractivityAsync(page);
                await page.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten", Exact = true }).ClickAsync();
                var editor = page.GetByTestId("content-editor");
                await Assertions.Expect(editor).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByTestId("content-editor-surface"))
                    .ToHaveAttributeAsync("data-paste-policy", "active", new() { Timeout = 15_000 });
                await Assertions.Expect(editor.Locator(".ProseMirror")).ToBeVisibleAsync(new() { Timeout = 15_000 });

                var save = editor.GetByTestId("content-editor-save");
                await Assertions.Expect(save).ToBeEnabledAsync();
                await save.ClickAsync();
                await Assertions.Expect(editor.GetByRole(AriaRole.Status))
                    .ToContainTextAsync("Gespeichert", new() { Timeout = 15_000 });

                var readback = await BrowserMcpAssertions.CallAsync(client, "get_node", new Dictionary<string, object?>
                {
                    ["nodeId"] = nodeId,
                    ["audienceId"] = "Default",
                    ["transactionId"] = transactionId
                });
                var actualMarkdown = readback.GetProperty("data").GetProperty("content").GetString();
                Assert.NotNull(actualMarkdown);
                Assert.Equal(baseline, SemanticProjection(actualMarkdown!));
            }
        }
        finally
        {
            await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId
            });
        }
    }

    private static string SemanticProjection(string markdown) => Markdown.ToHtml(markdown, MarkdownPipeline);
}
