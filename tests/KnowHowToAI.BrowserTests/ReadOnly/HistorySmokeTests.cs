using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class HistorySmokeTests
{
    private readonly PublishedServerHost _host;

    public HistorySmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task History_RendersImmutableSnapshotAndReleaseOverviews()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();

        var response = await page.GotoAsync($"{_host.Address}/history", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);

        await Assertions.Expect(page.GetByTestId("history-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("history-working-separation")).ToContainTextAsync("Working Transactions gehören nicht");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Committed Snapshots" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Releases", Exact = true })).ToBeVisibleAsync();
    }
}
