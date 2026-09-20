using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

/// <summary>
/// Layout-Smokes gegen die echte Shell: Desktopbreite 1280 × 720 zeigt
/// Navigation, Arbeitsfläche und Seitenbereiche nebeneinander ohne
/// Horizontalüberlauf, auch mit langem Testinhalt, der per Tastatur im
/// Dokument scrollbar bleibt; der Desktop-Menübutton schließt und öffnet die
/// Navigation wieder und gibt der Arbeitsfläche die geschlossene Spalte frei.
/// Kompakte Breite 1024 × 720 klappt die Seitenbereiche über beschriftete
/// Buttons ein und aus, übergibt den Fokus an den Bereich und gibt ihn beim
/// Schließen an den Auslöser zurück.
/// </summary>
[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class LayoutShellSmokeTests
{
    private readonly PublishedServerHost _host;

    public LayoutShellSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task DesktopViewportShowsColumnsAndScrollsLongContentWithoutHorizontalOverflow()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "KnowHowToAI" })).ToBeVisibleAsync();
        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        await Assertions.Expect(navigation).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToHaveCountAsync(1);
        var navigationToggle = page.Locator("button[aria-controls='shell-navigation']");
        await Assertions.Expect(navigationToggle).ToBeVisibleAsync();
        await Assertions.Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "true");

        var navigationBox = await navigation.BoundingBoxAsync();
        var main = page.GetByRole(AriaRole.Main);
        var mainBox = (await main.BoundingBoxAsync())
            ?? throw new InvalidOperationException("Hauptinhaltsbereich besitzt keine Begrenzungsbox.");
        Assert.NotNull(navigationBox);
        Assert.True(navigationBox!.X < mainBox.X, "Navigation steht in der Desktopbreite nicht links der Arbeitsfläche.");

        await navigationToggle.ClickAsync();
        await Assertions.Expect(navigation).ToHaveCountAsync(0);
        await Assertions.Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(navigationToggle).ToHaveAccessibleNameAsync("Navigation einblenden");
        var closedMainBox = (await main.BoundingBoxAsync())
            ?? throw new InvalidOperationException("Hauptinhaltsbereich besitzt nach dem Schließen keine Begrenzungsbox.");
        Assert.True(
            closedMainBox.Width > mainBox.Width,
            "Die geschlossene Navigation gibt der Arbeitsfläche keinen zusätzlichen Platz.");

        await navigationToggle.ClickAsync();
        await Assertions.Expect(navigation).ToBeVisibleAsync();
        await Assertions.Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(navigationToggle).ToHaveAccessibleNameAsync("Navigation ausblenden");

        await AppendLongContentAsync(page);
        var metrics = await ReadScrollMetricsAsync(page);
        Assert.True(metrics.ScrollWidth <= metrics.ClientWidth, "Die Arbeitsfläche läuft horizontal über.");
        Assert.True(metrics.ScrollHeight > metrics.ClientHeight, "Der lange Testinhalt ist nicht über die Seite scrollbar.");
        await page.Locator("#layout-long-content").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var scrollY = await page.EvaluateAsync<double>("() => window.scrollY");
        Assert.True(scrollY > 0, "Die Seite blieb per Tastatur bei langem Inhalt an derselben Position.");
    }

    [Fact]
    public async Task CompactViewportCollapsesSideRegionsWithFocusAndEscapeHandoff()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 720 }
        });

        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        await CircuitProbe.WaitForInteractivityAsync(page);
        // Der Schalter erscheint erst, wenn der Circuit verbunden ist und das
        // Modul die kompakte Breite gemeldet hat; das Warten auf Sichtbarkeit
        // belegt beides ohne feste Wartezeit.
        var navigationToggle = page.Locator("button[aria-controls='shell-navigation']");
        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        await Assertions.Expect(navigationToggle).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(navigationToggle).ToHaveAccessibleNameAsync("Navigation einblenden");
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToHaveCountAsync(1);

        await OpenNavigationAsync(page, navigationToggle);
        await Assertions.Expect(navigation).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Start", Exact = true })).ToBeFocusedAsync();
        await navigationToggle.ClickAsync();
        await ExpectPanelClosedAsync(page, navigationToggle);

        await OpenNavigationAsync(page, navigationToggle);
        await Assertions.Expect(navigation).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await ExpectPanelClosedAsync(page, navigationToggle);

        await AppendLongContentAsync(page);
        var metrics = await ReadScrollMetricsAsync(page);
        Assert.True(metrics.ScrollWidth <= metrics.ClientWidth, "Die kompakte Arbeitsfläche läuft horizontal über.");
        Assert.True(metrics.ScrollHeight > metrics.ClientHeight, "Der lange Testinhalt ist in der kompakten Breite nicht über die Seite scrollbar.");
    }

    private static async Task OpenNavigationAsync(IPage page, ILocator navigationToggle)
    {
        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        await navigationToggle.ClickAsync();
        await Assertions.Expect(navigation).ToBeVisibleAsync();
    }

    private static async Task ExpectPanelClosedAsync(IPage page, ILocator navigationToggle)
    {
        await Assertions.Expect(
            page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" })).ToHaveCountAsync(0);
        await Assertions.Expect(navigationToggle).ToBeFocusedAsync();
    }

    private static async Task AppendLongContentAsync(IPage page) =>
        await page.EvaluateAsync(
            """
            () => {
                const longContent = document.createElement('div');
                longContent.id = 'layout-long-content';
                longContent.tabIndex = 0;
                longContent.textContent = 'Langer Testinhalt. '.repeat(400);
                document.getElementById('shell-main').appendChild(longContent);
            }
            """);

    private static async Task<(double ScrollWidth, double ClientWidth, double ScrollHeight, double ClientHeight)>
        ReadScrollMetricsAsync(IPage page)
    {
        var metrics = await page.EvaluateAsync<double[]>(
            "() => [document.documentElement.scrollWidth, document.documentElement.clientWidth, document.documentElement.scrollHeight, document.documentElement.clientHeight]");
        return (metrics[0], metrics[1], metrics[2], metrics[3]);
    }
}
