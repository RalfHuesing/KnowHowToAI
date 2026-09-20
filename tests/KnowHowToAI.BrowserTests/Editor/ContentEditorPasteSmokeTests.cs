using System.Text.Json;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;
using ModelContextProtocol.Client;

namespace KnowHowToAI.BrowserTests.Editor;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class ContentEditorPasteSmokeTests
{
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

        var transaction = await CallAsync(client, "begin_transaction");
        var transactionId = RequiredString(transaction, "transactionId");
        try
        {
            var root = await CallAsync(client, "get_root", new Dictionary<string, object?> { ["roleId"] = "Default" });
            var rootNodeId = RequiredString(root, "nodeId");
            await using var page = await browser.NewPageAsync();
            var externalRequestObserved = false;
            await page.RouteAsync("https://example.test/**", route =>
            {
                externalRequestObserved = true;
                return route.AbortAsync();
            });
            await page.GotoAsync(
                $"{_fixture.Host.Address}/knowledge/{rootNodeId}?roleId=Default&transactionId={transactionId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
            await CircuitProbe.WaitForInteractivityAsync(page);
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

            await Assertions.Expect(editor.GetByRole(AriaRole.Status))
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

    private static async Task<JsonElement> CallAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(toolName, arguments);
        using var document = JsonDocument.Parse(result.Content.Single().ToString()!);
        var response = document.RootElement.Clone();
        Assert.Equal("Success", response.GetProperty("code").GetString());
        return response;
    }

    private static string RequiredString(JsonElement response, string propertyName) =>
        response.GetProperty("data").GetProperty(propertyName).GetString()
        ?? throw new InvalidOperationException($"MCP-Feld '{propertyName}' fehlt.");
}
