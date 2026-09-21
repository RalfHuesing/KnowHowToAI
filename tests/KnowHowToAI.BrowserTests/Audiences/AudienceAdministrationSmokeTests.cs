using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Audiences;

[Trait("Category", "Integration")]
public sealed class AudienceAdministrationSmokeTests
{
    [Fact]
    public async Task AudienceAdministration_CreateRenameAndDelete_StaysInWorkingTransaction()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Guid? transactionId = null;
        try
        {
            await page.GotoAsync($"{host.Address}/transactions", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await page.GetByTestId("tx-purpose-input").FillAsync("Zielgruppenverwaltung Smoke");
            await page.GetByTestId("begin-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
            transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);

            await page.GetByTestId("tx-open-audiences-link").ClickAsync();
            await CircuitProbe.WaitForInteractivityAsync(page);

            await Assertions.Expect(page.GetByTestId("audiences-page")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("audiences-readonly")).ToBeHiddenAsync();
            await page.GetByTestId("audience-create-name").FillAsync("SmokeAudience");
            await page.GetByTestId("audience-create-description").FillAsync("Zielgruppe für den Browser-Smoke");
            await page.GetByTestId("audience-create-submit").ClickAsync();
            await Assertions.Expect(page.GetByTestId("audience-item-SmokeAudience")).ToBeVisibleAsync();

            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("audience-item-SmokeAudience")).ToBeVisibleAsync();

            var reconnectDialog = page.Locator("#components-reconnect-modal");
            await page.Context.SetOfflineAsync(true);
            await Assertions.Expect(reconnectDialog).ToHaveAttributeAsync("open", "", new() { Timeout = 60_000 });
            await page.Context.SetOfflineAsync(false);
            await Assertions.Expect(reconnectDialog).ToBeHiddenAsync(new() { Timeout = 30_000 });
            await Assertions.Expect(page.GetByTestId("audience-item-SmokeAudience")).ToBeVisibleAsync();

            await page.GetByTestId("audience-edit-SmokeAudience").ClickAsync();
            await page.GetByTestId("audience-edit-name-SmokeAudience").FillAsync("Umbenannte Smoke-Zielgruppe");
            await page.GetByTestId("audience-save-SmokeAudience").ClickAsync();
            await Assertions.Expect(page.GetByText("Umbenannte Smoke-Zielgruppe", new() { Exact = true })).ToBeVisibleAsync();

            await page.GetByTestId("audience-delete-SmokeAudience").ClickAsync();
            var deleteDialog = page.GetByRole(AriaRole.Dialog);
            await Assertions.Expect(deleteDialog).ToBeVisibleAsync();
            await Assertions.Expect(deleteDialog.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" })).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(deleteDialog).ToBeHiddenAsync();
            await Assertions.Expect(page.GetByTestId("audience-delete-SmokeAudience")).ToBeFocusedAsync();

            await page.GetByTestId("audience-delete-SmokeAudience").ClickAsync();
            await Assertions.Expect(deleteDialog).ToBeVisibleAsync();
            await deleteDialog.GetByRole(AriaRole.Button, new() { Name = "Zielgruppe löschen" }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("audience-item-SmokeAudience")).ToBeHiddenAsync();
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(host.Address, transactionId.Value);
        }
    }
}
