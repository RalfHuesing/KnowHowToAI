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

            await page.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten", Exact = true }).ClickAsync();
            var editor = page.GetByTestId("content-editor");
            await Assertions.Expect(editor).ToBeVisibleAsync();
            var viewMode = editor.GetByTestId("content-editor-view-mode");
            var surface = editor.GetByTestId("content-editor-surface");
            await Assertions.Expect(surface).ToBeVisibleAsync();
            var surfaceBox = await surface.BoundingBoxAsync();
            var toolbarRow = editor.Locator(".content-editor__toolbar-row");
            var visualToolbarBox = await toolbarRow.BoundingBoxAsync();
            var visualBox = await viewMode.BoundingBoxAsync();
            await viewMode.SelectOptionAsync("source");
            var source = editor.GetByTestId("content-editor-source");
            await Assertions.Expect(source).ToBeVisibleAsync();
            var sourceBox = await viewMode.BoundingBoxAsync();
            await Assertions.Expect(editor.Locator(".content-editor__source-label")).ToHaveCountAsync(0);
            var sourceTextBox = await source.BoundingBoxAsync();
            var sourceToolbarBox = await toolbarRow.BoundingBoxAsync();
            Assert.NotNull(visualBox);
            Assert.NotNull(sourceBox);
            Assert.NotNull(surfaceBox);
            Assert.NotNull(sourceTextBox);
            Assert.NotNull(visualToolbarBox);
            Assert.NotNull(sourceToolbarBox);
            Assert.True(
                Math.Abs(visualBox.X - sourceBox.X) <= 15,
                $"content-editor-view-mode springt im Quellmodus nach links (Visuell: {visualBox.X}, Quelle: {sourceBox.X}).");
            var surfaceOffset = surfaceBox.Y - (visualToolbarBox.Y + visualToolbarBox.Height);
            var sourceOffset = sourceTextBox.Y - (sourceToolbarBox.Y + sourceToolbarBox.Height);
            Assert.True(
                Math.Abs(surfaceOffset - sourceOffset) <= 1.0,
                $"Abstand der Text-Box zur Toolbar unterscheidet sich (Visuell: {surfaceOffset}, Quelle: {sourceOffset}).");
            var contentPane = page.GetByTestId("knowledge-content-pane");
            var contentPaneBox = await contentPane.BoundingBoxAsync();
            var saveButton = page.GetByTestId("content-editor-save");
            var saveBox = await saveButton.BoundingBoxAsync();
            Assert.NotNull(contentPaneBox);
            Assert.NotNull(saveBox);
            var distanceToBottom = (contentPaneBox.Y + contentPaneBox.Height) - (saveBox.Y + saveBox.Height);
            Assert.True(
                distanceToBottom <= 32,
                $"Der Speichern-Button schließt nicht bündig am unteren Rand ab (Abstand zum unteren Rand: {distanceToBottom}px, erwartet <= 32px).");

            Assert.True(
                surfaceBox.Height >= 180,
                $"Die visuelle Editor-Surface füllt die Resthöhe nicht aus (Höhe: {surfaceBox.Height}px, erwartet mindestens 180px).");
            Assert.True(
                sourceTextBox.Height >= 180,
                $"Die Quelltext-Box füllt die Resthöhe nicht aus (Höhe: {sourceTextBox.Height}px, erwartet mindestens 180px).");

            const string markdown = "Formatierung.\n\n- erster Eintrag\n- zweiter Eintrag\n\n[Dokumentation](https://example.test/docs)";
            await source.FillAsync(markdown);
            await viewMode.SelectOptionAsync("visual");
            await Assertions.Expect(editor.Locator(".ProseMirror")).ToBeVisibleAsync();
            var proseMirror = editor.Locator(".ProseMirror");
            Assert.True(await page.EvaluateAsync<bool>("() => Boolean(document.activeElement?.closest('.ProseMirror'))"));
            await Assertions.Expect(editor.Locator(".milkdown-link-preview, .milkdown-link-edit")).ToHaveCountAsync(0);
            await proseMirror.Locator("p").First.SelectTextAsync();
            var bold = editor.GetByTestId("content-editor-toolbar").GetByRole(AriaRole.Button, new() { Name = "Fett" });
            await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");
            await bold.ClickAsync();
            await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(proseMirror.Locator("strong")).ToContainTextAsync("Formatierung.");
            await page.EvaluateAsync("() => { window.prompt = (_, current) => { window.lastLinkPromptValue = current; return current ? 'https://example.test/edited' : 'https://example.test/editor'; }; }");
            var link = editor.GetByTestId("content-editor-toolbar").GetByRole(AriaRole.Button, new() { Name = "Link" });
            await link.ClickAsync();
            await Assertions.Expect(proseMirror.Locator("strong a[href='https://example.test/editor']"))
                .ToContainTextAsync("Formatierung.");
            await link.ClickAsync();
            Assert.Equal("https://example.test/editor", await page.EvaluateAsync<string>("() => window.lastLinkPromptValue"));
            await Assertions.Expect(proseMirror.Locator("strong a[href='https://example.test/edited']"))
                .ToContainTextAsync("Formatierung.");
            await Assertions.Expect(editor.Locator(".milkdown-link-preview, .milkdown-link-edit")).ToHaveCountAsync(0);
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
            const string formattedMarkdown = "**[Formatierung.](https://example.test/edited)**\n\n- erster Eintrag\n- zweiter Eintrag\n\n[Dokumentation](https://example.test/docs)";
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
