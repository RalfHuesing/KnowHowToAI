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

    [Theory]
    [InlineData("Before", 0.125)]
    [InlineData("Parent", 0.5)]
    [InlineData("After", 0.875)]
    public Task KnowledgeTree_DragAndDrop_PersistsTheConfirmedWorkingTree(string position, double relativeY) =>
        RunMoveAsync(position, relativeY);

    private async Task RunMoveAsync(string position, double relativeY)
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
            await Assertions.Expect(children.Nth(2)).ToBeVisibleAsync(new() { Timeout = 15_000 });
            var sourceIndex = position == "Before" ? 2 : 0;
            var targetIndex = position == "Before" ? 0 : 2;
            var source = await children.Nth(sourceIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Quellknoten fehlt.");
            var target = await children.Nth(targetIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Zielknoten fehlt.");

            await DragAndDropAsync(page, source, target, position, relativeY);

            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToContainTextAsync("Änderungsversion: 1");
            await Assertions.Expect(page.GetByTestId("tree-move-error")).ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByTestId($"treeitem-{source}")).ToBeVisibleAsync();
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId.Value);
        }
    }

    private static async Task DragAndDropAsync(IPage page, string source, string target, string position, double relativeY)
    {
        var sourceNode = page.GetByTestId($"treeitem-{source}");
        var targetNode = page.GetByTestId($"treeitem-{target}");
        var sourceBox = await sourceNode.BoundingBoxAsync() ?? throw new InvalidOperationException("Quellknoten ist nicht sichtbar.");
        var targetBox = await targetNode.BoundingBoxAsync() ?? throw new InvalidOperationException("Zielknoten ist nicht sichtbar.");
        var sourceX = sourceBox.X + sourceBox.Width / 2;
        var sourceY = sourceBox.Y + sourceBox.Height / 2;
        var targetX = targetBox.X + targetBox.Width / 2;
        var targetY = targetBox.Y + targetBox.Height * (float)relativeY;

        await page.Mouse.MoveAsync(sourceX, sourceY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(sourceX + 8, sourceY, new() { Steps = 2 });
        await page.Mouse.MoveAsync(targetX, targetY, new() { Steps = 8 });
        var expectedIndicator = $"is-drop-{position.ToLowerInvariant()}";
        Assert.True(await targetNode.EvaluateAsync<bool>($"node => node.classList.contains('{expectedIndicator}')"));
        await page.Mouse.UpAsync();
    }

}
