using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

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
            await page.GotoAsync($"{_host.Address}/knowledge?audienceId=Default", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();

            var setup = await PrepareTreeAsync(page, position);
            await DragAndDropAsync(page, setup.Source, setup.Target, position, relativeY);
            await AssertFirstMoveAsync(page, setup, position);
            var transactionMatch = Regex.Match(new Uri(page.Url).Query, @"transactionId=([0-9a-fA-F-]{36})");
            Assert.True(transactionMatch.Success, $"Der erste bestätigte Baum-Move hat keinen Entwurf aktiviert: {page.Url}");
            transactionId = Guid.Parse(transactionMatch.Groups[1].Value);

            var expectedAfterReload = await ReadVisibleSiblingTitlesAsync(page);
            await ReloadAndVerifyAsync(page, setup, position, expectedAfterReload);
            await MoveVisibleSiblingAsync(page, sourceIndex: 0, targetIndex: 1, "After", 0.875);
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("tree-move-error")).ToHaveCountAsync(0);
            var keyboardMove = await MoveWithKeyboardAsync(page);
            await ReloadAndVerifyParentMoveAsync(page, keyboardMove.Source, keyboardMove.Target);
            await VerifyResponsiveTreeAsync(page);
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId.Value);
        }
    }

    private sealed record TreeMoveSetup(string Source, string Target);

    private static async Task<TreeMoveSetup> PrepareTreeAsync(IPage page, string position)
    {
        var root = page.GetByRole(AriaRole.Treeitem).First;
        await Assertions.Expect(root).ToBeVisibleAsync();
        await root.Locator("button.tree-toggle-btn").ClickAsync();
        var children = page.Locator("div[role='treeitem'][aria-level='2']");
        await Assertions.Expect(children.Nth(2)).ToBeVisibleAsync(new() { Timeout = 15_000 });
        var sourceIndex = position == "Before" ? 2 : 0;
        var targetIndex = position == "Before" ? 0 : 2;
        var source = await children.Nth(sourceIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Quellknoten fehlt.");
        var target = await children.Nth(targetIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Zielknoten fehlt.");
        return new TreeMoveSetup(source, target);
    }

    private static async Task AssertFirstMoveAsync(IPage page, TreeMoveSetup setup, string position)
    {
        await WaitForMoveAsync(page, setup.Source, setup.Target, position);
        await Assertions.Expect(page.GetByTestId("active-draft-link")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("tree-move-error")).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByTestId($"treeitem-{setup.Source}")).ToBeVisibleAsync();
        if (position is "Before" or "After")
        {
            var siblingIds = await page.Locator("div[role='treeitem'][aria-level='2']").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.nodeid)");
            var sourcePosition = Array.IndexOf(siblingIds, setup.Source);
            var targetPosition = Array.IndexOf(siblingIds, setup.Target);
            Assert.True(
                position == "Before" ? sourcePosition < targetPosition : sourcePosition > targetPosition,
                $"{position} expected source {setup.Source} before target {setup.Target}; visible order: {string.Join(",", siblingIds)}");
        }
    }

    private static async Task<(string Source, string Target)> MoveWithKeyboardAsync(IPage page)
    {
        var children = page.Locator("div[role='treeitem'][aria-level='2']");
        var keyboardSource = await children.Nth(0).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Tastatur-Quelle fehlt.");
        var keyboardTarget = await children.Nth(1).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Tastatur-Ziel fehlt.");
        var moveButton = page.GetByTestId($"move-node-{keyboardSource}");
        await moveButton.FocusAsync();
        await moveButton.PressAsync("Enter");
        await Assertions.Expect(page.GetByTestId($"drop-targets-{keyboardTarget}")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId($"move-before-{keyboardTarget}")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId($"move-under-{keyboardTarget}")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId($"move-after-{keyboardTarget}")).ToBeVisibleAsync();
        var underButton = page.GetByTestId($"move-under-{keyboardTarget}");
        await underButton.FocusAsync();
        await underButton.PressAsync("Enter");
        await Assertions.Expect(page.GetByTestId("active-draft-link")).ToBeVisibleAsync();
        await WaitForParentMoveAsync(page, keyboardSource, keyboardTarget);
        return (keyboardSource, keyboardTarget);
    }

    private static async Task ReloadAndVerifyParentMoveAsync(IPage page, string source, string target)
    {
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        var root = page.GetByRole(AriaRole.Treeitem).First;
        if (await root.GetAttributeAsync("aria-expanded") != "true")
            await root.Locator("button.tree-toggle-btn").ClickAsync();
        var targetNode = page.GetByTestId($"treeitem-{target}");
        await Assertions.Expect(targetNode).ToBeVisibleAsync();
        if (await targetNode.GetAttributeAsync("aria-expanded") != "true")
            await targetNode.Locator("button.tree-toggle-btn").ClickAsync();
        await Assertions.Expect(page.GetByTestId($"treeitem-{source}")).ToBeVisibleAsync();
        Assert.True(await page.GetByTestId($"treeitem-{target}").EvaluateAsync<bool>(
            $"target => target.closest('.tree-node-wrapper').contains(document.querySelector('[data-nodeid=\\\"{source}\\\"]'))"));
    }

    private static async Task ReloadAndVerifyAsync(IPage page, TreeMoveSetup setup, string position, IReadOnlyList<string> expectedOrder)
    {
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        var root = page.GetByRole(AriaRole.Treeitem).First;
        if (await root.GetAttributeAsync("aria-expanded") != "true")
            await root.Locator("button.tree-toggle-btn").ClickAsync();
        await Assertions.Expect(page.Locator("div[role='treeitem'][aria-level='2']").First).ToBeVisibleAsync();
        if (position is "Before" or "After")
        {
            Assert.Equal(expectedOrder, await ReadVisibleSiblingTitlesAsync(page));
            return;
        }

        var targetNode = page.GetByTestId($"treeitem-{setup.Target}");
        if (await targetNode.GetAttributeAsync("aria-expanded") != "true")
            await targetNode.Locator("button.tree-toggle-btn").ClickAsync();
        var nestedSource = page.GetByTestId($"treeitem-{setup.Source}");
        await Assertions.Expect(nestedSource).ToBeVisibleAsync();
        await Assertions.Expect(nestedSource).ToHaveAttributeAsync("aria-level", "3");
    }

    private static async Task VerifyResponsiveTreeAsync(IPage page)
    {
        await page.SetViewportSizeAsync(390, 844);
        var horizontalOverflow = await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth");
        var overflowingElements = horizontalOverflow
            ? await page.EvaluateAsync<string[]>("""() => [...document.querySelectorAll('body *')].filter(element => element.getBoundingClientRect().right > document.documentElement.clientWidth + 1).slice(0, 12).map(element => `${element.tagName}.${element.className} right=${Math.round(element.getBoundingClientRect().right)}`)""")
            : [];
        Assert.False(horizontalOverflow, $"Der Wissensbaum darf bei schmalem Viewport keinen horizontalen Seiten-Overflow erzeugen. Elemente: {string.Join("; ", overflowingElements)}");
        await Assertions.Expect(page.GetByTestId("knowledge-tree")).ToBeVisibleAsync();
    }

    private static async Task MoveVisibleSiblingAsync(IPage page, int sourceIndex, int targetIndex, string position, double relativeY)
    {
        var children = page.Locator("div[role='treeitem'][aria-level='2']");
        var source = await children.Nth(sourceIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Quellknoten fehlt.");
        var target = await children.Nth(targetIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Zielknoten fehlt.");
        await DragAndDropAsync(page, source, target, position, relativeY);
    }

    private static async Task<IReadOnlyList<string>> ReadVisibleSiblingTitlesAsync(IPage page)
    {
        var titles = await page.Locator("div[role='treeitem'][aria-level='2'] .tree-node-title").AllTextContentsAsync();
        return titles.Select(title => title.Trim()).ToArray();
    }

    private static async Task DragAndDropAsync(IPage page, string source, string target, string position, double relativeY)
    {
        var sourceNode = page.GetByTestId($"treeitem-{source}");
        var targetNode = page.GetByTestId($"treeitem-{target}");
        var sourceBox = await sourceNode.Locator(".tree-node-title").BoundingBoxAsync() ?? throw new InvalidOperationException("Quellknoten ist nicht sichtbar.");
        var targetBox = await targetNode.Locator(".tree-node-title").BoundingBoxAsync() ?? throw new InvalidOperationException("Zieltitel ist nicht sichtbar.");
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
        await WaitForMoveAsync(page, source, target, position);
    }

    private static async Task WaitForMoveAsync(IPage page, string source, string target, string position)
    {
        try
        {
            await page.WaitForFunctionAsync(
                """
            ({ source, target, position }) => {
                const sourceNode = document.querySelector(`[data-nodeid="${source}"]`);
                const targetNode = document.querySelector(`[data-nodeid="${target}"]`);
                if (document.querySelector('[data-testid="tree-move-error"]')) return true;
                if (!sourceNode || !targetNode) return false;
                if (position === 'Parent') return sourceNode.getAttribute('aria-level') === '3'
                    && targetNode.closest('.tree-node-wrapper').contains(sourceNode);
                const siblings = [...document.querySelectorAll('div[role="treeitem"][aria-level="2"]')];
                const sourceIndex = siblings.findIndex(node => node.dataset.nodeid === source);
                const targetIndex = siblings.findIndex(node => node.dataset.nodeid === target);
                return position === 'Before' ? sourceIndex >= 0 && sourceIndex < targetIndex
                    : sourceIndex >= 0 && sourceIndex > targetIndex;
            }
            """,
                new { source, target, position },
                new PageWaitForFunctionOptions { Timeout = 15_000 });
        }
        catch (TimeoutException exception)
        {
            var diagnostic = await page.EvaluateAsync<string>("""
            () => JSON.stringify({
                url: location.href,
                alerts: [...document.querySelectorAll('[role="alert"]')].map(node => node.innerText),
                nodes: [...document.querySelectorAll('div[role="treeitem"][aria-level="2"]')].map(node => ({
                    id: node.dataset.nodeid,
                    title: node.querySelector('.tree-node-title')?.innerText,
                    classes: node.className
                }))
            })
            """);
            throw new Xunit.Sdk.XunitException($"Baum-Move {position} wurde nicht übernommen. UI: {diagnostic}{Environment.NewLine}{exception.Message}");
        }
    }

    private static Task WaitForParentMoveAsync(IPage page, string source, string target) =>
        page.WaitForFunctionAsync(
            """
            ({ source, target }) => {
                const targetNode = document.querySelector(`[data-nodeid="${target}"]`);
                if (document.querySelector('[data-testid="tree-move-error"]')) return true;
                if (!targetNode) return false;
                return ![...document.querySelectorAll('div[role="treeitem"][aria-level="2"]')]
                    .some(node => node.dataset.nodeid === source);
            }
            """,
            new { source, target },
            new PageWaitForFunctionOptions { Timeout = 15_000 });

}
