using System.Text.Json;
using System.Text.RegularExpressions;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

/// <summary>
/// On-demand UI-Audit-Aufnahmen. Der Test wird nur durch
/// <c>scripts/capture-ui-audit.ps1</c> aktiviert; ein normaler Browser-Testlauf
/// startet dafür weder Host noch Browser.
/// </summary>
[Trait("Category", "UiAudit")]
public sealed class UiAuditScreenshotTests
{
    private const string EnabledVariable = "KNOWHOWTOAI_UI_AUDIT_ENABLED";
    private const string OutputVariable = "KNOWHOWTOAI_UI_AUDIT_OUTPUT_DIR";

    [Fact]
    public async Task CaptureUiAuditScreenshotsOnDemand()
    {
        Assert.SkipUnless(IsEnabled(), $"UiAudit ist deaktiviert. Setze {EnabledVariable}=1 über scripts/capture-ui-audit.ps1.");

        var outputDirectory = GetOutputDirectory();
        var viewports = new[] { new ViewportSpec("desktop", 1280, 800) };
        Directory.CreateDirectory(outputDirectory);

        await using var host = await PublishedServerHost.StartAsync(BrowserTestDatabaseKind.VisualShell);
        await BrowserKnowledgeSeed.EnsureVisualShellAsync(host.Address);
        var captures = new List<CaptureRecord>();
        string? chromeVersion = null;

        foreach (var viewport in viewports)
        {
            await using var browser = await ChromeBrowser.LaunchAsync();
            chromeVersion ??= browser.Version;
            await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = viewport.Width, Height = viewport.Height },
                ReducedMotion = ReducedMotion.Reduce
            });

            await CaptureKnowledgeEntryAsync(page, host.Address, viewport, outputDirectory, captures);
            await CaptureKnowledgeAsync(page, host.Address, viewport, outputDirectory, captures);
            await CaptureSearchAsync(page, host.Address, viewport, outputDirectory, captures);
            await CaptureHistoryAsync(page, host.Address, viewport, outputDirectory, captures);
            await CaptureTransactionsAndWorkingKnowledgeAsync(page, host.Address, viewport, outputDirectory, captures);
            await CaptureAudiencesAsync(page, host.Address, viewport, outputDirectory, captures);
            await CaptureDraftsAsync(page, host.Address, viewport, outputDirectory, captures);
        }

        Assert.Equal(22, captures.Count);
        var manifest = new
        {
            generatedAtUtc = DateTimeOffset.UtcNow,
            chromeVersion,
            screenshots = captures
        };
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool IsEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable(EnabledVariable), "1", StringComparison.Ordinal)
        || string.Equals(Environment.GetEnvironmentVariable(EnabledVariable), "true", StringComparison.OrdinalIgnoreCase);

    private static string GetOutputDirectory()
    {
        var value = Environment.GetEnvironmentVariable(OutputVariable);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{OutputVariable} muss gesetzt sein.");

        var fullPath = Path.GetFullPath(value);
        if (Path.GetFileName(fullPath) is not { Length: > 0 })
            throw new InvalidOperationException("Das UiAudit-Ausgabeverzeichnis ist ungültig.");

        return fullPath;
    }

    private static async Task CaptureKnowledgeEntryAsync(IPage page, string address, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await GotoAsync(page, address, "/knowledge?audienceId=Default");
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        await CaptureAsync(page, "01_knowledge_entry", viewport, output, captures);
    }

    private static async Task CaptureKnowledgeAsync(IPage page, string address, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await page.EvaluateAsync("() => localStorage.clear()");
        await GotoAsync(page, address, "/knowledge");
        var selector = page.GetByTestId("context-selector-dialog");
        await Assertions.Expect(selector).ToBeVisibleAsync();
        await CaptureAsync(page, "02_knowledge_audience-selection", viewport, output, captures);
        await selector.GetByTestId("audience-option-Default").GetByRole(AriaRole.Radio).CheckAsync();
        await selector.GetByTestId("selector-apply-button").ClickAsync();
        await Assertions.Expect(selector).ToHaveCountAsync(0);
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/knowledge\?audienceId=Default"));
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("knowledge-tree")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Treeitem).First.GetByText("Browser-Testwissen", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "03_knowledge_root", viewport, output, captures);

        var root = page.GetByRole(AriaRole.Treeitem).First;
        await root.Locator("button.tree-toggle-btn").ClickAsync();
        var child = page.Locator("div[role='treeitem'][aria-level='2']").First;
        await Assertions.Expect(child).ToBeVisibleAsync();
        await child.ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-details")).ToBeVisibleAsync();

        // Zustand 04 darf erst aufgenommen werden, wenn die bereits gerenderte
        // Anwendungsshell und der Detailbereich im ersten Viewport angekommen
        // sind. Die Assertions verwenden ausschließlich vorhandene semantische
        // Anker; sie erzeugen, verschieben oder kaschieren keinen Produktinhalt.
        var shell = page.GetByTestId("shell-root");
        var shellHeader = page.GetByRole(AriaRole.Banner);
        var primaryNavigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        var shellMain = page.Locator("#shell-main");
        var nodeDetails = page.GetByTestId("node-details");
        var nodeDetailsTitle = page.GetByTestId("knowledge-page").GetByRole(AriaRole.Heading, new() { Level = 1 });
        await Assertions.Expect(shell).ToBeVisibleAsync();
        await Assertions.Expect(shellHeader).ToBeVisibleAsync();
        await Assertions.Expect(primaryNavigation).ToBeVisibleAsync();
        await Assertions.Expect(shellMain).ToBeVisibleAsync();
        await Assertions.Expect(nodeDetails).ToBeVisibleAsync();
        await Assertions.Expect(nodeDetailsTitle).ToBeVisibleAsync();
        await Assertions.Expect(shellHeader).ToBeInViewportAsync();
        await Assertions.Expect(primaryNavigation).ToBeInViewportAsync();
        await Assertions.Expect(nodeDetailsTitle).ToBeInViewportAsync();
        await CaptureAsync(page, "04_knowledge_node-detail", viewport, output, captures);

        await GotoAsync(page, address, "/knowledge?audienceId=BrowserDownloadAudience");
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        var fallbackRoot = page.GetByRole(AriaRole.Treeitem).First;
        await fallbackRoot.Locator("button.tree-toggle-btn").ClickAsync();
        var exportNode = page.Locator(".tree-node-title").Filter(new LocatorFilterOptions { HasText = BrowserKnowledgeSeed.ExportNodeTitle });
        await Assertions.Expect(exportNode).ToBeVisibleAsync();
        await exportNode.ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-details")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("node-details-availability")).ToContainTextAsync("Fallback");
        await CaptureAsync(page, "05_knowledge_fallback-detail", viewport, output, captures);
    }

    private static async Task CaptureSearchAsync(IPage page, string address, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await GotoAsync(page, address, "/search?audienceId=Default");
        await Assertions.Expect(page.GetByTestId("search-page")).ToBeVisibleAsync();
        await CaptureAsync(page, "06_search_empty", viewport, output, captures);
        await page.GetByTestId("search-text").FillAsync("Markdown-Download");
        await page.GetByTestId("search-submit").ClickAsync();
        await Assertions.Expect(page.GetByTestId("search-results")).ToBeVisibleAsync();
        var searchResults = page.GetByTestId("search-results");
        await Assertions.Expect(searchResults.Locator(".search-results__summary")).ToBeInViewportAsync();
        var firstResult = searchResults.Locator(".search-results__node").First;
        await Assertions.Expect(firstResult).ToBeVisibleAsync();
        await Assertions.Expect(firstResult).ToBeInViewportAsync();
        var firstResultBox = await firstResult.BoundingBoxAsync();
        Assert.NotNull(firstResultBox);
        Assert.True(
            firstResultBox.Y + firstResultBox.Height <= viewport.Height,
            "Die vollständige erste Suchtrefferkarte muss im ersten Viewport sichtbar sein.");
        await CaptureAsync(page, "07_search_results", viewport, output, captures);
    }

    private static async Task CaptureHistoryAsync(IPage page, string address, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await GotoAsync(page, address, "/history?audienceId=Default");
        await Assertions.Expect(page.GetByTestId("history-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("snapshot-list")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("history-comparison-context")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("history-selection-status")).ToContainTextAsync("Ausgang");
        await CaptureAsync(page, "08_history_list", viewport, output, captures);

        var snapshotItems = page.GetByTestId("snapshot-list").Locator(":scope > li");
        var diffScenario = "09_history_snapshot-selection-required";
        if (await snapshotItems.CountAsync() > 1)
        {
            var baseAction = snapshotItems.Last.Locator("[data-testid^='snapshot-diff-base-']");
            var targetAction = snapshotItems.First.Locator("[data-testid^='snapshot-diff-target-']");
            await baseAction.ClickAsync();
            await targetAction.ClickAsync();

            var selectionStatus = page.GetByTestId("history-selection-status");
            await Assertions.Expect(selectionStatus).ToContainTextAsync("Ausgang");
            await Assertions.Expect(selectionStatus).ToContainTextAsync("Ziel");

            var diffList = page.GetByTestId("snapshot-diff-list");
            var emptyDiff = page.GetByTestId("snapshot-diff-empty");
            var selectionRequired = page.GetByTestId("snapshot-diff-selection-required");
            await Assertions.Expect(selectionRequired).ToBeHiddenAsync();
            var diffResult = page.Locator("[data-testid='snapshot-diff-list'], [data-testid='snapshot-diff-empty']");
            await diffResult.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            if (await diffList.IsVisibleAsync())
            {
                await Assertions.Expect(diffList).ToBeVisibleAsync();
                await Assertions.Expect(diffList.Locator("li").First).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByTestId("snapshot-diff-summary")).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByTestId("snapshot-diff-summary")).ToContainTextAsync("Snapshot");
                await Assertions.Expect(diffList.Locator("li").First).ToBeInViewportAsync();
                diffScenario = "09_history_snapshot-diff";
            }
            else if (await emptyDiff.CountAsync() > 0)
            {
                await Assertions.Expect(emptyDiff).ToBeVisibleAsync();
                diffScenario = "09_history_snapshot-empty";
            }
            else
            {
                await Assertions.Expect(selectionRequired).ToBeVisibleAsync();
            }
        }
        else
        {
            await Assertions.Expect(page.GetByTestId("snapshot-diff-selection-required")).ToBeVisibleAsync();
        }
        await CaptureAsync(page, diffScenario, viewport, output, captures);
    }

    private static async Task CaptureTransactionsAndWorkingKnowledgeAsync(IPage page, string address, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await GotoAsync(page, address, "/transactions");
        await Assertions.Expect(page.GetByTestId("transactions-page")).ToBeVisibleAsync();
        await CaptureAsync(page, "10_transactions_overview", viewport, output, captures);

        await page.GetByTestId("tx-purpose-input").FillAsync("UI Audit Working Transaction");
        await page.GetByTestId("begin-transaction-button").ClickAsync();
        await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync();
        var transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
        try
        {
            await GotoAsync(page, address, "/transactions");
            await Assertions.Expect(page.GetByTestId("transaction-item-" + transactionId)).ToBeVisibleAsync();
            await CaptureAsync(page, "11_transactions_open", viewport, output, captures);

            await GotoAsync(page, address, $"/transactions/{transactionId:D}");
            await Assertions.Expect(page.GetByTestId("transaction-diff")).ToBeVisibleAsync();
            await CaptureAsync(page, "12_transaction_detail", viewport, output, captures);

            await page.GetByTestId("commit-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Übernehmen", Exact = true })).ToBeVisibleAsync();
            await CaptureAsync(page, "13_transaction_commit-dialog", viewport, output, captures);
            await page.GetByRole(AriaRole.Button, new() { Name = "Abbrechen", Exact = true }).ClickAsync();

            await page.GetByTestId("discard-transaction-button").ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Entwurf verwerfen", Exact = true })).ToBeVisibleAsync();
            await CaptureAsync(page, "14_transaction_discard-dialog", viewport, output, captures);
            await page.GetByRole(AriaRole.Button, new() { Name = "Abbrechen", Exact = true }).ClickAsync();

            await GotoAsync(page, address, $"/knowledge?audienceId=Default&transactionId={transactionId:D}");
            await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
            var workingRoot = page.GetByRole(AriaRole.Treeitem).First;
            await workingRoot.ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-details-section")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("node-details-edit")).ToHaveCountAsync(0);
            await CaptureAsync(page, "15_working-knowledge-root-read-only", viewport, output, captures);

            await GotoAsync(page, address, "/knowledge?audienceId=Default");
            var root = page.GetByRole(AriaRole.Treeitem).First;
            await root.ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-details-section")).ToBeVisibleAsync();
            await CaptureAsync(page, "16_knowledge-root-document", viewport, output, captures);

            await root.Locator("button.tree-toggle-btn").ClickAsync();
            var child = page.Locator(".tree-node-title").Filter(new LocatorFilterOptions { HasText = BrowserKnowledgeSeed.ExportNodeTitle });
            await Assertions.Expect(child).ToBeVisibleAsync();
            await child.ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-details-section")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("node-details-edit")).ToHaveCountAsync(0);
            await CaptureAsync(page, "17_knowledge-selected-node-read-only", viewport, output, captures);
        }
        finally
        {
            await BrowserTransactionDiscarder.DiscardAsync(address, transactionId);
        }
    }

    private static async Task CaptureDraftsAsync(IPage page, string address, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await GotoAsync(page, address, "/transactions");
        await page.GetByTestId("tx-purpose-input").FillAsync("UI Audit Draft");
        await page.GetByTestId("begin-transaction-button").ClickAsync();
        await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync();
        var transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
        try
        {
            await GotoAsync(page, address, "/drafts");
            await Assertions.Expect(page.GetByTestId($"draft-item-{transactionId:D}")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("drafts-list")).ToBeInViewportAsync();
            await CaptureAsync(page, "21_drafts_overview", viewport, output, captures);

            await page.GetByTestId($"draft-open-{transactionId:D}").ClickAsync();
            await Assertions.Expect(page.GetByTestId("draft-review")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Entwurf: UI Audit Draft");
            await Assertions.Expect(page.GetByTestId("transaction-diff")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("transaction-validation")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("discard-transaction-button")).ToBeVisibleAsync();
            await CaptureAsync(page, "22_draft_detail", viewport, output, captures);
        }
        finally
        {
            await BrowserTransactionDiscarder.DiscardAsync(address, transactionId);
        }
    }

    private static async Task CaptureAudiencesAsync(IPage page, string address, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await GotoAsync(page, address, "/audiences?audienceId=Default");
        await Assertions.Expect(page.GetByTestId("audiences-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("audiences-readonly")).ToBeVisibleAsync();
        await CaptureAsync(page, "18_audiences_readonly", viewport, output, captures);

        await GotoAsync(page, address, "/transactions");
        await page.GetByTestId("tx-purpose-input").FillAsync("UI Audit Audiences Transaction");
        await page.GetByTestId("begin-transaction-button").ClickAsync();
        await Assertions.Expect(page.GetByTestId("transaction-page")).ToBeVisibleAsync();
        var transactionId = await BrowserTransactionReader.ReadTransactionIdAsync(page);
        try
        {
            await GotoAsync(page, address, $"/audiences?audienceId=Default&transactionId={transactionId:D}");
            await Assertions.Expect(page.GetByTestId("audiences-list")).ToBeVisibleAsync();
            await CaptureAsync(page, "19_audiences_working", viewport, output, captures);
            var delete = page.GetByTestId("audience-delete-BrowserDownloadAudience");
            await Assertions.Expect(delete).ToBeVisibleAsync();
            await delete.ClickAsync();
            var confirmation = page.Locator("dialog.confirmation-app-dialog[open]");
            await Assertions.Expect(confirmation).ToBeVisibleAsync();
            await Assertions.Expect(confirmation.GetByRole(AriaRole.Heading, new() { Name = "Zielgruppe „BrowserDownloadAudience“ löschen?", Exact = true })).ToBeVisibleAsync();
            var confirmButton = confirmation.GetByRole(AriaRole.Button, new() { Name = "Zielgruppe löschen" });
            await Assertions.Expect(confirmButton).ToBeVisibleAsync();
            await confirmation.ScrollIntoViewIfNeededAsync();
            await confirmButton.FocusAsync();
            await Assertions.Expect(confirmButton).ToBeFocusedAsync();
            await CaptureAsync(page, "20_audiences_delete-dialog", viewport, output, captures);
        }
        finally
        {
            await BrowserTransactionDiscarder.DiscardAsync(address, transactionId);
        }
    }

    private static async Task GotoAsync(IPage page, string address, string route)
    {
        var response = await page.GotoAsync(address + route, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal(200, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);
    }

    private static async Task CaptureAsync(IPage page, string scenario, ViewportSpec viewport, string output, List<CaptureRecord> captures)
    {
        await page.EvaluateAsync(
            """
            () => {
                let style = document.getElementById('ui-audit-mask');
                if (!style) {
                    style = document.createElement('style');
                    style.id = 'ui-audit-mask';
                    document.head.appendChild(style);
                }
                style.textContent = '*, *::before, *::after { animation-duration: 0s !important; animation-delay: 0s !important; transition: none !important; caret-color: transparent !important; scroll-behavior: auto !important; } [data-volatile], [data-testid="tx-id"], [data-testid="tx-created-at"], [data-testid="tx-base-snapshot"], [data-testid="tx-working-snapshot"], [data-testid="tx-change-version"], [data-testid="context-base-snapshot"], [data-testid="context-change-version"], .node-meta-id, .node-meta-revision { visibility: hidden !important; }';
            }
            """);
        await page.EvaluateAsync("() => document.fonts.ready");
        var fileName = $"{scenario}_{viewport.Name}_{viewport.Width}x{viewport.Height}.png";
        var path = Path.Combine(output, fileName);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = false });
        captures.Add(new CaptureRecord(fileName, scenario, page.Url, viewport.Name, DateTimeOffset.UtcNow));
    }

    private sealed record ViewportSpec(string Name, int Width, int Height);

    private sealed record CaptureRecord(string FileName, string Scenario, string Route, string Viewport, DateTimeOffset CapturedAtUtc);
}
