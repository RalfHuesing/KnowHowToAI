using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class MarkdownDownloadSmokeTests
{
    private readonly PublishedServerHost _host;

    public MarkdownDownloadSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task KnowledgeNode_OffersMarkdownDownloadFromTheSelectedAudienceAndContext()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();

        var response = await page.GotoAsync($"{_host.Address}/knowledge", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);

        var audienceSelector = page.GetByTestId("context-selector-dialog");
        await Assertions.Expect(audienceSelector).ToBeVisibleAsync();
        await audienceSelector.GetByTestId("audience-option-BrowserDownloadAudience").GetByRole(AriaRole.Radio).CheckAsync();
        await audienceSelector.GetByTestId("selector-apply-button").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"audienceId=BrowserDownloadAudience"));

        var tree = page.GetByTestId("knowledge-tree");
        await Assertions.Expect(tree).ToBeVisibleAsync();

        var rootNode = page.GetByRole(AriaRole.Treeitem).First;
        await Assertions.Expect(rootNode).ToBeVisibleAsync();
        await rootNode.Locator("button.tree-toggle-btn").ClickAsync();

        var exportNode = page.GetByRole(AriaRole.Treeitem, new() { Name = BrowserKnowledgeSeed.ExportNodeTitle, Exact = true });
        await Assertions.Expect(exportNode).ToBeVisibleAsync();
        await exportNode.ClickAsync();

        var downloadLink = page.GetByTestId("node-details-markdown-download");
        await Assertions.Expect(downloadLink).ToBeVisibleAsync();
        var downloadTask = page.WaitForDownloadAsync();
        var downloadResponseTask = page.WaitForResponseAsync(candidate =>
            candidate.Url.Contains("/downloads/markdown", StringComparison.Ordinal));
        await downloadLink.ClickAsync();

        var download = await downloadTask;
        var downloadResponse = await downloadResponseTask;
        Assert.Equal((int)HttpStatusCode.OK, downloadResponse.Status);
        Assert.Equal("text/markdown; charset=utf-8", downloadResponse.Headers["content-type"]);
        Assert.Contains("attachment", downloadResponse.Headers["content-disposition"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("no-store", downloadResponse.Headers["cache-control"]);
        Assert.Equal("Browser-Export-Teilbaum-BrowserDownloadAudience.md", download.SuggestedFilename);
        await using var content = await download.CreateReadStreamAsync();
        using var reader = new StreamReader(content, Encoding.UTF8);
        Assert.Equal(
            "# Browser-Export-Teilbaum\n\nBrowser-Testinhalt für den Markdown-Download.\n",
            await reader.ReadToEndAsync());
    }
}
