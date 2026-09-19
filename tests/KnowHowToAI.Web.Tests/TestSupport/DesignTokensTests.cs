using System.Globalization;
using System.Text.RegularExpressions;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Web.Tests.TestSupport;

/// <summary>
/// Belegt die zentralen Design-Tokens aus <c>wwwroot/css/app.css</c>
/// (M2.1-T2): feste Startwerte, berechnete WCAG-Kontraste für Text, Status
/// und Fokus sowie die Regel, dass der Fokusring nie entfernt wird und
/// Komponenten keine verstreuten globalen Designwerte enthalten.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DesignTokensTests
{
    private const double TextContrastMinimum = 4.5;
    private const double NonTextContrastMinimum = 3.0;

    private static readonly Lazy<string> Stylesheet = new(() => File.ReadAllText(
        Path.Combine(
            TestRepositoryRoot.Resolve(),
            "src",
            "KnowHowToAI.Server",
            "wwwroot",
            "css",
            "app.css")));

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Tokens = new(ParseTokens);

    [Fact]
    public void StyleSheetDefinesAllDesignTokensWithTheAgreedStartValues()
    {
        var expected = new Dictionary<string, string>
        {
            ["--ktai-color-primary"] = "#2563eb",
            ["--ktai-color-primary-hover"] = "#1d4ed8",
            ["--ktai-color-primary-active"] = "#1e40af",
            ["--ktai-color-primary-contrast"] = "#ffffff",
            ["--ktai-color-text"] = "#111827",
            ["--ktai-color-text-secondary"] = "#4b5563",
            ["--ktai-color-text-muted"] = "#6b7280",
            ["--ktai-color-page"] = "#f8fafc",
            ["--ktai-color-surface"] = "#ffffff",
            ["--ktai-color-surface-subtle"] = "#f9fafb",
            ["--ktai-color-border"] = "#cbd5e1",
            ["--ktai-color-border-light"] = "#e5e7eb",
            ["--ktai-color-border-subtle"] = "#f3f4f6",
            ["--ktai-color-border-input"] = "#d1d5db",
            ["--ktai-color-border-hover"] = "#9ca3af",
            ["--ktai-color-success"] = "#15803d",
            ["--ktai-color-success-background"] = "#f0fdf4",
            ["--ktai-color-success-emphasis"] = "#065f46",
            ["--ktai-color-success-emphasis-background"] = "#d1fae5",
            ["--ktai-color-success-strong"] = "#166534",
            ["--ktai-color-success-strong-background"] = "#dcfce7",
            ["--ktai-color-warning"] = "#b45309",
            ["--ktai-color-warning-background"] = "#fffbeb",
            ["--ktai-color-warning-emphasis"] = "#92400e",
            ["--ktai-color-warning-emphasis-background"] = "#fef3c7",
            ["--ktai-color-danger"] = "#b91c1c",
            ["--ktai-color-danger-background"] = "#fef2f2",
            ["--ktai-color-danger-strong-background"] = "#fee2e2",
            ["--ktai-color-info"] = "#2563eb",
            ["--ktai-color-info-background"] = "#eff6ff",
            ["--ktai-color-info-emphasis-background"] = "#dbeafe",
            ["--ktai-color-focus"] = "#2563eb",
            ["--ktai-color-code-background"] = "#f1f5f9",
            ["--ktai-color-code-block-background"] = "#0f172a",
            ["--ktai-color-code-block-text"] = "#e2e8f0",
            ["--ktai-space-1"] = "4px",
            ["--ktai-space-2"] = "8px",
            ["--ktai-space-3"] = "12px",
            ["--ktai-space-4"] = "16px",
            ["--ktai-space-6"] = "24px",
            ["--ktai-space-8"] = "32px",
            ["--ktai-radius-small"] = "4px",
            ["--ktai-radius-medium"] = "8px",
            ["--ktai-shadow-surface"] = "0 1px 2px rgb(15 23 42 / 0.08)",
            ["--ktai-font-family"] = "\"Segoe UI\", Arial, sans-serif",
            ["--ktai-font-size-base"] = "16px",
            ["--ktai-line-height-base"] = "1.5",
            ["--ktai-focus-ring-width"] = "3px",
            ["--ktai-focus-ring-offset"] = "2px"
        };

        foreach (var (token, expectedValue) in expected)
        {
            Assert.True(
                Tokens.Value.TryGetValue(token, out var actualValue),
                $"Der Token {token} fehlt in wwwroot/css/app.css.");
            Assert.Equal(expectedValue, actualValue);
        }
    }

    [Theory]
    [InlineData("--ktai-color-text", "--ktai-color-surface", TextContrastMinimum, "Text auf Surface")]
    [InlineData("--ktai-color-text", "--ktai-color-page", TextContrastMinimum, "Text auf Page")]
    [InlineData("--ktai-color-text-secondary", "--ktai-color-surface", TextContrastMinimum, "Sekundärtext auf Surface")]
    [InlineData("--ktai-color-text-secondary", "--ktai-color-page", TextContrastMinimum, "Sekundärtext auf Page")]
    [InlineData("--ktai-color-primary-contrast", "--ktai-color-primary", TextContrastMinimum, "Buttonbeschriftung auf Primary")]
    [InlineData("--ktai-color-primary-contrast", "--ktai-color-primary-hover", TextContrastMinimum, "Buttonbeschriftung auf Primary Hover")]
    [InlineData("--ktai-color-primary-contrast", "--ktai-color-primary-active", TextContrastMinimum, "Buttonbeschriftung auf Primary Active")]
    [InlineData("--ktai-color-primary", "--ktai-color-surface", TextContrastMinimum, "Primary als Textlink auf Surface")]
    [InlineData("--ktai-color-primary", "--ktai-color-page", TextContrastMinimum, "Primary als Textlink auf Page")]
    [InlineData("--ktai-color-success", "--ktai-color-success-background", TextContrastMinimum, "Erfolgsstatus")]
    [InlineData("--ktai-color-warning", "--ktai-color-warning-background", TextContrastMinimum, "Warnungsstatus")]
    [InlineData("--ktai-color-danger", "--ktai-color-danger-background", TextContrastMinimum, "Fehlerstatus")]
    [InlineData("--ktai-color-info", "--ktai-color-info-background", TextContrastMinimum, "Info-/Aktivstatus")]
    [InlineData("--ktai-color-focus", "--ktai-color-surface", NonTextContrastMinimum, "Fokusring auf Surface")]
    [InlineData("--ktai-color-focus", "--ktai-color-page", NonTextContrastMinimum, "Fokusring auf Page")]
    public void ColorPairsMeetTheComputedWcagContrastThresholds(
        string foregroundToken,
        string backgroundToken,
        double minimumRatio,
        string description)
    {
        var ratio = ContrastRatio(Tokens.Value[foregroundToken], Tokens.Value[backgroundToken]);

        Assert.True(
            ratio >= minimumRatio,
            $"{description}: {foregroundToken} auf {backgroundToken} erreicht nur {ratio.ToString("F2", CultureInfo.InvariantCulture)}:1 statt mindestens {minimumRatio.ToString("F1", CultureInfo.InvariantCulture)}:1.");
    }

    [Fact]
    public void FocusRingIsDefinedAndNeverRemoved()
    {
        var css = Stylesheet.Value;

        Assert.Contains(":focus-visible", css, StringComparison.Ordinal);
        Assert.Contains("outline: var(--ktai-focus-ring-width) solid var(--ktai-color-focus)", css, StringComparison.Ordinal);
        Assert.DoesNotContain("outline: none", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("outline:none", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalStylesheetsDoNotContainHexColorLiterals()
    {
        var serverDirectory = Path.Combine(
            TestRepositoryRoot.Resolve(),
            "src",
            "KnowHowToAI.Server");
        var centralStylesheet = Path.GetFullPath(Path.Combine(
            serverDirectory,
            "wwwroot",
            "css",
            "app.css"));

        var localStylesheets = Directory.EnumerateFiles(serverDirectory, "*.css", SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifact(path))
            .Where(path => !string.Equals(
                Path.GetFullPath(path),
                centralStylesheet,
                StringComparison.OrdinalIgnoreCase));

        foreach (var stylesheet in localStylesheets)
        {
            var content = File.ReadAllText(stylesheet);
            var scatteredColorLiterals = Regex.Matches(
                content,
                @"#[0-9a-f]{3,4}\b|#[0-9a-f]{6}\b|#[0-9a-f]{8}\b",
                RegexOptions.IgnoreCase);

            Assert.True(
                scatteredColorLiterals.Count == 0,
                $"{stylesheet} enthält {scatteredColorLiterals.Count} verstreute Hex-Farbliterale; Farben gehören ausschließlich in die globalen Tokens in wwwroot/css/app.css.");
        }
    }

    private static bool IsBuildArtifact(string path)
    {
        var pathSegments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return pathSegments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            || pathSegments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> ParseTokens()
    {
        var matches = Regex.Matches(
            Stylesheet.Value,
            @"--(ktai-[a-z0-9-]+)\s*:\s*([^;]+);");

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            tokens[$"--{match.Groups[1].Value}"] = match.Groups[2].Value.Trim();
        }

        return tokens;
    }

    private static double ContrastRatio(string foreground, string background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);
        var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        var darker = Math.Min(foregroundLuminance, backgroundLuminance);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(string hexColor)
    {
        var value = hexColor.TrimStart('#');
        Assert.Equal(6, value.Length);

        var red = ChannelLuminance(value[..2]);
        var green = ChannelLuminance(value[2..4]);
        var blue = ChannelLuminance(value[4..6]);

        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
    }

    private static double ChannelLuminance(string hexChannel)
    {
        var channel = int.Parse(hexChannel, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;

        return channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
