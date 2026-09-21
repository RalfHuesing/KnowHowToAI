using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Transactions;

[Collection("RootNode-Host")]
[Trait("Category", "Integration")]
public sealed class RootNodeInitializationSmokeTests
{
    private readonly PublishedServerHost _host;

    public RootNodeInitializationSmokeTests(RootNodeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task KnowledgePage_EmptyWorkingTree_CreatesEditsAndSelectsItsInitialRootNode()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Guid? transactionId = null;
        try
        {
            await page.GotoAsync($"{_host.Address}/transactions", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await page.GetByTestId("tx-purpose-input").FillAsync("Root-Initialanlage-Browsertest");
            await page.GetByTestId("begin-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync();
            transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
            await page.GetByTestId("tx-open-knowledge-link").ClickAsync();
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();

            var audienceSelector = page.GetByTestId("context-selector-dialog");
            await Assertions.Expect(audienceSelector).ToBeVisibleAsync();
            var audienceOption = audienceSelector.GetByTestId("audience-option-Default").GetByRole(AriaRole.Radio);
            await audienceOption.CheckAsync();
            await audienceSelector.GetByTestId("selector-apply-button").ClickAsync();
            await Assertions.Expect(audienceSelector).ToBeHiddenAsync();

            await page.GetByRole(AriaRole.Treeitem).First.Locator(".tree-node-title").ClickAsync();
            await page.GetByText("Weitere Arbeitsbereich-Aktionen", new() { Exact = true }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("delete-node")).ToBeVisibleAsync();
            await page.GetByTestId("delete-node").ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-deletion-preview")).ToBeVisibleAsync();
            await page.GetByTestId("delete-subtree").CheckAsync();
            await page.GetByTestId("confirm-delete-node").ClickAsync();
            var confirmation = page.GetByRole(AriaRole.Dialog, new() { Name = "Node löschen" });
            await Assertions.Expect(confirmation).ToBeVisibleAsync();
            await confirmation.GetByRole(AriaRole.Button, new() { Name = "Endgültig löschen" }).ClickAsync();

            await Assertions.Expect(page.GetByTestId("tree-empty")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("root-node-editor")).ToBeVisibleAsync();
            await page.GetByTestId("root-node-title").FillAsync("Erstes Browser-Wissen");
            await page.GetByTestId("root-node-description").FillAsync("Initial über die Weboberfläche angelegt.");
            await page.GetByTestId("create-root-node").ClickAsync();

            var root = page.GetByRole(AriaRole.Treeitem).First;
            await Assertions.Expect(root).ToContainTextAsync("Erstes Browser-Wissen");
            await Assertions.Expect(root).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(page.GetByTestId("node-details-title")).ToHaveTextAsync("Erstes Browser-Wissen");
            await Assertions.Expect(page.GetByTestId("node-details-description")).ToHaveTextAsync("Initial über die Weboberfläche angelegt.");

            await page.GetByTestId("edit-node-metadata").ClickAsync();
            await page.GetByTestId("node-metadata-title").FillAsync("Aktualisiertes Browser-Wissen");
            await page.GetByTestId("node-metadata-description").FillAsync("Über die Weboberfläche aktualisiert.");
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "true");

            await page.GetByTestId("link-transactions").ClickAsync();
            var navigationConfirmation = page.GetByRole(AriaRole.Dialog, new() { Name = "Ungespeicherte Änderungen" });
            await Assertions.Expect(navigationConfirmation).ToBeVisibleAsync();
            await navigationConfirmation.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-metadata-title")).ToHaveValueAsync("Aktualisiertes Browser-Wissen");
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "true");

            await page.GetByTestId("save-node-metadata").ClickAsync();

            await Assertions.Expect(root).ToContainTextAsync("Aktualisiertes Browser-Wissen");
            await Assertions.Expect(page.GetByTestId("node-details-title")).ToHaveTextAsync("Aktualisiertes Browser-Wissen");
            await Assertions.Expect(page.GetByTestId("node-details-description")).ToHaveTextAsync("Über die Weboberfläche aktualisiert.");
            await Assertions.Expect(page.GetByTestId("edit-node-metadata")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "false");
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId.Value);
        }
    }
}
