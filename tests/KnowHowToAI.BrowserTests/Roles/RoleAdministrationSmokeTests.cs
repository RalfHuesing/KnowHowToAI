using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Roles;

[Trait("Category", "Integration")]
public sealed class RoleAdministrationSmokeTests
{
    [Fact]
    public async Task RoleAdministration_CreateRenameAndDelete_StaysInWorkingTransaction()
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
            await page.GetByTestId("tx-purpose-input").FillAsync("Rollenverwaltung Smoke");
            await page.GetByTestId("begin-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
            transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);

            await page.GetByTestId("tx-open-roles-link").ClickAsync();
            await CircuitProbe.WaitForInteractivityAsync(page);

            await Assertions.Expect(page.GetByTestId("roles-page")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("roles-readonly")).ToBeHiddenAsync();
            await page.GetByTestId("role-create-name").FillAsync("SmokeRole");
            await page.GetByTestId("role-create-description").FillAsync("Rolle für den Browser-Smoke");
            await page.GetByTestId("role-create-submit").ClickAsync();
            await Assertions.Expect(page.GetByTestId("role-item-SmokeRole")).ToBeVisibleAsync();

            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("role-item-SmokeRole")).ToBeVisibleAsync();

            var reconnectDialog = page.Locator("#components-reconnect-modal");
            await page.Context.SetOfflineAsync(true);
            await Assertions.Expect(reconnectDialog).ToHaveAttributeAsync("open", "", new() { Timeout = 60_000 });
            await page.Context.SetOfflineAsync(false);
            await Assertions.Expect(reconnectDialog).ToBeHiddenAsync(new() { Timeout = 30_000 });
            await Assertions.Expect(page.GetByTestId("role-item-SmokeRole")).ToBeVisibleAsync();

            await page.GetByTestId("role-edit-SmokeRole").ClickAsync();
            await page.GetByTestId("role-edit-name-SmokeRole").FillAsync("Umbenannte Smoke-Rolle");
            await page.GetByTestId("role-save-SmokeRole").ClickAsync();
            await Assertions.Expect(page.GetByText("Umbenannte Smoke-Rolle", new() { Exact = true })).ToBeVisibleAsync();

            await page.GetByTestId("role-delete-SmokeRole").ClickAsync();
            var deleteConfirmation = page.GetByTestId("role-delete-confirmation");
            await Assertions.Expect(deleteConfirmation).ToBeVisibleAsync();
            await page.GetByTestId("role-delete-confirm").ClickAsync();
            await Assertions.Expect(page.GetByTestId("role-item-SmokeRole")).ToBeHiddenAsync();
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(host.Address, transactionId.Value);
        }
    }
}
