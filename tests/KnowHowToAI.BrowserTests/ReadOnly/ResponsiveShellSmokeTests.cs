using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

/// <summary>
/// Responsive Mindestdarstellung und Tastaturnavigation (M2.4-T1). Die
/// Desktopbreiten 1280 × 720 und 1024 × 720 sind bereits in
/// <see cref="LayoutShellSmokeTests"/> abgedeckt; diese Klasse ergänzt die
/// Zoom-/Reflow-Nachweise für die äquivalenten Layoutbreiten 640 CSS-Pixel
/// (200 %) und 320 CSS-Pixel (400 %) – kein Smartphonefreigabe, echter
/// Browserzoom bleibt Teil der manuellen Abnahme (docs/Manuelle-UI-Abnahme.md).
/// Außerdem belegt sie die gebundene Tastatursequenz ab Dokumentanfang:
/// Skip-Link nutzen, den persistenten Navigationsbutton bedienen und den
/// Seitenbereich über den Kopfbutton öffnen und schließen. Der Kontextseitenbereich besitzt im
/// M2-Produkt noch keinen echten Verbraucher (die Startseite hängt keinen
/// Kontext ein); sein Öffnen-/Schließvertrag ist als Komponentenassertion in
/// den MainLayoutTests abgesichert und wird wie der Dialog-/Toastvertrag mit
/// dem ersten echten Verbraucher in den Browserlauf aufgenommen. Alle
/// M2.4-Assertions liegen bewusst in dieser einen Klasse, damit der reguläre
/// Sammellauf nicht vom Publish-Race mehrerer Smoke-Klassen abhängt.
/// </summary>
[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class ResponsiveShellSmokeTests
{
    private readonly PublishedServerHost _host;

    public ResponsiveShellSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task NarrowReflowWidthsShowAllContentWithoutHorizontalOverflow()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 640, Height = 720 }
        });

        var response = await page.GotoAsync(_host.Address + "/knowledge?audienceId=Default", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        await CircuitProbe.WaitForInteractivityAsync(page);
        foreach (var width in new[] { 640, 320 })
        {
            await page.SetViewportSizeAsync(width, 720);
            await AssertReflowStateAsync(page, width);
        }
    }

    [Fact]
    public async Task KeyboardSequenceFromDocumentStartUsesSkipLinkNavigationAndPanel()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 720 }
        });

        var response = await page.GotoAsync(_host.Address + "/knowledge?audienceId=Default", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.Locator("main#shell-main h1")).ToBeVisibleAsync();

        // Zuerst auf den stabilen Kompaktzustand warten: der Kopfbutton
        // erscheint erst, wenn der Circuit verbunden ist und das Modul die
        // kompakte Breite gemeldet hat; ab dann ist die Tabreihenfolge final.
        var navigationToggle = page.GetByRole(AriaRole.Button, new() { Name = "Navigation einblenden", Exact = true });
        await Assertions.Expect(navigationToggle).ToBeVisibleAsync(new() { Timeout = 30_000 });

        // Start ab Dokumentanfang: der erste Tab erreicht den Skip-Link, der
        // durch den Fokus sichtbar wird, und springt auf den Hauptinhalt –
        // beobachtbar, ohne feste Wartezeit.
        var skipLink = page.GetByRole(AriaRole.Link, new() { Name = "Zum Hauptinhalt springen" });
        var main = page.Locator("#shell-main");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(skipLink).ToBeFocusedAsync();
        await Assertions.Expect(skipLink).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(main).ToBeFocusedAsync();

        // Die Navigation liegt in der kompakten Breite hinter dem
        // beschrifteten Kopfbutton. Der Button bleibt als native
        // Tastatursteuerung bedienbar.
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" }))
            .ToHaveCountAsync(0);

        await navigationToggle.FocusAsync();
        await OpenNavigationWithKeyboardAsync(page, navigationToggle);
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" }))
            .ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Wissen", Exact = true })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(navigationToggle).ToBeFocusedAsync();
    }

    /// <summary>
    /// Beobachtbarer Reflow-Zustand einer Prüfbreite: der Circuit hat die
    /// kompakte Breite übernommen (Sichtbarkeit des Kopfbuttons), alle Texte
    /// und Aktionen sind sichtbar und liegen innerhalb der Viewportbreite,
    /// die Seite läuft nicht horizontal über und eine fachlich
    /// zweidimensionale Testfläche scrollt ausschließlich in ihrem eigenen
    /// Bereich, auch per Tastatur.
    /// </summary>
    private static async Task AssertReflowStateAsync(IPage page, int viewportWidth)
    {
        // Der Schalter erscheint erst, wenn der Circuit verbunden ist und das
        // Modul die kompakte Breite gemeldet hat; das Warten auf Sichtbarkeit
        // belegt beides ohne feste Wartezeit.
        var navigationToggle = page.GetByRole(AriaRole.Button, new() { Name = "Navigation einblenden", Exact = true });
        await Assertions.Expect(navigationToggle).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" }))
            .ToHaveCountAsync(0);

        await AppendTwoDimensionalSurfaceAsync(page);

        var reachableElements = new (string Beschreibung, ILocator Locator)[]
        {
            ("Wortmarke", page.GetByRole(AriaRole.Link, new() { Name = "KnowHowToAI" })),
            ("Seitenüberschrift", page.Locator("main#shell-main h1")),
            ("Navigationskopfbutton", navigationToggle),
            ("Testfläche", page.Locator("#reflow-surface"))
        };
        foreach (var (beschreibung, locator) in reachableElements)
        {
            await Assertions.Expect(locator).ToBeVisibleAsync(new() { Timeout = 5_000 });
            var box = await locator.BoundingBoxAsync()
                ?? throw new InvalidOperationException($"{beschreibung} besitzt keine Begrenzungsbox.");
            Assert.True(
                box.X >= 0 && box.X + box.Width <= viewportWidth,
                $"{beschreibung} liegt bei {viewportWidth} CSS-Pixeln außerhalb der Viewportbreite.");
        }

        var metrics = await ReadScrollMetricsAsync(page);
        Assert.True(
            metrics.ScrollWidth <= metrics.ClientWidth,
            $"Die Seite läuft bei {viewportWidth} CSS-Pixeln horizontal über.");

        var surface = page.Locator("#reflow-surface");
        var surfaceMetrics = await ReadSurfaceScrollMetricsAsync(surface);
        Assert.True(
            surfaceMetrics.ScrollWidth > surfaceMetrics.ClientWidth,
            "Die zweidimensionale Testfläche kann horizontal nicht in ihrem eigenen Bereich den Bildlauf nutzen.");
        await surface.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        var scrollLeft = await surface.EvaluateAsync<double>("element => element.scrollLeft");
        Assert.True(scrollLeft > 0, "Die Testfläche ist nicht per Tastatur in ihrem eigenen Bereich scrollbar.");
        var metricsAfterSurfaceScroll = await ReadScrollMetricsAsync(page);
        Assert.True(
            metricsAfterSurfaceScroll.ScrollWidth <= metricsAfterSurfaceScroll.ClientWidth,
            $"Der horizontale Scroll der Testfläche erzeugt bei {viewportWidth} CSS-Pixeln einen Seitenüberlauf.");
    }

    private static async Task OpenNavigationWithKeyboardAsync(IPage page, ILocator navigationToggle)
    {
        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        await navigationToggle.PressAsync("Enter");
        await Assertions.Expect(navigation).ToBeVisibleAsync();
    }

    private static async Task AppendTwoDimensionalSurfaceAsync(IPage page) =>
        await page.EvaluateAsync(
            """
            () => {
                document.getElementById('reflow-surface')?.remove();
                const surface = document.createElement('div');
                surface.id = 'reflow-surface';
                surface.tabIndex = 0;
                surface.style.overflow = 'auto';
                surface.style.maxHeight = '240px';
                surface.style.maxWidth = '100%';
                surface.style.border = '1px solid #cbd5e1';
                const inner = document.createElement('div');
                inner.style.width = '1600px';
                inner.style.height = '900px';
                inner.textContent = 'Zweidimensionale Testfläche. ';
                surface.appendChild(inner);
                document.getElementById('shell-main').appendChild(surface);
            }
            """);

    private static async Task<(double ScrollWidth, double ClientWidth)> ReadScrollMetricsAsync(IPage page)
    {
        var metrics = await page.EvaluateAsync<double[]>(
            "() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        return (metrics[0], metrics[1]);
    }

    private static async Task<(double ScrollWidth, double ClientWidth)> ReadSurfaceScrollMetricsAsync(ILocator surface)
    {
        var metrics = await surface.EvaluateAsync<double[]>("element => [element.scrollWidth, element.clientWidth]");
        return (metrics[0], metrics[1]);
    }
}
