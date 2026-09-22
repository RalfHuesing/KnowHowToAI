using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class DashboardSmokeTests
{
    private readonly PublishedServerHost _host;

    public DashboardSmokeTests(SmokeHostFixture fixture) => _host = fixture.Host;

    [Fact]
    public async Task RootRedirectsToTheKnowledgeWorkspace()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync();

        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);
        Assert.EndsWith("/knowledge", page.Url, StringComparison.Ordinal);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Wissensbasis" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("link-knowledge")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("link-drafts")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("dashboard-page")).ToHaveCountAsync(0);
    }
}
