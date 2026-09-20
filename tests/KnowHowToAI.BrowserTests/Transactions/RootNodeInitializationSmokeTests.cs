using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Transactions;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class RootNodeInitializationSmokeTests
{
    private readonly PublishedServerHost _host;

    public RootNodeInitializationSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task KnowledgePage_EmptyWorkingTree_CreatesAndSelectsItsInitialRootNode()
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

            var roleSelector = page.GetByTestId("context-selector-dialog");
            await Assertions.Expect(roleSelector).ToBeVisibleAsync();
            var roleOption = roleSelector.GetByTestId("role-option-Default").GetByRole(AriaRole.Radio);
            await roleOption.CheckAsync();
            await roleSelector.GetByTestId("selector-apply-button").ClickAsync();
            await Assertions.Expect(roleSelector).ToBeHiddenAsync();

            await page.GetByRole(AriaRole.Treeitem).First.Locator(".tree-node-title").ClickAsync();
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
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId.Value);
        }
    }
}
