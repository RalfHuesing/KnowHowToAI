using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class PageFrameSmokeTests
{
    private readonly PublishedServerHost _host;

    public PageFrameSmokeTests(SmokeHostFixture fixture) => _host = fixture.Host;

    [Fact]
    public async Task RemainingPagesUseAvailableFrameAndKeepResponsiveContentReachable()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        foreach (var viewport in new[] { 1280, 1024 })
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
    public async Task RemainingPageRootsUsePageFrameForSharedRhythmAndContainNoRootOverrides()
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
    public async Task RemainingPagesKeepContentAndActionsReachableAtDesktopWidths()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        foreach (var viewport in new[] { 1280, 1024 })
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

                await AssertInitialNavigationStateAsync(page, viewport);

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
    public async Task SelectedKnowledgeNodeKeepsTheSamePageContract()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        foreach (var (width, height) in new[] { (1280, 800), (1024, 720) })
        {
            await page.SetViewportSizeAsync(width, height);
            await GotoAsync(page, "/knowledge?audienceId=Default");
            var rootItem = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
            await rootItem.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
            var childItem = page.Locator(".knowledge-tree > .tree-node-wrapper > .tree-children-group > .tree-node-wrapper > .tree-node-row > .tree-node-select")
                .Filter(new() { HasText = "Browser-Fallback-Teilbaum" });
            if (await childItem.CountAsync() == 0)
            {
                childItem = page.Locator(".knowledge-tree > .tree-node-wrapper > .tree-children-group > .tree-node-wrapper > .tree-node-row > .tree-node-select").First;
            }
            await Assertions.Expect(childItem).ToBeVisibleAsync();
            await childItem.Locator(".tree-node-title").ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-details")).ToBeVisibleAsync();
            await AssertPageContractAsync(
                page,
                page.GetByTestId("knowledge-page"),
                new RouteSpec("Wissensbasis ausgewählter Node", "/knowledge/{NodeId:guid}", "knowledge-page", "knowledge-page"),
                width);
            await Assertions.Expect(page.GetByTestId("knowledge-page").Locator("h1")).ToContainTextAsync("Browser-");
            var tabs = page.GetByTestId("node-details-tabs");
            await AssertTabBarAndContextAsync(page, tabs, width, height);

            var readPanel = page.GetByTestId("node-view-panel-read");
            var readContent = page.GetByTestId("node-details-content");
            await AssertFollowsAsync(tabs, readPanel, "Lesebereich", width, height);
            await AssertFollowsAsync(tabs, readContent, "Leseinhalt", width, height);

            await tabs.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten", Exact = true }).ClickAsync();
            var editor = page.GetByTestId("content-editor");
            await Assertions.Expect(editor).ToBeVisibleAsync();
            var editorPanel = page.GetByTestId("node-view-panel-editor");
            await AssertFollowsAsync(tabs, editorPanel, "Editorbereich", width, height);
            await AssertEditorLayoutAndActionsAsync(page, editor, editorPanel, width, height);
            var pageFrameContentMetrics = await page.EvaluateAsync<double[]>(
                "() => { const el = document.querySelector('.page-frame__content'); return [el.scrollHeight, el.clientHeight]; }");
            Assert.True(
                pageFrameContentMetrics[0] <= pageFrameContentMetrics[1],
                $"Der Inhaltsbereich überläuft bei {width}×{height} intern vertikal ({pageFrameContentMetrics[0]} > {pageFrameContentMetrics[1]}).");
            var viewportMetrics = await page.EvaluateAsync<double[]>(
                "() => [document.documentElement.scrollWidth, document.documentElement.clientWidth, document.documentElement.scrollHeight, window.innerHeight]");
            Assert.True(viewportMetrics[2] <= viewportMetrics[3],
                $"Die Wissensroute erzeugt bei {width}x{height} einen Fensterscrollbalken ({viewportMetrics[2]} > {viewportMetrics[3]}).");
            Assert.True(viewportMetrics[0] <= viewportMetrics[1],
                $"Die Wissensroute läuft bei {width}×{height} horizontal über ({viewportMetrics[0]} > {viewportMetrics[1]}).");
        }

    }

    private static async Task AssertEditorLayoutAndActionsAsync(IPage page, ILocator editor, ILocator editorPanel, int width, int height)
    {
        var toolbarRow = editor.Locator(".content-editor__toolbar-row");
        await Assertions.Expect(toolbarRow).ToBeVisibleAsync();
        var toolbarStyles = await toolbarRow.EvaluateAsync<string[]>(
            "element => { const style = getComputedStyle(element); return [style.display, style.flexWrap]; }");
        Assert.Equal(["flex", "wrap"], toolbarStyles);
        var viewMode = editor.GetByTestId("content-editor-view-mode");
        await Assertions.Expect(viewMode).ToBeVisibleAsync();
        await Assertions.Expect(viewMode.Locator("option")).ToHaveTextAsync(["Visuell", "Markdown-Quelle"]);

        var editorBox = await editor.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Der Inhaltseditor besitzt bei {width}×{height} keine Begrenzungsbox.");
        Assert.True(editorBox.X >= 0 && editorBox.X + editorBox.Width <= width,
            $"Der Inhaltseditor liegt bei {width}×{height} außerhalb der erreichbaren Breite.");
        var surface = editor.GetByTestId("content-editor-surface");
        var surfaceStyles = await surface.EvaluateAsync<string[]>(
            "element => { const style = getComputedStyle(element); return [style.borderTopStyle, style.overflowY, style.borderRadius]; }");
        Assert.NotEqual("none", surfaceStyles[0]);
        Assert.Equal("auto", surfaceStyles[1]);
        Assert.NotEqual("0px", surfaceStyles[2]);
        var surfaceBox = await surface.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Die visuelle Editor-Fläche besitzt bei {width}×{height} keine Begrenzungsbox.");
        var minSurfaceHeight = height >= 800 ? 150 : 70;
        Assert.True(
            surfaceBox.Height >= minSurfaceHeight,
            $"Die visuelle Editor-Fläche füllt bei {width}×{height} die Resthöhe nicht aus (Höhe: {surfaceBox.Height}px, erwartet mindestens {minSurfaceHeight}px).");

        var footer = editorPanel.Locator(".tab-panel-layout__footer");
        var footerStyles = await footer.EvaluateAsync<string[]>(
            "element => { const style = getComputedStyle(element); return [style.display, style.justifyContent, style.flexWrap]; }");
        Assert.Equal(["flex", "space-between", "wrap"], footerStyles);
        var saveStatus = editor.GetByTestId("content-editor-save-status");
        await Assertions.Expect(saveStatus).ToContainTextAsync("Gespeichert");
        var saveAction = page.GetByTestId("content-editor-save");
        await Assertions.Expect(saveAction).ToBeVisibleAsync();
        var statusBox = await saveStatus.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Der Speicherstatus besitzt bei {width}×{height} keine Begrenzungsbox.");
        Assert.True(statusBox.Y >= 0 && statusBox.Y + statusBox.Height <= height,
            $"Der Speicherstatus ist bei {width}×{height} nicht im Viewport erreichbar.");
        var saveBox = await saveAction.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Die Editor-Speicheraktion ist bei {width}×{height} nicht erreichbar.");
        Assert.True(saveBox.X >= 0 && saveBox.X + saveBox.Width <= width,
            $"Die Editor-Speicheraktion liegt bei {width}×{height} außerhalb der erreichbaren Breite.");
        Assert.True(saveBox.Y >= 0 && saveBox.Y + saveBox.Height <= height,
            $"Die Editor-Speicheraktion ist bei {width}×{height} nicht im Viewport erreichbar.");
        Assert.True(statusBox.X + statusBox.Width <= saveBox.X,
            $"Der Speicherstatus steht bei {width}×{height} nicht links vor der Speichern-Aktion.");
        var contentPane = page.GetByTestId("knowledge-content-pane");
        var contentPaneBox = await contentPane.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Das Inhalts-Pane besitzt bei {width}×{height} keine Begrenzungsbox.");
        var distanceToBottom = (contentPaneBox.Y + contentPaneBox.Height) - (saveBox.Y + saveBox.Height);
        Assert.True(
            distanceToBottom <= 32,
            $"Die Speichern-Aktion schließt bei {width}×{height} nicht am unteren Rand des Inhalts-Panes ab (Abstand: {distanceToBottom}px, erwartet <= 32px).");
    }

    private static async Task AssertTabBarAndContextAsync(IPage page, ILocator tabs, int width, int height)
    {
        var buttons = tabs.GetByRole(AriaRole.Button);
        await Assertions.Expect(buttons).ToHaveTextAsync(["Lesen", "Bearbeiten", "Titel", "Technische Details"]);
        var tabBar = await tabs.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Knotenreiter besitzen bei {width}×{height} keine Begrenzungsbox.");
        Assert.True(tabBar.X >= 0 && tabBar.X + tabBar.Width <= width,
            $"Knotenreiter liegen bei {width}×{height} außerhalb der erreichbaren Breite.");
        var styles = await tabs.EvaluateAsync<string[]>(
            "element => { const style = getComputedStyle(element); return [style.display, style.flexWrap]; }");
        Assert.Equal("flex", styles[0]);
        Assert.Equal("wrap", styles[1]);

        var context = page.GetByTestId("node-details-context");
        await Assertions.Expect(context).ToHaveTextAsync("Nur lesen");
        var contextBox = await context.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Der Lese-/Arbeitskontext besitzt bei {width}×{height} keine Begrenzungsbox.");
        Assert.True(contextBox.X >= tabBar.X + tabBar.Width - 1 || contextBox.Y >= tabBar.Y + tabBar.Height - 1,
            $"Der Lese-/Arbeitskontext steht bei {width}×{height} weder rechts neben noch unter den Reitern.");
        var contextMargin = await context.Locator("xpath=..")
            .EvaluateAsync<string>("element => getComputedStyle(element).marginLeft");
        Assert.True(ParsePixels(contextMargin) > 0,
            $"Der Kontext wird bei {width}×{height} nicht rechts neben den Reitern ausgerichtet.");
    }

    private static async Task AssertFollowsAsync(ILocator preceding, ILocator following, string description, int width, int height)
    {
        var precedingBox = await preceding.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Der vorgelagerte Bereich für {description} besitzt bei {width}×{height} keine Begrenzungsbox.");
        var followingBox = await following.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"{description} besitzt bei {width}×{height} keine Begrenzungsbox.");
        Assert.True(followingBox.Y >= precedingBox.Y + precedingBox.Height - 1,
            $"{description} beginnt bei {width}×{height} vor oder in der Reiterleiste.");
    }

    [Fact]
    public async Task OpenDraftDetailKeepsSharedPageContractAndActionsReachableAtDesktopWidths()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        var transactionId = await BrowserMcpAssertions.BeginTransactionAsync(_host.Address, "Browser Frame Draft");
        try
        {
            await using var browser = await ChromeBrowser.LaunchAsync();
            await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
            });

            foreach (var viewport in new[] { 1280, 1024 })
            {
                await page.SetViewportSizeAsync(viewport, 720);
                var route = $"/drafts/{transactionId:D}";
                await GotoAsync(page, route);
                var frame = page.GetByTestId("draft-page");
                await Assertions.Expect(frame).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByTestId("active-draft-link")).ToHaveTextAsync("Entwurf öffnen");
                await Assertions.Expect(page.GetByTestId("transaction-diff")).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByTestId("transaction-validation")).ToBeVisibleAsync();
                await AssertPageContractAsync(
                    page,
                    frame,
                    new RouteSpec("Offener Entwurf", route, "draft-page", "draft-page"),
                    viewport);

                foreach (var testId in new[] { "commit-transaction-button", "discard-transaction-button" })
                {
                    var action = page.GetByTestId(testId);
                    await Assertions.Expect(action).ToBeVisibleAsync();
                    await action.ScrollIntoViewIfNeededAsync();
                    var actionBox = await action.BoundingBoxAsync()
                        ?? throw new InvalidOperationException($"{testId} besitzt bei {viewport} keine Begrenzungsbox.");
                    Assert.True(
                        actionBox.X >= 0 && actionBox.X + actionBox.Width <= viewport,
                        $"{testId} liegt bei {viewport} außerhalb der erreichbaren Seitenbreite.");
                }
            }
        }
        finally
        {
            await BrowserTransactionDiscarder.DiscardAsync(_host.Address, transactionId);
        }
    }

    private static async Task AssertInitialNavigationStateAsync(IPage page, int viewport)
    {
        var navigationToggle = page.Locator("button[aria-controls='shell-navigation']");
        await Assertions.Expect(navigationToggle).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(navigationToggle).ToHaveAccessibleNameAsync(
            viewport >= 1280 ? "Navigation ausblenden" : "Navigation einblenden");

        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        if (viewport >= 1280)
            await Assertions.Expect(navigation).ToBeVisibleAsync();
        else
            await Assertions.Expect(navigation).ToHaveCountAsync(0);
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

        Assert.Equal(["none", "0px", "0px", "0px", "0px"], frameStyles);

        var expectedLeft = mainBox.X + ParsePixels(mainStyles[0]);
        var expectedRight = mainBox.X + mainBox.Width - ParsePixels(mainStyles[1]);
        Assert.True(
            Math.Abs(frameBox.X - expectedLeft) <= 1.5,
            $"{route.Name} weicht bei {viewport} an der linken Shell-Innenkante ab.");
        Assert.True(
            Math.Abs(frameBox.X + frameBox.Width - expectedRight) <= 1.5,
            $"{route.Name} weicht bei {viewport} an der rechten Shell-Innenkante ab.");

        var metrics = await page.EvaluateAsync<double[]>(
            """() => [document.documentElement.scrollWidth, document.documentElement.clientWidth, document.documentElement.scrollHeight, window.innerHeight]""");
        Assert.True(
            metrics[2] <= metrics[3],
            $"{route.Name} erzeugt bei {viewport} CSS-Pixeln einen Fensterscrollbalken ({metrics[2]} > {metrics[3]}).");
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
        new("Wissensbasis ohne Auswahl", "/knowledge?audienceId=Default", "knowledge-page", "knowledge-page"),
        new("Wissensbasis nicht gefundener Node", "/knowledge/00000000-0000-0000-0000-000000000000?audienceId=Default", "knowledge-page", "knowledge-page"),
        new("Entwürfe", "/drafts", "drafts-page", "drafts-page"),
        new("Entwurf nicht gefunden", "/drafts/00000000-0000-0000-0000-000000000000", "draft-page", "draft-page")
    ];

    private sealed record RouteSpec(string Name, string Path, string TestId, string RootClass);
}
