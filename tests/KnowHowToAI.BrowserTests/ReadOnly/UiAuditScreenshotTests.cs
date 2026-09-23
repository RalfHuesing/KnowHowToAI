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
    private static readonly int[] ReviewWidths = [1280, 1024, 640, 320];

    [Fact]
    public async Task CaptureUiAuditScreenshotsOnDemand()
    {
        Assert.SkipUnless(IsEnabled(), $"UiAudit ist deaktiviert. Setze {EnabledVariable}=1 über scripts/capture-ui-audit.ps1.");
        var output = GetOutputDirectory();
        Directory.CreateDirectory(output);
        await using var host = await PublishedServerHost.StartAsync(BrowserTestDatabaseKind.VisualShell);
        await BrowserKnowledgeSeed.EnsureVisualShellAsync(host.Address);
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
        var child = page.Locator(".knowledge-tree > .tree-node-wrapper > .tree-children-group > .tree-node-wrapper > .tree-node-row > .tree-node-select").First;
        await child.Locator(".tree-node-title").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-details")).ToBeVisibleAsync();
        await CaptureAsync(page, "04_knowledge_node-detail", output, captures);
        foreach (var width in ReviewWidths)
            await CaptureAsync(page, "04_knowledge_node-detail", output, captures, width, 720);

        await GotoAsync(page, host.Address, "/knowledge?audienceId=BrowserFallbackAudience");
        var fallbackRoot = page.GetByTestId("knowledge-tree").Locator(":scope > li > .tree-node-row > .tree-node-select");
        await fallbackRoot.Locator("xpath=..").Locator("button.tree-toggle-btn").ClickAsync();
        var fallbackNode = page.GetByText(BrowserKnowledgeSeed.FallbackNodeTitle, new() { Exact = true }).Locator("xpath=..");
        await Assertions.Expect(fallbackNode).ToBeVisibleAsync();
        await fallbackNode.Locator(".tree-node-title").ClickAsync();
        await Assertions.Expect(page.GetByTestId("node-details-availability")).ToContainTextAsync("Fallback");
        await CaptureAsync(page, "05_knowledge_fallback-detail", output, captures);

        var transactionId = await BrowserMcpAssertions.BeginTransactionAsync(host.Address, "UI Audit Draft");
        try
        {
            await GotoAsync(page, host.Address, "/drafts");
            await Assertions.Expect(page.GetByTestId($"draft-item-{transactionId:D}")).ToBeVisibleAsync();
            await CaptureAsync(page, "06_drafts_overview", output, captures);
            await GotoAsync(page, host.Address, $"/drafts/{transactionId:D}");
            await Assertions.Expect(page.GetByTestId("draft-review")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("transaction-diff")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("transaction-validation")).ToBeVisibleAsync();
            await CaptureAsync(page, "07_draft_detail", output, captures);
            foreach (var width in ReviewWidths)
                await CaptureAsync(page, "07_draft_detail", output, captures, width, 720);
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
        List<CaptureRecord> captures,
        int width = 1280,
        int height = 800)
    {
        await page.SetViewportSizeAsync(width, height);
        await page.EvaluateAsync("() => document.fonts.ready");
        var fileName = $"{scenario}_{width}x{height}.png";
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, fileName), FullPage = false });
        captures.Add(new CaptureRecord(fileName, scenario, page.Url, $"{width}x{height}", DateTimeOffset.UtcNow));
    }

    private sealed record CaptureRecord(string FileName, string Scenario, string Route, string Viewport, DateTimeOffset CapturedAtUtc);
}
