using System.Net;
using System.Text.RegularExpressions;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class KnowledgeTreeSmokeTests
{
    private readonly PublishedServerHost _host;

    public KnowledgeTreeSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task KnowledgeTree_RendersAndNavigatesInBrowser()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();

        // 1. Einstieg öffnet den Wissensarbeitsplatz direkt.
        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        await CircuitProbe.WaitForInteractivityAsync(page);

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/knowledge$"));

// Ohne ausgewählte Zielgruppe verlangt die Shell eine explizite Auswahl.
            var audienceSelector = page.GetByTestId("context-selector-dialog");
            await Assertions.Expect(audienceSelector).ToBeVisibleAsync();
            await audienceSelector.GetByTestId("audience-option-Default").GetByRole(AriaRole.Radio).CheckAsync();
            await audienceSelector.GetByTestId("selector-apply-button").ClickAsync();
            await Assertions.Expect(audienceSelector).ToHaveCountAsync(0);
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"audienceId=Default"));

        // 3. Prüfe Hauptcontainer der Wissensseite
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("breadcrumbs")).ToBeVisibleAsync();

        // 4. Prüfe, ob Wissensbaum angezeigt wird
        var tree = page.GetByTestId("knowledge-tree");
        await Assertions.Expect(tree).ToBeVisibleAsync();

        var rootItem = page.GetByRole(AriaRole.Treeitem).First;
        await Assertions.Expect(rootItem).ToBeVisibleAsync();
        await Assertions.Expect(rootItem).ToHaveAttributeAsync("aria-level", "1");
        await Assertions.Expect(rootItem).ToHaveAttributeAsync("tabindex", "0");

        // Root expandieren über Toggle-Button
        var toggleBtn = rootItem.Locator("button.tree-toggle-btn");
        await Assertions.Expect(toggleBtn).ToBeVisibleAsync();
        await toggleBtn.ClickAsync();

        // Kindknoten auf Ebene 2 prüfen
        var childItem = page.Locator("div[role='treeitem'][aria-level='2']").First;
        await Assertions.Expect(childItem).ToBeVisibleAsync();

        // Klick auf Kindknoten -> URL wird aktualisiert
        await childItem.Locator(".tree-node-title").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/knowledge/[0-9a-fA-F-]+"));

        // Breadcrumbs zeigen den ausgewählten Knoten als aria-current="page"
        var currentBreadcrumb = page.Locator("span[aria-current='page']");
        await Assertions.Expect(currentBreadcrumb).ToBeVisibleAsync();

        // Tastaturnavigation im realen Browser prüfen und sicherstellen, dass kein Fenster-Bildlauf stattfand
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("ArrowDown");
        var scrollY = await page.EvaluateAsync<double>("() => window.scrollY");
        Assert.Equal(0, scrollY);

        // Browser-Reload prüfen: Auswahl bleibt aus Route rekonstruiert
        await page.ReloadAsync(new PageReloadOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        await Assertions.Expect(currentBreadcrumb).ToBeVisibleAsync();
    }

    [Fact]
    public async Task KnowledgeNode_DirectDeepUrlAndReloadRestoreTheSelectedPathReadOnly()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync($"{_host.Address}/knowledge", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);

        var audienceSelector = page.GetByTestId("context-selector-dialog");
        await Assertions.Expect(audienceSelector).ToBeVisibleAsync();
        await audienceSelector.GetByTestId("audience-option-Default").GetByRole(AriaRole.Radio).CheckAsync();
        await audienceSelector.GetByTestId("selector-apply-button").ClickAsync();
        await Assertions.Expect(audienceSelector).ToHaveCountAsync(0);

        var rootItem = page.GetByRole(AriaRole.Treeitem).First;
        await rootItem.Locator("button.tree-toggle-btn").ClickAsync();
        var exportItem = page.GetByRole(AriaRole.Treeitem, new() { Name = BrowserKnowledgeSeed.FallbackNodeTitle });
        await Assertions.Expect(exportItem).ToBeVisibleAsync();
        await exportItem.Locator("button.tree-toggle-btn").ClickAsync();

        var branch = page.GetByRole(AriaRole.Treeitem, new() { Name = BrowserKnowledgeSeed.DeepNavigationBranchTitle });
        await Assertions.Expect(branch).ToBeVisibleAsync();
        await branch.Locator("button.tree-toggle-btn").ClickAsync();
        var deepNode = page.GetByRole(AriaRole.Treeitem, new() { Name = BrowserKnowledgeSeed.DeepNavigationNodeTitle });
        await Assertions.Expect(deepNode).ToBeVisibleAsync();
        await deepNode.Locator(".tree-node-title").ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/knowledge/[0-9a-fA-F-]+\?audienceId=Default"));
        var selectedUrl = page.Url;
        await page.GotoAsync(selectedUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);

        await Assertions.Expect(page.GetByTestId("knowledge-page").GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToHaveTextAsync(BrowserKnowledgeSeed.DeepNavigationNodeTitle);
        await Assertions.Expect(page.GetByTestId("breadcrumbs")).ToContainTextAsync(BrowserKnowledgeSeed.FallbackNodeTitle);
        await Assertions.Expect(page.GetByTestId("breadcrumbs")).ToContainTextAsync(BrowserKnowledgeSeed.DeepNavigationBranchTitle);
        await Assertions.Expect(page.GetByTestId("node-details-section")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("node-details-requested-audience")).ToHaveTextAsync("Default");

        await page.ReloadAsync(new PageReloadOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("knowledge-page").GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToHaveTextAsync(BrowserKnowledgeSeed.DeepNavigationNodeTitle);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten", Exact = true })).ToBeVisibleAsync();
    }
}

