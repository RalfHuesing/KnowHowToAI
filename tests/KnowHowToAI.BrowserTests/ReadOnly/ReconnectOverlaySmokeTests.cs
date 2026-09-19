using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Trait("Category", "Integration")]
public sealed class ReconnectOverlaySmokeTests
{
    [Fact]
    public async Task InterruptedConnection_ShowsReconnectOverlayAndContinuesCircuitAfterSuccessfulReconnect()
    {
        await using var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        await page.GotoAsync(host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        // Circuit bereit machen: Interaktivitätsnachweis wie im M1-Smoke.
        var interactionStatus = page.GetByTestId("interaction-status");
        await CircuitProbe.WaitForInteractivityAsync(page);

        var reconnectDialog = page.Locator("#components-reconnect-modal");
        await page.Context.SetOfflineAsync(true);
        // SignalR erkennt den Verbindungsverlust erst mit dem Ablauf des
        // KeepAlive-Timeouts (etwa 30 Sekunden); gewartet wird auf den
        // beobachtbaren Dialogzustand, nicht auf feste Zeiten.
        await Assertions.Expect(reconnectDialog).ToHaveAttributeAsync("open", "", new() { Timeout = 60_000 });
        // Beim Retryübergang setzt das Framework "components-reconnect-retrying",
        // ohne "components-reconnect-show" zu entfernen: der Wiederherstellungs-
        // Absatz bleibt neben dem Countdown-Absatz sichtbar. Gezählt wird daher
        // nicht die Anzahl sichtbarer Zustände, sondern genau der geprüfte
        // Absatz der Wiederherstellungsphase.
        var reconnectingState = reconnectDialog.Locator(
            "p.components-reconnect-state.components-reconnect-first-attempt-visible");
        await Assertions.Expect(reconnectingState).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(reconnectingState).ToContainTextAsync("Verbindung wird wiederhergestellt", new() { Timeout = 5_000 });
        Assert.Equal("components-reconnect-modal", await page.EvaluateAsync<string?>("document.activeElement?.id"));

        await page.Context.SetOfflineAsync(false);
        await Assertions.Expect(reconnectDialog).ToBeHiddenAsync(new() { Timeout = 30_000 });

        // Der Circuit ist derselbe geblieben: der vor dem Abbruch gesetzte
        // Status steht weiterhin da, ohne dass die Seite neu geladen hätte,
        // und der M1-Interaktionsnachweis antwortet erneut.
        await Assertions.Expect(interactionStatus).ToHaveTextAsync(
            "Interaktivität ist verfügbar.", new() { Timeout = 5_000 });
        await CircuitProbe.WaitForInteractivityAsync(page);

        // Erfolgreicher Reconnect zeigt keinen fachlichen Erfolgshinweis.
        Assert.Empty(await page.Locator(".toast-region__toast").AllAsync());
    }

    [Fact]
    public async Task ExpiredCircuitAfterHostRestart_ShowsSessionLostOverlayAndReloadRestoresShell()
    {
        var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        await page.GotoAsync(host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        // "Shell bereit" erscheint bereits im Prerendering; erst der
        // beobachtbare Interaktionsnachweis belegt, dass ein Circuit
        // etabliert ist, dessen Verlust die Reconnect-Oberfläche auslöst.
        await Assertions.Expect(page.GetByTestId("shell-status")).ToContainTextAsync(
            "Shell bereit", new() { Timeout = 15_000 });
        await CircuitProbe.WaitForInteractivityAsync(page);

        // Hostneustart am selben Loopback-Origin: der Circuit des Browsers
        // existiert im neuen Prozess nicht mehr und wird beim nächsten
        // Reconnect-Versuch abgelehnt.
        var address = host.Address;
        await host.DisposeAsync();
        await using var restartedHost = await PublishedServerHost.StartAsync(address);

        var reconnectDialog = page.Locator("#components-reconnect-modal");
        await Assertions.Expect(reconnectDialog).ToHaveAttributeAsync("open", "", new() { Timeout = 60_000 });
        // Im abgelehnten Zustand entfernt das Framework sämtliche
        // Zustandsklassen und setzt ausschließlich "components-reconnect-
        // rejected"; geprüft wird genau dieser Absatz statt einer Anzahl
        // aller sichtbaren Zustandsabsätze.
        var rejectedState = reconnectDialog.Locator(
            "p.components-reconnect-state.components-reconnect-rejected-visible");
        await Assertions.Expect(rejectedState).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Assertions.Expect(rejectedState).ToContainTextAsync("Sitzung nicht mehr verfügbar", new() { Timeout = 5_000 });

        var reloadButton = reconnectDialog.GetByRole(AriaRole.Button, new() { Name = "Seite neu laden" });
        await Assertions.Expect(reloadButton).ToBeVisibleAsync(new() { Timeout = 5_000 });
        Assert.Equal("components-reconnect-reload-button", await page.EvaluateAsync<string?>("document.activeElement?.id"));

        await reloadButton.ClickAsync();
        await Assertions.Expect(page.GetByTestId("shell-status")).ToContainTextAsync(
            "Shell bereit", new() { Timeout = 30_000 });
    }
}
