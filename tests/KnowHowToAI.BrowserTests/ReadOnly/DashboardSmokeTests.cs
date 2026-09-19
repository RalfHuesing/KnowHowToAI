using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class DashboardSmokeTests
{
    private readonly PublishedServerHost _host;

    public DashboardSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task Dashboard_RendersAllSectionsAndNavigationLinks()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();

        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        await CircuitProbe.WaitForInteractivityAsync(page);

        // Prüfe Hauptüberschrift, Badge und Dashboard-Container
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "KnowHowToAI" })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".dashboard-badge")).ToContainTextAsync("Wissensdashboard");
        await Assertions.Expect(page.GetByTestId("dashboard-page")).ToBeVisibleAsync();

        // Prüfe alle vier fachlichen Sektionen des Dashboards
        await Assertions.Expect(page.GetByTestId("snapshot-summary")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("open-transactions")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("quality-summary")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("recent-changes")).ToBeVisibleAsync();

        // Prüfe direkte Einstiege in Wissensbaum und Historie
        var knowledgeLink = page.GetByTestId("link-knowledge");
        await Assertions.Expect(knowledgeLink).ToBeVisibleAsync();
        await Assertions.Expect(knowledgeLink).ToHaveAttributeAsync("href", "/knowledge");

        var historyLink = page.GetByTestId("link-history");
        await Assertions.Expect(historyLink).ToBeVisibleAsync();
        await Assertions.Expect(historyLink).ToHaveAttributeAsync("href", "/history");
    }
}
