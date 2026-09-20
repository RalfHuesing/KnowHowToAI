using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Transactions;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class KnowledgeTreeMoveSmokeTests
{
    private readonly PublishedServerHost _host;

    public KnowledgeTreeMoveSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task KnowledgeTree_MoveActionButtons_PersistsTheConfirmedWorkingTree()
    {
        await RunMoveAsync(async (page, source, target) =>
        {
            await page.GetByTestId($"tree-move-source-{source}").ClickAsync();
            await page.GetByTestId($"tree-move-after-{target}").ClickAsync();
        });
    }

    [Fact]
    public async Task KnowledgeTree_DragAndDrop_PersistsTheConfirmedWorkingTree()
    {
        await RunMoveAsync(async (page, source, target) =>
        {
            var sourceNode = page.GetByTestId($"treeitem-{source}");
            var parentTarget = page.GetByTestId($"tree-move-parent-{target}");
            await sourceNode.EvaluateAsync("node => node.dispatchEvent(new DragEvent('dragstart', { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() }))");
            await Assertions.Expect(parentTarget).ToBeVisibleAsync();
            await parentTarget.EvaluateAsync("node => node.dispatchEvent(new DragEvent('dragover', { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() }))");
            await parentTarget.EvaluateAsync("node => node.dispatchEvent(new DragEvent('drop', { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() }))");
        });
    }

    private async Task RunMoveAsync(Func<IPage, string, string, Task> move)
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
            await page.GetByTestId("tx-purpose-input").FillAsync("Tree-Move-Browsertest");
            await page.GetByTestId("begin-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync();
            transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
            await page.GetByTestId("tx-open-knowledge-link").ClickAsync();
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();

            var roleSelector = page.GetByTestId("context-selector-dialog");
            await Assertions.Expect(roleSelector).ToBeVisibleAsync();
            var roleOption = roleSelector.GetByTestId("role-option-Default").GetByRole(AriaRole.Radio);
            await roleOption.CheckAsync();
            await Assertions.Expect(roleOption).ToBeCheckedAsync();
            var applyButton = roleSelector.GetByTestId("selector-apply-button");
            await Assertions.Expect(applyButton).ToBeEnabledAsync();
            await applyButton.ClickAsync();
            await Assertions.Expect(roleSelector).ToBeHiddenAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("[?&]roleId=Default(?:&|$)"));

            var root = page.GetByRole(AriaRole.Treeitem).First;
            await Assertions.Expect(root).ToBeVisibleAsync();
            await root.Locator("button.tree-toggle-btn").ClickAsync();
            var children = page.Locator("div[role='treeitem'][aria-level='2']");
            await Assertions.Expect(children.Nth(1)).ToBeVisibleAsync(new() { Timeout = 15_000 });
            var source = await children.Nth(0).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Quellknoten fehlt.");
            var target = await children.Nth(1).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Zielknoten fehlt.");

            await move(page, source, target);

            await Assertions.Expect(page.GetByTestId("tree-move-error")).ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByTestId($"treeitem-{source}")).ToBeVisibleAsync();
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId.Value);
        }
    }

}
