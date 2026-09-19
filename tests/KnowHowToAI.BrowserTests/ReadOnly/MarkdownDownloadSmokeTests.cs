using System.Net;
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
    public async Task KnowledgeNode_OffersMarkdownDownloadFromTheSelectedRoleAndContext()
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

        var roleSelector = page.GetByTestId("context-selector-dialog");
        await Assertions.Expect(roleSelector).ToBeVisibleAsync();
        await roleSelector.GetByTestId("role-option-Default").GetByRole(AriaRole.Radio).CheckAsync();
        await roleSelector.GetByTestId("selector-apply-button").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"roleId=Default"));

        var tree = page.GetByTestId("knowledge-tree");
        if (await tree.CountAsync() == 0)
        {
            await Assertions.Expect(page.GetByTestId("tree-empty")).ToBeVisibleAsync();
            return;
        }

        var rootNode = page.GetByRole(AriaRole.Treeitem).First;
        await rootNode.ClickAsync();

        var downloadLink = page.GetByTestId("node-details-markdown-download");
        await Assertions.Expect(downloadLink).ToBeVisibleAsync();
        var downloadTask = page.WaitForDownloadAsync();
        var downloadResponseTask = page.WaitForResponseAsync(candidate =>
            candidate.Url.Contains("/downloads/markdown", StringComparison.Ordinal));
        await downloadLink.ClickAsync();

        var download = await downloadTask;
        var downloadResponse = await downloadResponseTask;
        Assert.Equal((int)HttpStatusCode.OK, downloadResponse.Status);
        Assert.StartsWith("text/markdown", downloadResponse.Headers["content-type"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("attachment", downloadResponse.Headers["content-disposition"], StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".md", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
    }
}
