using KnowHowToAI.BrowserTests.TestSupport;
using ModelContextProtocol.Client;
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

    [Fact]
    public async Task KnowledgeTree_DroppingOntoItselfDoesNotChangeTheConfirmedOrder()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        await page.GotoAsync($"{_host.Address}/knowledge?audienceId=Default", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await CircuitProbe.WaitForInteractivityAsync(page);

        var root = RootSelection(page);
        await root.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var children = RootChildren(page);
        await Assertions.Expect(children.Nth(1)).ToBeVisibleAsync(new() { Timeout = 15_000 });
        var originalOrder = await ReadVisibleSiblingTitlesAsync(page);
        var source = children.First;
        var sourceId = await source.GetAttributeAsync("data-nodeid")
            ?? throw new InvalidOperationException("Quellknoten fehlt.");
        var sourceTitle = await source.Locator(".tree-node-title").BoundingBoxAsync()
            ?? throw new InvalidOperationException("Quelltitel ist nicht sichtbar.");
        var startX = sourceTitle.X + sourceTitle.Width / 2;
        var startY = sourceTitle.Y + sourceTitle.Height / 2;

        await page.Mouse.MoveAsync(startX, startY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(startX + 8, startY, new() { Steps = 2 });
        await page.Mouse.MoveAsync(startX + 2, startY + 1, new() { Steps = 2 });
        await page.Mouse.UpAsync();

        await Assertions.Expect(page.GetByTestId("active-draft-link")).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByTestId("tree-move-error")).ToHaveCountAsync(0);
        Assert.Equal(originalOrder, await ReadVisibleSiblingTitlesAsync(page));
        Assert.Empty(await page.GetByTestId($"tree-node-{sourceId}").EvaluateAsync<string[]>(
            "node => ['is-dragging', 'is-drop-before', 'is-drop-parent', 'is-drop-after'].filter(className => node.classList.contains(className))"));
    }

    [Fact]
    public async Task KnowledgeTree_StaleSnapshotDropShowsServerRejectionAndRestoresConfirmedTree()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        string? externalNodeId = null;
        try
        {
            await page.GotoAsync($"{_host.Address}/knowledge?audienceId=Default", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await CircuitProbe.WaitForInteractivityAsync(page);
            var root = RootSelection(page);
            var rootId = await root.GetAttributeAsync("data-nodeid")
                ?? throw new InvalidOperationException("Der Root-Node fehlt.");
            await root.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
            await Assertions.Expect(RootChildren(page).First).ToBeVisibleAsync();
            var originalChildren = await ReadVisibleSiblingTitlesAsync(page);

            var externalTitle = $"Browser-Parallel-{Guid.NewGuid():N}";
            externalNodeId = await CommitExternalChildAsync(rootId, externalTitle);
            await MoveVisibleSiblingAsync(page, sourceIndex: 0, targetIndex: 1, "After", 0.875);

            var error = page.GetByTestId("tree-move-error");
            await Assertions.Expect(error).ToBeVisibleAsync();
            Assert.False(string.IsNullOrWhiteSpace(await error.InnerTextAsync()));
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToHaveCountAsync(0);
            root = RootSelection(page);
            var rootToggle = root.Locator("xpath=..").Locator("button.tree-toggle-btn");
            await Assertions.Expect(rootToggle).ToHaveAttributeAsync("aria-expanded", "true");
            var loadChildren = page.GetByTestId($"tree-load-children-{rootId}");
            await Assertions.Expect(loadChildren).ToBeVisibleAsync();
            await loadChildren.ClickAsync();
            await Assertions.Expect(RootChildren(page).First).ToBeVisibleAsync();
            var restoredChildren = await ReadVisibleSiblingTitlesAsync(page);
            Assert.Contains(externalTitle, restoredChildren);
            Assert.Equal(originalChildren, restoredChildren.Where(title => title != externalTitle));

            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            root = RootSelection(page);
            rootToggle = root.Locator("xpath=..").Locator("button.tree-toggle-btn");
            await Assertions.Expect(rootToggle).ToHaveAttributeAsync("aria-expanded", "false");
            await rootToggle.ClickAsync();
            await Assertions.Expect(page.GetByText(externalTitle, new() { Exact = true }).Locator("xpath=..")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToHaveCountAsync(0);
        }
        finally
        {
            if (externalNodeId is not null)
                await DeleteCommittedNodeAsync(externalNodeId);
        }
    }

    private async Task<string> CommitExternalChildAsync(string parentNodeId, string title)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{_host.Address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var begin = await BrowserMcpAssertions.CallAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "Parallele Änderung für den verworfenen Browser-Drop",
            ["client"] = "KnowHowToAI.BrowserTests"
        });
        var transactionId = BrowserMcpAssertions.RequiredString(begin, "transactionId");
        try
        {
            var created = await BrowserMcpAssertions.CallAsync(client, "create_node", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId,
                ["parentNodeId"] = parentNodeId,
                ["title"] = title
            });
            var nodeId = BrowserMcpAssertions.RequiredString(created, "nodeId");
            await BrowserMcpAssertions.CallAsync(client, "commit_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId
            });
            return nodeId;
        }
        catch
        {
            await client.CallToolAsync("discard_transaction", new Dictionary<string, object?>
            {
                ["transactionId"] = transactionId
            });
            throw;
        }
    }

    private async Task DeleteCommittedNodeAsync(string nodeId)
    {
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{_host.Address}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }));
        var begin = await BrowserMcpAssertions.CallAsync(client, "begin_transaction", new Dictionary<string, object?>
        {
            ["purpose"] = "Browser-Testbereinigung nach verworfenem Drop",
            ["client"] = "KnowHowToAI.BrowserTests"
        });
        var transactionId = BrowserMcpAssertions.RequiredString(begin, "transactionId");
        await BrowserMcpAssertions.CallAsync(client, "delete_node", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId,
            ["nodeId"] = nodeId
        });
        await BrowserMcpAssertions.CallAsync(client, "commit_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = transactionId
        });
    }

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
            await Assertions.Expect(page.Locator("[data-testid^='move-node-'], [data-testid^='move-before-'], [data-testid^='move-under-'], [data-testid^='move-after-']")).ToHaveCountAsync(0);
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
        var root = RootSelection(page);
        await Assertions.Expect(root).ToBeVisibleAsync();
        await root.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var children = RootChildren(page);
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
        await Assertions.Expect(RootSelection(page).Locator("xpath=..").Locator("button.tree-toggle-btn")).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(page.GetByTestId($"tree-node-{setup.Source}")).ToBeVisibleAsync();
        if (position is "Before" or "After")
        {
            var siblingIds = await RootChildren(page).EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.nodeid)");
            var sourcePosition = Array.IndexOf(siblingIds, setup.Source);
            var targetPosition = Array.IndexOf(siblingIds, setup.Target);
            Assert.True(
                position == "Before" ? sourcePosition < targetPosition : sourcePosition > targetPosition,
                $"{position} expected source {setup.Source} before target {setup.Target}; visible order: {string.Join(",", siblingIds)}");
        }
    }

    private static async Task ReloadAndVerifyAsync(IPage page, TreeMoveSetup setup, string position, IReadOnlyList<string> expectedOrder)
    {
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        var root = RootSelection(page);
        if (await root.Locator("xpath=..").Locator("button.tree-toggle-btn").GetAttributeAsync("aria-expanded") != "true")
            await root.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        await Assertions.Expect(RootChildren(page).First).ToBeVisibleAsync();
        if (position is "Before" or "After")
        {
            Assert.Equal(expectedOrder, await ReadVisibleSiblingTitlesAsync(page));
            return;
        }

        var targetNode = page.GetByTestId($"tree-node-{setup.Target}");
        if (await targetNode.Locator("xpath=..").Locator("button.tree-toggle-btn").GetAttributeAsync("aria-expanded") != "true")
            await targetNode.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var nestedSource = page.GetByTestId($"tree-node-{setup.Source}");
        await Assertions.Expect(nestedSource).ToBeVisibleAsync();
        Assert.True(await nestedSource.EvaluateAsync<bool>("node => node.closest('.tree-node-wrapper').parentElement.closest('.tree-node-wrapper') !== null"));
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
        var children = RootChildren(page);
        var source = await children.Nth(sourceIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Quellknoten fehlt.");
        var target = await children.Nth(targetIndex).GetAttributeAsync("data-nodeid") ?? throw new InvalidOperationException("Zielknoten fehlt.");
        await DragAndDropAsync(page, source, target, position, relativeY);
    }

    private static async Task<IReadOnlyList<string>> ReadVisibleSiblingTitlesAsync(IPage page)
    {
        var titles = await RootChildren(page).Locator(".tree-node-title").AllTextContentsAsync();
        return titles.Select(title => title.Trim()).ToArray();
    }

    private static async Task DragAndDropAsync(IPage page, string source, string target, string position, double relativeY)
    {
        var sourceNode = page.GetByTestId($"tree-node-{source}");
        var targetNode = page.GetByTestId($"tree-node-{target}");
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
                if (position === 'Parent') return targetNode.closest('.tree-node-wrapper').querySelector(':scope > .tree-children-group')?.contains(sourceNode) === true;
                const siblings = [...document.querySelectorAll('.knowledge-tree > .tree-node-wrapper > .tree-children-group > .tree-node-wrapper > .tree-node-row > .tree-node-select')];
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
                nodes: [...document.querySelectorAll('.knowledge-tree > .tree-node-wrapper > .tree-children-group > .tree-node-wrapper > .tree-node-row > .tree-node-select')].map(node => ({
                    id: node.dataset.nodeid,
                    title: node.querySelector('.tree-node-title')?.innerText,
                    classes: node.className
                }))
            })
            """);
            throw new Xunit.Sdk.XunitException($"Baum-Move {position} wurde nicht übernommen. UI: {diagnostic}{Environment.NewLine}{exception.Message}");
        }
    }

    private static ILocator RootSelection(IPage page) =>
        page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");

    private static ILocator RootChildren(IPage page) =>
        page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-children-group > li > .tree-node-row > .tree-node-select");
}
