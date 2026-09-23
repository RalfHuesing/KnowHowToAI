using System.Text.Json;
using System.Text.RegularExpressions;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

/// <summary>On-demand screenshots for the remaining Knowledge and Drafts UI.</summary>
[Trait("Category", "UiAudit")]
public sealed class UiAuditScreenshotTests
{
    private const string EnabledVariable = "KNOWHOWTOAI_UI_AUDIT_ENABLED";
    private const string OutputVariable = "KNOWHOWTOAI_UI_AUDIT_OUTPUT_DIR";
    [Fact]
    public async Task CaptureUiAuditScreenshotsOnDemand()
    {
        Assert.SkipUnless(IsEnabled(), $"UiAudit ist deaktiviert. Setze {EnabledVariable}=1 über scripts/capture-ui-audit.ps1.");
        var output = GetOutputDirectory();
        Directory.CreateDirectory(output);
        await using var host = await PublishedServerHost.StartAsync(BrowserTestDatabaseKind.VisualShell);
        await BrowserKnowledgeSeed.EnsureWorkflowAsync(host.Address);
        await using var browser = await ChromeBrowser.LaunchAsync();
        await using var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            ReducedMotion = ReducedMotion.Reduce
        });
        var captures = new List<CaptureRecord>();

        await GotoAsync(page, host.Address, "/knowledge?audienceId=Default");
        await Assertions.Expect(page.GetByTestId("knowledge-page")).ToBeVisibleAsync();
        await CaptureAsync(page, "01_knowledge_entry", output, captures);

        await page.EvaluateAsync("() => localStorage.clear()");
        await GotoAsync(page, host.Address, "/knowledge");
        var selector = page.GetByTestId("context-selector-dialog");
        await Assertions.Expect(selector).ToBeVisibleAsync();
        await CaptureAsync(page, "02_knowledge_audience-selection", output, captures);
        await selector.GetByTestId("audience-option-Default").GetByRole(AriaRole.Radio).CheckAsync();
        await selector.GetByTestId("selector-apply-button").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/knowledge\?audienceId=Default"));
        await Assertions.Expect(page.GetByTestId("knowledge-tree")).ToBeVisibleAsync();
        await CaptureAsync(page, "03_knowledge_root", output, captures);

        var root = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
        await root.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var child = page.GetByText(BrowserKnowledgeSeed.FallbackNodeTitle, new() { Exact = true }).Locator("xpath=..");
        await Assertions.Expect(child).ToBeVisibleAsync();
        await child.Locator(".tree-node-title").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-details")).ToBeVisibleAsync();
        await CaptureAsync(page, "04_knowledge_read", output, captures);

        await page.GetByTestId("tab-Metadata").ClickAsync();
        await CaptureAsync(page, "05_knowledge_metadata", output, captures);

        await page.GetByTestId("tab-Editor").ClickAsync();
        await Assertions.Expect(page.GetByTestId("content-editor-save")).ToBeVisibleAsync();
        await CaptureAsync(page, "06_knowledge_editor", output, captures);
        await page.GetByTestId("content-editor").ScrollIntoViewIfNeededAsync();
        await CaptureAsync(page, "07_knowledge_editor_workspace", output, captures);

        await page.GetByTestId("tab-Technical").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-details-availability")).ToBeVisibleAsync();
        await CaptureAsync(page, "08_knowledge_technical", output, captures);

        await GotoAsync(page, host.Address, "/knowledge?audienceId=BrowserFallbackAudience");
        var fallbackRoot = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
        await fallbackRoot.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var fallbackNode = page.GetByText(BrowserKnowledgeSeed.FallbackNodeTitle, new() { Exact = true }).Locator("xpath=..");
        await Assertions.Expect(fallbackNode).ToBeVisibleAsync();
        await fallbackNode.Locator(".tree-node-title").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-content-fallback-context")).ToBeVisibleAsync();
        await CaptureAsync(page, "09_knowledge_fallback-read", output, captures);
        await page.GetByTestId("tab-Editor").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-editor-independent-copy")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("content-editor-save")).ToBeVisibleAsync();
        await CaptureAsync(page, "10_knowledge_fallback-editor", output, captures);
        await page.GetByTestId("content-editor").ScrollIntoViewIfNeededAsync();
        await CaptureAsync(page, "11_knowledge_fallback-editor-workspace", output, captures);

        await GotoAsync(page, host.Address, $"/knowledge?audienceId={Uri.EscapeDataString(BrowserKnowledgeSeed.HistoryAudienceId)}");
        var derivedRoot = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
        await derivedRoot.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var derivedNode = page.GetByText(BrowserKnowledgeSeed.HistoryDerivedTitle, new() { Exact = true }).Locator("xpath=..");
        await Assertions.Expect(derivedNode).ToBeVisibleAsync();
        await derivedNode.Locator(".tree-node-title").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-content-derived-context")).ToBeVisibleAsync();
        await CaptureAsync(page, "12_knowledge_derived-read", output, captures);
        await page.GetByTestId("tab-Editor").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-editor-derived-readonly")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("content-editor-save")).ToHaveCountAsync(0);
        await CaptureAsync(page, "13_knowledge_derived-editor", output, captures);

        var transactionId = await BrowserMcpAssertions.BeginTransactionAsync(host.Address, "UI Audit Draft");
        try
        {
            await GotoAsync(page, host.Address, $"/knowledge?audienceId=BrowserFallbackAudience&transactionId={transactionId:D}");
            var workingRoot = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
            await workingRoot.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
            var workingNode = page.GetByText(BrowserKnowledgeSeed.FallbackNodeTitle, new() { Exact = true }).Locator("xpath=..");
            await Assertions.Expect(workingNode).ToBeVisibleAsync();
            await workingNode.Locator(".tree-node-title").ClickAsync();
            await Assertions.Expect(page.GetByTestId("active-draft-link")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("node-content-fallback-context")).ToBeVisibleAsync();
            await page.GetByTestId("tab-Editor").ClickAsync();
            await Assertions.Expect(page.GetByTestId("node-editor-independent-copy")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("content-editor-save")).ToBeVisibleAsync();
            await CaptureAsync(page, "14_knowledge_working-fallback-editor", output, captures);

            await GotoAsync(page, host.Address, "/drafts");
            await Assertions.Expect(page.GetByTestId($"draft-item-{transactionId:D}")).ToBeVisibleAsync();
            await CaptureAsync(page, "15_drafts_overview", output, captures);
            await GotoAsync(page, host.Address, $"/drafts/{transactionId:D}");
            await Assertions.Expect(page.GetByTestId("draft-review")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("transaction-diff")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("transaction-validation")).ToBeVisibleAsync();
            await CaptureAsync(page, "16_draft_detail", output, captures);
            await page.GetByTestId("commit-transaction-button").ScrollIntoViewIfNeededAsync();
            await CaptureAsync(page, "17_draft_detail_actions", output, captures);
        }
        finally
        {
            await BrowserTransactionDiscarder.DiscardAsync(host.Address, transactionId);
        }

        var manifest = new { generatedAtUtc = DateTimeOffset.UtcNow, chromeVersion = browser.Version, screenshots = captures };
        await File.WriteAllTextAsync(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
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

    private static async Task CaptureAsync(
        IPage page,
        string scenario,
        string output,
        List<CaptureRecord> captures)
    {
        await page.EvaluateAsync("() => document.fonts.ready");
        const string viewport = "1280x800";
        var fileName = $"{scenario}_{viewport}.png";
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, fileName) });
        captures.Add(new CaptureRecord(fileName, scenario, page.Url, viewport, DateTimeOffset.UtcNow));
    }

    private sealed record CaptureRecord(string FileName, string Scenario, string Route, string Viewport, DateTimeOffset CapturedAtUtc);
}
