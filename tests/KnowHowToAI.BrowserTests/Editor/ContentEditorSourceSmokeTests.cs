using KnowHowToAI.BrowserTests.TestSupport;
using Markdig;
using Microsoft.Playwright;
using ModelContextProtocol.Client;

namespace KnowHowToAI.BrowserTests.Editor;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class ContentEditorSourceSmokeTests
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
    private readonly SmokeHostFixture _fixture;

    public ContentEditorSourceSmokeTests(SmokeHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ContentEditor_SourceModeRoundTripsValidMarkdownThroughTheSameSavePath()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{_fixture.Host.Address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));

        var transaction = await BrowserMcpAssertions.CallAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "M5.2-T3 Markdown-Quellmodus"
        });
        var transactionId = BrowserMcpAssertions.RequiredString(transaction, "transactionId");
        try
        {
            var root = await BrowserMcpAssertions.CallAsync(client, "get_root", new Dictionary<string, object?> { ["audienceId"] = "Default" });
            var nodeId = BrowserMcpAssertions.RequiredString(root, "nodeId");
            await using var page = await browser.NewPageAsync();
            await page.GotoAsync(
                $"{_fixture.Host.Address}/knowledge/{nodeId}?audienceId=Default&transactionId={transactionId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
            await CircuitProbe.WaitForInteractivityAsync(page);

            await page.GetByTestId("tab-Editor").ClickAsync();
            var editor = page.GetByTestId("content-editor");
            await Assertions.Expect(editor).ToBeVisibleAsync();
            await editor.GetByTestId("content-editor-view-mode").SelectOptionAsync("source");
            var source = editor.GetByTestId("content-editor-source");
            await Assertions.Expect(source).ToBeVisibleAsync();

            const string markdown = "Formatierung.\n\n- erster Eintrag\n- zweiter Eintrag\n\n[Dokumentation](https://example.test/docs)";
            await source.FillAsync(markdown);
            await editor.GetByTestId("content-editor-view-mode").SelectOptionAsync("visual");
            await Assertions.Expect(editor.Locator(".ProseMirror")).ToBeVisibleAsync();
            var proseMirror = editor.Locator(".ProseMirror");
            await proseMirror.Locator("p").First.SelectTextAsync();
            var bold = editor.GetByTestId("content-editor-toolbar").GetByRole(AriaRole.Button, new() { Name = "Fett" });
            await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");
            await bold.ClickAsync();
            await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(proseMirror.Locator("strong")).ToContainTextAsync("Formatierung.");
            await editor.GetByTestId("content-editor-save").ClickAsync();
            await Assertions.Expect(editor.GetByTestId("content-editor-save-status"))
                .ToContainTextAsync("Gespeichert", new() { Timeout = 15_000 });

            var readback = await BrowserMcpAssertions.CallAsync(client, "get_node", new Dictionary<string, object?>
            {
                ["nodeId"] = nodeId,
                ["audienceId"] = "Default",
                ["transactionId"] = transactionId
            });
            var saved = readback.GetProperty("data").GetProperty("content").GetString();
            Assert.NotNull(saved);
            const string formattedMarkdown = "**Formatierung.**\n\n- erster Eintrag\n- zweiter Eintrag\n\n[Dokumentation](https://example.test/docs)";
            Assert.Equal(Markdown.ToHtml(formattedMarkdown, MarkdownPipeline), Markdown.ToHtml(saved!, MarkdownPipeline));
        }
        finally
        {
            await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId
            });
        }
    }

}
