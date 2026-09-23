using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Drafts;

[Trait("Category", "Integration")]
public sealed class DraftWorkflowSmokeTests
{
    [Fact]
    public async Task DraftCanBeOpenedDiscardedAndConfirmedAbsentAfterReload()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Guid? draftId = null;
        try
        {
            draftId = await BrowserMcpAssertions.BeginTransactionAsync(host.Address, "Draft Browser Smoke");

            await page.GotoAsync($"{host.Address}/drafts", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId($"draft-item-{draftId}")).ToBeVisibleAsync();
            await page.GetByTestId($"draft-open-{draftId}").ClickAsync();
            await Assertions.Expect(page.GetByTestId("draft-review")).ToBeVisibleAsync(new() { Timeout = 15_000 });
            await Assertions.Expect(page.GetByTestId("transaction-diff")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("validate-transaction-button")).ToBeVisibleAsync();
            await page.GetByTestId("validate-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("validation-valid")).ToBeVisibleAsync(new() { Timeout = 15_000 });
            var selectedDraftLink = page.GetByTestId("active-draft-link");
            await Assertions.Expect(selectedDraftLink).ToBeVisibleAsync();
            await Assertions.Expect(selectedDraftLink).ToHaveAttributeAsync("href", $"/drafts/{draftId:D}");

            await page.GetByTestId("discard-transaction-button").ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Entwurf verwerfen" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"{host.Address}/knowledge(?:\\?.*)?$"));
            draftId = null;

            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await page.GotoAsync($"{host.Address}/drafts", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("drafts-empty")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally
        {
            if (draftId is { } id)
                await BrowserTransactionDiscarder.DiscardAsync(host.Address, id);
        }
    }
}
