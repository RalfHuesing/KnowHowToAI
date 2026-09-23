using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Drafts;

[Trait("Category", "Integration")]
public sealed class DraftContextNavigationSmokeTests
{
    [Fact]
    public async Task DraftContext_PersistsAcrossKnowledgeDraftNavigationAndRefresh()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        var transactionId = await BrowserMcpAssertions.BeginTransactionAsync(host.Address, "Draft-Kontext-Smoke");
        try
        {
            var knowledgeUrl = $"{host.Address}/knowledge?audienceId=Default&transactionId={transactionId:D}";
            await page.GotoAsync(knowledgeUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("knowledge-audience-context")).ToContainTextAsync("Default");
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToHaveAttributeAsync("href", $"/drafts/{transactionId:D}");

            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToHaveAttributeAsync("href", $"/drafts/{transactionId:D}");

            await page.GetByTestId("active-draft-link").ClickAsync();
            await Assertions.Expect(page.GetByTestId("draft-page")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("draft-resume")).ToHaveAttributeAsync(
                "href", $"/knowledge?transactionId={transactionId:D}&audienceId=Default");
            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("draft-page")).ToBeVisibleAsync();

            await page.GetByTestId("draft-resume").ClickAsync();
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("knowledge-audience-context")).ToContainTextAsync("Default");
        }
        finally
        {
            await BrowserTransactionDiscarder.DiscardAsync(host.Address, transactionId);
        }
    }

    [Fact]
    public async Task DraftContext_PersistsAcrossReconnectAndHostRestart()
    {
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        PublishedServerHost? host = await PublishedServerHost.StartAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        var transactionId = await BrowserMcpAssertions.BeginTransactionAsync(host.Address, "Draft-Reconnect-Smoke");
        try
        {
            await page.GotoAsync(
                $"{host.Address}/knowledge?audienceId=Default&transactionId={transactionId:D}",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToBeVisibleAsync();

            var reconnectDialog = page.Locator("#components-reconnect-modal");
            await page.Context.SetOfflineAsync(true);
            await Assertions.Expect(reconnectDialog).ToHaveAttributeAsync("open", "", new() { Timeout = 60_000 });
            await page.Context.SetOfflineAsync(false);
            await Assertions.Expect(reconnectDialog).ToBeHiddenAsync(new() { Timeout = 30_000 });
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToBeVisibleAsync();

            var address = host.Address;
            await host.DisposeAsync();
            host = null;
            host = await PublishedServerHost.StartWithoutDatabaseCleanupAsync(address);
            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToHaveAttributeAsync(
                "href", $"/drafts/{transactionId:D}");
        }
        finally
        {
            if (host is null)
                host = await PublishedServerHost.StartAsync();

            await BrowserTransactionDiscarder.DiscardAsync(host.Address, transactionId);
            await host.DisposeAsync();
        }
    }
}
