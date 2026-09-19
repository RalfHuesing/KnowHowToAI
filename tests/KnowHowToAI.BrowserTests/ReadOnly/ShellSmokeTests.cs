using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class ShellSmokeTests
{
    private readonly PublishedServerHost _host;

    public ShellSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task RootShell_UsesOneInteractiveCircuitWithoutServerLoopback()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "chrome",
            Headless = true
        });
        var page = await browser.NewPageAsync();
        var observedRequests = new List<string>();
        page.Request += (_, request) => observedRequests.Add(request.Url);

        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        Assert.Contains("text/html", response.Headers["content-type"], StringComparison.OrdinalIgnoreCase);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "KnowHowToAI" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("shell-status")).ToContainTextAsync("Shell bereit");
        // Der Klick kann ankommen, bevor der Circuit das Ereignis verdrahtet
        // hat (Warmup nach dem Serverstart). Deshalb klicken wir erneut, bis
        // der beobachtbare Statuswechsel die Interaktivität belegt.
        var interactionStatus = page.GetByTestId("interaction-status");
        for (var attempt = 1; ; attempt++)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Interaktivität prüfen" }).ClickAsync();
            try
            {
                await Assertions.Expect(interactionStatus).ToHaveTextAsync(
                    "Interaktivität ist verfügbar.",
                    new() { Timeout = 2_000 });
                break;
            }
            catch (PlaywrightException) when (attempt < 10)
            {
                // Circuit noch nicht verbunden; erneut klicken.
            }
        }

        Assert.NotEmpty(observedRequests);
        Assert.All(observedRequests, request => Assert.StartsWith(_host.Address, request, StringComparison.OrdinalIgnoreCase));
    }
}
