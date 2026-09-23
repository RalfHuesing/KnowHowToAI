using System.Text.RegularExpressions;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.Transactions;

[Trait("Category", "Integration")]
public sealed class KnowledgeDirectEditingSmokeTests
{
    [Fact]
    public async Task CurrentNodeCanBeEditedDirectlyAndDraftContinuesAcrossNodes()
    {
        await using var host = await PublishedServerHost.StartAsync();
        await BrowserKnowledgeSeed.EnsureVisualShellAsync(host.Address);
        using var writeLease = await BrowserWorkflowDatabaseGate.AcquireAsync();
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Guid? transactionId = null;
        try
        {
            await page.GotoAsync($"{host.Address}/knowledge?audienceId=Default", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });
            await CircuitProbe.WaitForInteractivityAsync(page);
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });

            var root = page.GetByRole(AriaRole.Treeitem).First;
            await root.Locator("button.tree-toggle-btn").ClickAsync();
            var exportNode = page.GetByRole(AriaRole.Treeitem, new() { Name = BrowserKnowledgeSeed.ExportNodeTitle });
            await Assertions.Expect(exportNode).ToBeVisibleAsync();
            var exportNodeId = await exportNode.GetAttributeAsync("data-nodeid");
            Assert.False(string.IsNullOrWhiteSpace(exportNodeId));
            await exportNode.ClickAsync();
            await page.GetByTestId("node-details-edit").ClickAsync();

            await page.GetByTestId("node-metadata-title").FillAsync("Direkt bearbeiteter Browser-Knoten");
            await page.GetByTestId("save-node-metadata").ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Ungespeicherte Änderungen" })).ToBeHiddenAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"transactionId=[0-9a-fA-F-]{36}"), new() { Timeout = 30_000 });
            transactionId = Guid.Parse(Regex.Match(new Uri(page.Url).Query, @"transactionId=([0-9a-fA-F-]{36})").Groups[1].Value);
            var updatedNode = page.GetByTestId($"treeitem-{exportNodeId}");
            await Assertions.Expect(updatedNode).ToContainTextAsync("Direkt bearbeiteter Browser-Knoten", new() { Timeout = 15_000 });
            if (await page.GetByRole(AriaRole.Alert).CountAsync() > 0)
                throw new Xunit.Sdk.XunitException($"Knowledge route failed after draft synchronization: {string.Join(Environment.NewLine, host.Log.Snapshot().TakeLast(30))}");
            await Assertions.Expect(page.GetByRole(AriaRole.Main)
                .GetByRole(AriaRole.Heading, new() { Name = "Direkt bearbeiteter Browser-Knoten", Level = 1 }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToBeVisibleAsync();

            await Assertions.Expect(page.GetByTestId("node-editing-section")).ToBeVisibleAsync();

            await page.GetByTestId("content-editor-mode-source").ClickAsync();
            await page.GetByTestId("content-editor-source").FillAsync("Direkt bearbeiteter Markdown-Inhalt.");
            await page.GetByTestId("link-drafts").ClickAsync();
            var navigationConfirmation = page.GetByRole(AriaRole.Dialog, new() { Name = "Ungespeicherte Änderungen" });
            await Assertions.Expect(navigationConfirmation).ToBeVisibleAsync();
            await navigationConfirmation.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();
            await Assertions.Expect(navigationConfirmation).ToBeHiddenAsync();
            await Assertions.Expect(page.GetByTestId("content-editor-source")).ToHaveValueAsync("Direkt bearbeiteter Markdown-Inhalt.");
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "true");
            await page.GetByTestId("content-editor-save").ClickAsync();
            await Assertions.Expect(page.GetByTestId("shell-root")).ToHaveAttributeAsync("data-ktai-dirty", "false");

            await updatedNode.Locator("button.tree-toggle-btn").ClickAsync();
            var branch = page.GetByRole(AriaRole.Treeitem, new() { Name = BrowserKnowledgeSeed.DeepNavigationBranchTitle });
            await Assertions.Expect(branch).ToBeVisibleAsync();
            await branch.Locator("button.tree-toggle-btn").ClickAsync();
            var secondNode = page.GetByRole(AriaRole.Treeitem, new() { Name = BrowserKnowledgeSeed.DeepNavigationNodeTitle });
            await Assertions.Expect(secondNode).ToBeVisibleAsync();
            await secondNode.ClickAsync();
            await page.GetByTestId("node-details-edit").ClickAsync();
            await page.GetByTestId("content-editor-mode-source").ClickAsync();
            await page.GetByTestId("content-editor-source").FillAsync("Weiterer Inhalt im selben Entwurf.");
            await page.GetByTestId("link-drafts").ClickAsync();

            navigationConfirmation = page.GetByRole(AriaRole.Dialog, new() { Name = "Ungespeicherte Änderungen" });
            await Assertions.Expect(navigationConfirmation).ToBeVisibleAsync();
            await navigationConfirmation.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();
            await Assertions.Expect(page.GetByTestId("content-editor-source")).ToHaveValueAsync("Weiterer Inhalt im selben Entwurf.");
            await page.GetByTestId("content-editor-save").ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex($@"transactionId={transactionId.Value:D}"));
            await Assertions.Expect(page.Locator("[data-ktai-dirty]")).ToHaveAttributeAsync("data-ktai-dirty", "false");
        }
        finally
        {
            if (transactionId is not null)
                await BrowserTransactionDiscarder.DiscardAsync(host.Address, transactionId.Value);
        }
    }
}
