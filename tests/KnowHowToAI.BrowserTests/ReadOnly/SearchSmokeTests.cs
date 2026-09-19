using System.Net;
using System.Text.RegularExpressions;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class SearchSmokeTests
{
    private readonly PublishedServerHost _host;

    public SearchSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task Search_RendersAndExecutesAgainstTheSelectedRoleAndContext()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();

        var response = await page.GotoAsync($"{_host.Address}/search", new PageGotoOptions
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
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/search\?roleId=Default"));

        await Assertions.Expect(page.GetByTestId("search-page")).ToBeVisibleAsync();
        await page.GetByTestId("search-text").FillAsync("TODO");
        await page.GetByTestId("search-submit").ClickAsync();

        var results = page.GetByTestId("search-results");
        var empty = page.GetByTestId("search-empty");
        if (await results.CountAsync() > 0)
        {
            await Assertions.Expect(results).ToBeVisibleAsync();
            await Assertions.Expect(results.Locator(".search-results__breadcrumb").First).ToBeVisibleAsync();
        }
        else
        {
            await Assertions.Expect(empty).ToBeVisibleAsync();
        }
    }
}
