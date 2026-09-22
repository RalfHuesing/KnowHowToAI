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
        await using var browser = await ChromeBrowser.LaunchAsync();
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
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Wissensbasis" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("shell-root")).ToHaveAttributeAsync("data-ktai-interactive", "true");
        await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "false");

        Assert.NotEmpty(observedRequests);
        Assert.All(observedRequests, request => Assert.StartsWith(_host.Address, request, StringComparison.OrdinalIgnoreCase));
    }
}
