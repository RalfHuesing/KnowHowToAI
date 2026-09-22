using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class PageFrameSmokeTests
{
    private readonly PublishedServerHost _host;

    public PageFrameSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task FeaturePagesUseAvailableFrameAndKeepResponsiveContentReachable()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        foreach (var viewport in new[] { 1280, 1920, 2560, 1024 })
        {
            await page.SetViewportSizeAsync(viewport, 720);
            foreach (var route in Routes)
            {
                var response = await page.GotoAsync(_host.Address + route.Path, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30_000
                });

                Assert.NotNull(response);
                Assert.Equal((int)HttpStatusCode.OK, response.Status);
                await CircuitProbe.WaitForInteractivityAsync(page);

                var frame = page.Locator($"[data-testid='{route.TestId}'].page-frame");
                await Assertions.Expect(frame).ToBeVisibleAsync(new() { Timeout = 30_000 });
                await AssertPageContractAsync(page, frame, route, viewport);

                if (viewport is 1280 or 1024)
                    await AssertNavigationStatesAsync(page, frame, route, viewport);
            }
        }
    }

    [Fact]
    public async Task FeaturePageRootsUsePageFrameForSharedRhythmAndContainNoRootOverrides()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        string[]? expectedSharedStyles = null;
        foreach (var route in Routes)
        {
            await GotoAsync(page, route.Path);

            var frame = page.Locator($"[data-testid='{route.TestId}'].page-frame");
            await Assertions.Expect(frame).ToBeVisibleAsync(new() { Timeout = 30_000 });

            var sharedStyles = await frame.EvaluateAsync<string[]>(
                """element => { const style = getComputedStyle(element); return [style.display, style.flexDirection, style.gap]; }""");
            Assert.Equal("flex", sharedStyles[0]);
            Assert.Equal("column", sharedStyles[1]);
            Assert.NotEqual("normal", sharedStyles[2]);

            expectedSharedStyles ??= sharedStyles;
            Assert.Equal(expectedSharedStyles, sharedStyles);

            var rootOverrides = await page.EvaluateAsync<string[]>(
                """
                rootClass => {
                    const sharedRootProperties = new Set([
                        'display', 'flex-direction', 'gap', 'box-sizing', 'width', 'min-width', 'max-width',
                        'height', 'margin', 'margin-top', 'margin-right', 'margin-bottom', 'margin-left',
                        'padding', 'padding-top', 'padding-right', 'padding-bottom', 'padding-left'
                    ]);
                    const exactRootSelector = selector => {
                        const rootToken = `.${rootClass}`;
                        if (!selector.startsWith(rootToken))
                            return false;
                        return selector.slice(rootToken.length).replace(/\[[^\]]+\]/g, '').trim() === '';
                    };
                    const violations = [];
                    const visit = rules => {
                        for (const rule of rules) {
                            if (rule.cssRules)
                                visit(rule.cssRules);
                            if (typeof rule.selectorText !== 'string')
                                continue;
                            const selectors = rule.selectorText.split(',').map(selector => selector.trim());
                            if (!selectors.some(exactRootSelector))
                                continue;
                            const declarations = Array.from(rule.style)
                                .filter(property => sharedRootProperties.has(property));
                            if (declarations.length > 0)
                                violations.push(`${rule.selectorText}: ${declarations.join(', ')}`);
                        }
                    };
                    for (const sheet of document.styleSheets) {
                        try { visit(sheet.cssRules); } catch { }
                    }
                    return violations;
                }
                """, route.RootClass);

            Assert.Empty(rootOverrides);
        }
    }

    [Fact]
    public async Task FeaturePagesKeepContentAndActionsReachableAtReflowWidths()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 640, Height = 720 }
        });

        foreach (var viewport in new[] { 640, 320 })
        {
            await page.SetViewportSizeAsync(viewport, 720);
            foreach (var route in Routes)
            {
                var response = await page.GotoAsync(_host.Address + route.Path, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30_000
                });

                Assert.NotNull(response);
                Assert.Equal((int)HttpStatusCode.OK, response.Status);
                await CircuitProbe.WaitForInteractivityAsync(page);

                var frame = page.Locator($"[data-testid='{route.TestId}'].page-frame");
                await Assertions.Expect(frame).ToBeVisibleAsync(new() { Timeout = 30_000 });
                await AssertPageContractAsync(page, frame, route, viewport);

                var navigationToggle = page.Locator("button[aria-controls='shell-navigation']");
                await Assertions.Expect(navigationToggle).ToBeVisibleAsync(new() { Timeout = 30_000 });
                await Assertions.Expect(navigationToggle).ToHaveAccessibleNameAsync("Navigation einblenden");

                var reachableActions = frame.Locator("a:visible, button:visible, input:visible, textarea:visible, select:visible");
                if (await reachableActions.CountAsync() > 0)
                {
                    var reachableAction = reachableActions.First;
                    await reachableAction.ScrollIntoViewIfNeededAsync();
                    var actionBox = await reachableAction.BoundingBoxAsync()
                        ?? throw new InvalidOperationException($"{route.Name} besitzt bei {viewport} keine erreichbare Aktion.");
                    Assert.True(
                        actionBox.X >= 0 && actionBox.X + actionBox.Width <= viewport,
                        $"{route.Name} besitzt bei {viewport} eine außerhalb liegende Aktion.");
                }
                else
                {
                    await Assertions.Expect(frame.Locator("h1")).ToBeVisibleAsync();
                }
            }
        }
    }

    [Fact]
    public async Task SelectedKnowledgeNodeAndOpenTransactionKeepTheSamePageContract()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        await GotoAsync(page, "/knowledge?audienceId=Default");
        var rootItem = page.GetByRole(AriaRole.Treeitem).First;
        await rootItem.Locator("button.tree-toggle-btn").ClickAsync();
        var childItem = page.Locator("div[role='treeitem'][aria-level='2']").First;
        await Assertions.Expect(childItem).ToBeVisibleAsync();
        await childItem.ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-details")).ToBeVisibleAsync();
        await AssertPageContractAsync(
            page,
            page.GetByTestId("knowledge-page"),
            new RouteSpec("Wissensbasis ausgewählter Node", "/knowledge/{NodeId:guid}", "knowledge-page", "knowledge-page"),
            1280);
        await Assertions.Expect(page.GetByTestId("node-details-title")).ToContainTextAsync("Browser-");

        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        Guid transactionId;
        await GotoAsync(page, "/transactions");
        await page.GetByTestId("tx-purpose-input").FillAsync("PageFrame-Vertragsnachweis");
        await page.GetByTestId("begin-transaction-button").ClickAsync();
        await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync();
        transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
        try
        {
            await GotoAsync(page, $"/transactions/{transactionId:D}");
            await Assertions.Expect(page.GetByTestId("transaction-diff")).ToBeVisibleAsync();
            await AssertPageContractAsync(
                page,
                page.GetByTestId("transaction-page"),
                new RouteSpec("Transaction offen", $"/transactions/{transactionId:D}", "transaction-page", "transaction-page"),
                1280);
        }
        finally
        {
            await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId);
        }
    }

    private static async Task AssertPageContractAsync(
        IPage page,
        ILocator frame,
        RouteSpec route,
        int viewport)
    {
        var main = page.Locator("main#shell-main");
        await Assertions.Expect(main).ToHaveCountAsync(1);
        await Assertions.Expect(main.Locator("main")).ToHaveCountAsync(0);
        await Assertions.Expect(frame).ToHaveCountAsync(1);
        await Assertions.Expect(frame.Locator("h1")).ToHaveCountAsync(1);
        await Assertions.Expect(main.Locator("h1")).ToHaveCountAsync(1);

        var frameBox = await frame.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"{route.Name} besitzt bei {viewport} keine Begrenzungsbox.");
        var mainBox = await main.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"#shell-main besitzt bei {viewport} keine Begrenzungsbox.");
        var mainStyles = await main.EvaluateAsync<string[]>(
            """element => { const style = getComputedStyle(element); return [style.paddingLeft, style.paddingRight]; }""");
        var frameStyles = await frame.EvaluateAsync<string[]>(
            """element => { const style = getComputedStyle(element); return [style.maxWidth, style.marginLeft, style.marginRight, style.paddingLeft, style.paddingRight]; }""");

        Assert.Equal("none", frameStyles[0]);
        Assert.Equal("0px", frameStyles[1]);
        Assert.Equal("0px", frameStyles[2]);
        Assert.Equal("0px", frameStyles[3]);
        Assert.Equal("0px", frameStyles[4]);

        var expectedLeft = mainBox.X + ParsePixels(mainStyles[0]);
        var expectedRight = mainBox.X + mainBox.Width - ParsePixels(mainStyles[1]);
        Assert.True(
            Math.Abs(frameBox.X - expectedLeft) <= 1.5,
            $"{route.Name} weicht bei {viewport} an der linken Shell-Innenkante ab.");
        Assert.True(
            Math.Abs(frameBox.X + frameBox.Width - expectedRight) <= 1.5,
            $"{route.Name} weicht bei {viewport} an der rechten Shell-Innenkante ab.");

        var metrics = await page.EvaluateAsync<double[]>(
            """() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]""");
        Assert.True(
            metrics[0] <= metrics[1],
            $"{route.Name} läuft bei {viewport} CSS-Pixeln horizontal über ({metrics[0]} > {metrics[1]}).");

        var readable = frame.Locator(".readable");
        if (await readable.CountAsync() > 0)
        {
            var maxWidths = await readable.EvaluateAllAsync<string[]>(
                "elements => elements.map(element => getComputedStyle(element).maxWidth)");
            Assert.All(maxWidths, maxWidth => Assert.NotEqual("none", maxWidth));
        }

        var actionGroups = frame.Locator(".action-group");
        if (await actionGroups.CountAsync() > 0)
        {
            var wrapping = await actionGroups.EvaluateAllAsync<string[]>(
                "elements => elements.map(element => getComputedStyle(element).flexWrap)");
            Assert.All(wrapping, value => Assert.Equal("wrap", value));
        }
    }

    private static async Task AssertNavigationStatesAsync(
        IPage page,
        ILocator frame,
        RouteSpec route,
        int viewport)
    {
        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        var navigationToggle = page.Locator("button[aria-controls='shell-navigation']");

        if (viewport >= 1280)
        {
            await Assertions.Expect(navigation).ToBeVisibleAsync();
            await Assertions.Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "true");
            await page.Locator("button[aria-controls='shell-navigation']").ClickAsync();
            await Assertions.Expect(navigation).ToHaveCountAsync(0);
            await Assertions.Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "false");
            await AssertPageContractAsync(page, frame, route, viewport);
            await navigationToggle.ClickAsync();
            await Assertions.Expect(navigation).ToBeVisibleAsync();
        }
        else
        {
            await Assertions.Expect(navigation).ToHaveCountAsync(0);
            await Assertions.Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "false");
            await navigationToggle.ClickAsync();
            await Assertions.Expect(navigation).ToBeVisibleAsync();
            await AssertPageContractAsync(page, frame, route, viewport);
            await navigationToggle.ClickAsync();
            await Assertions.Expect(navigation).ToHaveCountAsync(0);
        }
    }

    private static double ParsePixels(string value) =>
        double.Parse(value.TrimEnd('p', 'x'), System.Globalization.CultureInfo.InvariantCulture);

    private async Task GotoAsync(IPage page, string route)
    {
        var response = await page.GotoAsync(_host.Address + route, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);
    }

    private static readonly RouteSpec[] Routes =
    [
        new("Startseite", "/", "dashboard-page", "dashboard-page"),
        new("Wissensbasis ohne Auswahl", "/knowledge?audienceId=Default", "knowledge-page", "knowledge-page"),
        new("Wissensbasis nicht gefundener Node", "/knowledge/00000000-0000-0000-0000-000000000000?audienceId=Default", "knowledge-page", "knowledge-page"),
        new("Suche", "/search?audienceId=Default", "search-page", "search-page"),
        new("Transactions", "/transactions", "transactions-page", "transactions-page"),
        new("Transaction nicht gefunden", "/transactions/00000000-0000-0000-0000-000000000000", "transaction-page", "transaction-page"),
        new("Entwürfe", "/drafts", "drafts-page", "drafts-page"),
        new("Entwurf nicht gefunden", "/drafts/00000000-0000-0000-0000-000000000000", "draft-page", "draft-page"),
        new("Zielgruppen", "/audiences?audienceId=Default", "audiences-page", "audiences-page"),
        new("Historie", "/history?audienceId=Default", "history-page", "history-page")
    ];

    private sealed record RouteSpec(string Name, string Path, string TestId, string RootClass);
}
