using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Transactions;

[Trait("Category", "Integration")]
public sealed class TransactionStateAndNavigationSmokeTests
{
    [Fact]
    public async Task TransactionContext_PreservedAcrossRefreshAndBrowserNavigation()
    {
        await using var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        // 1. Transactions-Seite aufrufen und neue Transaction beginnen
        await page.GotoAsync($"{host.Address}/transactions", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);

        // Formular ausfüllen und Transaction starten
        await page.GetByTestId("tx-purpose-input").FillAsync("Browser Smoke Test Transaction");
        await page.GetByTestId("begin-transaction-button").ClickAsync();

        // Warten auf Navigation zur Detailseite /transactions/{id}
        await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("tx-title")).ToHaveTextAsync("Browser Smoke Test Transaction");

        // Im Wissensbaum öffnen
        var openKnowledgeLink = page.GetByTestId("tx-open-knowledge-link");
        await Assertions.Expect(openKnowledgeLink).ToBeVisibleAsync();
        await openKnowledgeLink.ClickAsync();

        // 2. Wissensseite mit Transaktionskontext prüfen
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        var contextBar = page.Locator(".knowledge-context");
        await Assertions.Expect(contextBar).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("context-base-snapshot")).ToBeVisibleAsync();

        // 3. Refresh (F5 / page.ReloadAsync)
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CircuitProbe.WaitForInteractivityAsync(page);

        // Nach Reload: Transaktionskontext und Base-Snapshot aus URL restauriert
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("context-base-snapshot")).ToBeVisibleAsync();

        // 4. Browsernavigation: Zu Suche mit selbem Transaktionskontext navigieren
        var uri = new Uri(page.Url);
        await page.GotoAsync($"{host.Address}/search{uri.Query}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("search-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("context-base-snapshot")).ToBeVisibleAsync();

        // Browser-Zurück
        await page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("context-base-snapshot")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task TransactionContext_PreservedAcrossReconnectAndHostRestart()
    {
        var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        // 1. Transaction beginnen
        await page.GotoAsync($"{host.Address}/transactions", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);

        await page.GetByTestId("tx-purpose-input").FillAsync("Reconnect Smoke Test Transaction");
        await page.GetByTestId("begin-transaction-button").ClickAsync();
        await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await page.GetByTestId("tx-open-knowledge-link").ClickAsync();
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("context-base-snapshot")).ToBeVisibleAsync();

        // 2. Reconnect: Verbindung unterbrechen
        var reconnectDialog = page.Locator("#components-reconnect-modal");
        await page.Context.SetOfflineAsync(true);
        await Assertions.Expect(reconnectDialog).ToHaveAttributeAsync("open", "", new() { Timeout = 60_000 });

        // Verbindung wiederherstellen
        await page.Context.SetOfflineAsync(false);
        await Assertions.Expect(reconnectDialog).ToBeHiddenAsync(new() { Timeout = 30_000 });

        // Zustand bleibt stabil
        await Assertions.Expect(page.GetByTestId("context-base-snapshot")).ToBeVisibleAsync();

        // 3. Prozessneustart am selben Origin
        var address = host.Address;
        await host.DisposeAsync();
        await using var restartedHost = await PublishedServerHost.StartAsync(address);

        // Seite nach Neustart neu laden
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CircuitProbe.WaitForInteractivityAsync(page);

        // Transaktionszustand und Base-Snapshot sind aus der Datenbank über die URL restauriert
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("context-base-snapshot")).ToBeVisibleAsync();
    }
}
