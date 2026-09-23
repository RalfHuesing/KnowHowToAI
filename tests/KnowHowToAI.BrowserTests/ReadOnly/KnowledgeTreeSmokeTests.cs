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

        var rootItem = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
        await Assertions.Expect(rootItem).ToBeVisibleAsync();
        await Assertions.Expect(rootItem).ToHaveAttributeAsync("data-nodeid", new Regex("[0-9a-fA-F-]{36}"));
        await Assertions.Expect(rootItem).ToHaveAttributeAsync("aria-pressed", "false");

        // Root expandieren über Toggle-Button
        var toggleBtn = rootItem.Locator("xpath=..").Locator("button.tree-toggle-btn");
        await Assertions.Expect(toggleBtn).ToBeVisibleAsync();
        await toggleBtn.ClickAsync();

        // Kindknoten auf Ebene 2 prüfen
        var childItem = page.Locator(".knowledge-tree > .tree-node-wrapper > .tree-children-group > .tree-node-wrapper > .tree-node-row > .tree-node-select").First;
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

        var rootItem = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
        await rootItem.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var exportItem = page.GetByText(BrowserKnowledgeSeed.FallbackNodeTitle, new() { Exact = true }).Locator("xpath=..");
        await Assertions.Expect(exportItem).ToBeVisibleAsync();
        await exportItem.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();

        var branch = page.GetByText(BrowserKnowledgeSeed.DeepNavigationBranchTitle, new() { Exact = true }).Locator("xpath=..");
        await Assertions.Expect(branch).ToBeVisibleAsync();
        await branch.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var deepNode = page.GetByText(BrowserKnowledgeSeed.DeepNavigationNodeTitle, new() { Exact = true }).Locator("xpath=..");
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
        await Assertions.Expect(page.GetByTestId("node-details-tabs").GetByRole(AriaRole.Button)).ToHaveCountAsync(4);
        await Assertions.Expect(page.GetByTestId("node-view-read")).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(page.GetByTestId("node-details-requested-audience")).ToHaveTextAsync("Default");
        await AssertNodeIdentifierIsNotReadableTextAsync(page);

        await page.ReloadAsync(new PageReloadOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("knowledge-page").GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToHaveTextAsync(BrowserKnowledgeSeed.DeepNavigationNodeTitle);
        await Assertions.Expect(page.GetByTestId("node-details-tabs").GetByRole(AriaRole.Button)).ToHaveCountAsync(4);
        await Assertions.Expect(page.GetByTestId("node-view-read")).ToHaveAttributeAsync("aria-pressed", "true");
        await AssertNodeIdentifierIsNotReadableTextAsync(page);
    }

    private static async Task AssertNodeIdentifierIsNotReadableTextAsync(IPage page)
    {
        var match = Regex.Match(page.Url, @"/knowledge/(?<nodeId>[0-9a-fA-F-]+)");
        Assert.True(match.Success, "Die unveränderte Node-Route muss eine Node-ID enthalten.");
        var nodeId = Guid.Parse(match.Groups["nodeId"].Value);
        var readableText = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain(nodeId.ToString(), readableText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nodeId.ToString("N")[..8], readableText, StringComparison.OrdinalIgnoreCase);
    }
}
