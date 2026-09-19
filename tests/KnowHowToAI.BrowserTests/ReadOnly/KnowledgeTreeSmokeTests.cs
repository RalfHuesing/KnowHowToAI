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

        // 1. Dashboard öffnen und Interaktivität sicherstellen
        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        await CircuitProbe.WaitForInteractivityAsync(page);

        // 2. Über Link zum Wissensbaum navigieren
        var knowledgeLink = page.GetByTestId("link-knowledge");
        await Assertions.Expect(knowledgeLink).ToBeVisibleAsync();
        await knowledgeLink.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/knowledge"));

        // 3. Prüfe Hauptcontainer der Wissensseite
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("breadcrumbs")).ToBeVisibleAsync();

        // 4. Prüfe, ob Wissensbaum oder Leerer Zustand angezeigt wird
        var tree = page.GetByTestId("knowledge-tree");
        var emptyTree = page.GetByTestId("tree-empty");

        var hasTree = await tree.CountAsync() > 0;
        if (hasTree)
        {
            await Assertions.Expect(tree).ToBeVisibleAsync();
            var rootItem = page.GetByRole(AriaRole.Treeitem).First;
            await Assertions.Expect(rootItem).ToBeVisibleAsync();
            await Assertions.Expect(rootItem).ToHaveAttributeAsync("aria-level", "1");
            await Assertions.Expect(rootItem).ToHaveAttributeAsync("tabindex", "0");

            // Wenn der Root Kinder hat (Toggle-Button vorhanden), expandieren
            var toggleBtn = rootItem.Locator("button.tree-toggle-btn");
            if (await toggleBtn.CountAsync() > 0)
            {
                await toggleBtn.ClickAsync();

                // Kindknoten auf Ebene 2 prüfen
                var childItem = page.Locator("div[role='treeitem'][aria-level='2']").First;
                if (await childItem.CountAsync() > 0)
                {
                    await Assertions.Expect(childItem).ToBeVisibleAsync();

                    // Klick auf Kindknoten -> URL wird aktualisiert
                    await childItem.ClickAsync();
                    await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/knowledge/[0-9a-fA-F-]+"));

                    // Breadcrumbs zeigen den ausgewählten Knoten als aria-current="page"
                    var currentBreadcrumb = page.Locator("span[aria-current='page']");
                    await Assertions.Expect(currentBreadcrumb).ToBeVisibleAsync();

                    // Tastaturnavigation im realen Browser prüfen
                    await page.Keyboard.PressAsync("ArrowUp");
                    await page.Keyboard.PressAsync("ArrowDown");

                    // Browser-Reload prüfen: Auswahl bleibt aus Route rekonstruiert
                    await page.ReloadAsync(new PageReloadOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30_000
                    });

                    await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
                    await Assertions.Expect(currentBreadcrumb).ToBeVisibleAsync();
                }
            }
        }
        else
        {
            // Leerer Zustand bei initialer Datenbank ohne Root
            await Assertions.Expect(emptyTree).ToBeVisibleAsync();
            await Assertions.Expect(emptyTree).ToHaveTextAsync("Keine Wissensknoten vorhanden.");
        }
    }
}
