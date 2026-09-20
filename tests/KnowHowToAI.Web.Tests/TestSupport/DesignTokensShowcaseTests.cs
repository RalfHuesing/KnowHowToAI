using Bunit;
using KnowHowToAI.TestSupport;
using Microsoft.Playwright;
 
namespace KnowHowToAI.Web.Tests.TestSupport;

/// <summary>
/// Rendert das Showcase-Fixture (nur Testunterstützung, keine Route im
/// Produkt) als bUnit-Markup, bettet es mit der globalen Stylesheetdatei und
/// den isolierten Komponenten-Stylesheets in ein Light-Theme-Dokument ein
/// und belegt per Headless Chrome (Playwright, SetContentAsync) die
/// berechneten Tokenwerte sowie den Light-Theme-Screenshot.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DesignTokensShowcaseTests : ShellTestContext
{
    private const string ShowcaseStyle = """
        .showcase { max-width: 720px; margin: 0 auto; padding: var(--ktai-space-6); }
        .showcase-secondary { color: var(--ktai-color-text-secondary); }
        .showcase-card {
            background-color: var(--ktai-color-surface);
            border: 1px solid var(--ktai-color-border);
            border-radius: var(--ktai-radius-medium);
            box-shadow: var(--ktai-shadow-surface);
            padding: var(--ktai-space-4);
            margin-top: var(--ktai-space-4);
        }
        .showcase-status-row { display: flex; flex-wrap: wrap; gap: var(--ktai-space-2); margin: 0; }
        .showcase-actions { display: flex; flex-wrap: wrap; gap: var(--ktai-space-2); }
        .showcase-button-primary {
            background-color: var(--ktai-color-primary);
            color: var(--ktai-color-primary-contrast);
            border: none;
            border-radius: var(--ktai-radius-small);
            padding: var(--ktai-space-2) var(--ktai-space-4);
        }
        .showcase-button-primary:hover:enabled { background-color: var(--ktai-color-primary-hover); }
        .showcase-button-primary:active:enabled { background-color: var(--ktai-color-primary-active); }
        .showcase-button-secondary {
            background-color: var(--ktai-color-surface);
            color: var(--ktai-color-text);
            border: 1px solid var(--ktai-color-border);
            border-radius: var(--ktai-radius-small);
            padding: var(--ktai-space-2) var(--ktai-space-4);
        }
        """;

    [Fact]
    public async Task ShowcaseRendersThemeAndCapturesTheLightThemeScreenshot()
    {
        ChromeStablePreflight.EnsureIsInstalled();

        var showcaseMarkup = Render<DesignTokensShowcase>().Markup;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "chrome",
            Headless = true
        });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        await page.SetContentAsync(
            BuildShowcaseDocument(showcaseMarkup),
            new PageSetContentOptions { WaitUntil = WaitUntilState.Load });

        await AssertComputedStyleAsync(page, "body", "font-family", "\"Segoe UI\", Arial, sans-serif");
        await AssertComputedStyleAsync(page, "body", "font-size", "16px");
        await AssertComputedStyleAsync(page, "body", "line-height", "24px");
        await AssertComputedStyleAsync(page, ".shell-brand", "font-weight", "600");
        await AssertComputedStyleAsync(page, "p.showcase-actions button:first-child", "background-color", "rgb(37, 99, 235)");
        await AssertComputedStyleAsync(page, ".showcase-button-primary[disabled]", "opacity", "0.6");
        await AssertComputedStyleAsync(page, ".app-status--neutral", "color", "rgb(75, 85, 99)");
        await AssertComputedStyleAsync(page, ".app-status--aktiv", "color", "rgb(37, 99, 235)");
        await AssertComputedStyleAsync(page, ".app-status--erfolg", "color", "rgb(21, 128, 61)");
        await AssertComputedStyleAsync(page, ".app-status--warnung", "color", "rgb(180, 83, 9)");
        await AssertComputedStyleAsync(page, ".app-status--fehler", "color", "rgb(185, 28, 28)");

        var input = page.Locator("#design-tokens-showcase-input");
        await input.FocusAsync();
        await AssertComputedStyleAsync(page, "#design-tokens-showcase-input", "outline-width", "3px");
        await AssertComputedStyleAsync(page, "#design-tokens-showcase-input", "outline-style", "solid");
        await AssertComputedStyleAsync(page, "#design-tokens-showcase-input", "outline-color", "rgb(37, 99, 235)");
        await AssertComputedStyleAsync(page, "#design-tokens-showcase-input", "outline-offset", "2px");

        var screenshotPath = Path.Combine(
            TestRepositoryRoot.Resolve(),
            "TestResults",
            "DesignTokens-Showcase-light.png");
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true
        });

        Assert.True(File.Exists(screenshotPath), "Der Light-Theme-Screenshot wurde nicht geschrieben.");
    }

    private static async Task AssertComputedStyleAsync(
        IPage page,
        string selector,
        string propertyName,
        string expectedValue)
    {
        var actualValue = await page.Locator(selector).EvaluateAsync<string>(
            $"element => getComputedStyle(element)['{propertyName}']");

        Assert.True(
            string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
            $"Berechneter Stil {propertyName} von {selector} ist '{actualValue}', erwartet '{expectedValue}'.");
    }

    private static string BuildShowcaseDocument(string showcaseMarkup)
    {
        var globalStyles = DesignTokensTests.LoadStylesheetWithImports(Path.Combine(
            TestRepositoryRoot.Resolve(),
            "src",
            "KnowHowToAI.Server",
            "wwwroot",
            "css",
            "app.css"));
        var shellStyles = ReadIsolatedStylesheet(Path.Combine("Layout", "Shell", "MainLayout.razor.css"));
        var statusStyles = ReadIsolatedStylesheet(Path.Combine("Shared", "Feedback", "AppStatus.razor.css"));

        return $"""
            <!DOCTYPE html>
            <html lang="de">
            <head>
                <meta charset="utf-8" />
                <title>Design-Tokens-Showcase</title>
                <style>{globalStyles}</style>
                <style>{shellStyles}</style>
                <style>{statusStyles}</style>
                <style>{ShowcaseStyle}</style>
            </head>
            <body>{showcaseMarkup}</body>
            </html>
            """;
    }

    /// <summary>
    /// Liest ein Blazor-CSS-Isolations-Stylesheet aus dem Quellprojekt und
    /// entfernt die Scope-Attributselektoren, damit die Regeln auf das ohne
    /// Isolationsattribute gerenderte bUnit-Markup wirken.
    /// </summary>
    private static string ReadIsolatedStylesheet(string fileName)
    {
        var path = Path.Combine(
            TestRepositoryRoot.Resolve(),
            "src",
            "KnowHowToAI.Server",
            "Web",
            "Components",
            fileName);
        var content = File.ReadAllText(path);

        return System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\[b-[a-z0-9]+\]",
            string.Empty);
    }
}
