using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class PageFrameSmokeTests
{
    private readonly PublishedServerHost _host;

    public PageFrameSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task FeaturePagesUseAvailableFrameAndKeepResponsiveContentReachable()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        foreach (var viewport in new[] { 1280, 1920, 2560, 1024 })
        {
            await page.SetViewportSizeAsync(viewport, 720);
            foreach (var route in Routes)
            {
                var response = await page.GotoAsync(_host.Address + route.Path, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30_000
                });

                Assert.NotNull(response);
                Assert.Equal((int)HttpStatusCode.OK, response.Status);
                await CircuitProbe.WaitForInteractivityAsync(page);

                var frame = page.Locator($"[data-testid='{route.TestId}'].page-frame");
                await Assertions.Expect(frame).ToBeVisibleAsync(new() { Timeout = 30_000 });

                var metrics = await page.EvaluateAsync<double[]>(
                    """() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]""");
                Assert.True(
                    metrics[0] <= metrics[1],
                    $"{route.TestId} läuft bei {viewport} CSS-Pixeln horizontal über.");

                var readable = frame.Locator(".readable");
                if (await readable.CountAsync() > 0)
                {
                    var maxWidths = await readable.EvaluateAllAsync<string[]>(
                        "elements => elements.map(element => getComputedStyle(element).maxWidth)");
                    Assert.All(maxWidths, maxWidth => Assert.NotEqual("none", maxWidth));
                }

                var actionGroups = frame.Locator(".action-group");
                if (await actionGroups.CountAsync() > 0)
                {
                    var wrapping = await actionGroups.EvaluateAllAsync<string[]>(
                        "elements => elements.map(element => getComputedStyle(element).flexWrap)");
                    Assert.All(wrapping, value => Assert.Equal("wrap", value));
                }
            }
        }
    }

    private static readonly (string Path, string TestId)[] Routes =
    [
        ("/", "dashboard-page"),
        ("/search?audienceId=Default", "search-page"),
        ("/history?audienceId=Default", "history-page"),
        ("/transactions", "transactions-page"),
        ("/knowledge?audienceId=Default", "knowledge-page")
    ];
}
