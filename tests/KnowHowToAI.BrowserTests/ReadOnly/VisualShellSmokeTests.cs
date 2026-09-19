using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using KnowHowToAI.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

/// <summary>
/// Visuelle Shell-Smokes: nach semantischen Verhaltensassertionen wird die
/// tatsächlich erreichbare Shell (1280 × 720) beziehungsweise die kompakte
/// Shell (1024 × 720) im Light Theme als Screenshot aufgenommen und gegen die
/// versionierte Baseline unter <c>TestSupport/Baselines</c> verglichen. Vor
/// der Aufnahme werden Animationen, Übergänge, Caret und weitere flüchtige
/// Inhalte stabil maskiert; die Aufnahme erfolgt erst nach dem Circuit-Ready-
/// Zustand, damit kein Ladezustand einfriert. Baselines werden niemals im
/// regulären Lauf automatisch aktualisiert; eine Abweichung erfordert eine
/// bewusste Diff-Prüfung mit anschließendem manuellen Übernehmen.
/// </summary>
[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class VisualShellSmokeTests
{
    private readonly PublishedServerHost _host;

    public VisualShellSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    public static TheoryData<int, int, string> ShellViewports => new()
    {
        { 1280, 720, "Shell-1280x720-light.png" },
        { 1024, 720, "Shell-1024x720-light.png" }
    };

    [Theory]
    [MemberData(nameof(ShellViewports))]
    public async Task ShellMatchesTheVersionedLightThemeBaseline(int width, int height, string baselineFileName)
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ReducedMotion = ReducedMotion.Reduce
        });

        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        // Verhaltensassertionen vor der visuellen Aufnahme: Zuerst die
        // Circuit-Interaktivität belegen, denn beim Verbinden setzt die Seite
        // ihren Status zurück und rendert „Shell wird initialisiert.“ erneut;
        // erst danach ist der Shell-Status stabil.
        await CircuitProbe.WaitForInteractivityAsync(page);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "KnowHowToAI" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("shell-status")).ToContainTextAsync("Shell bereit");
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToHaveCountAsync(1);

        var navigation = page.GetByRole(AriaRole.Navigation, new() { Name = "Hauptnavigation" });
        var navigationToggle = page.GetByRole(AriaRole.Button, new() { Name = "Navigation einblenden", Exact = true });
        if (width >= 1280)
        {
            await Assertions.Expect(navigation).ToBeVisibleAsync();
            await Assertions.Expect(navigationToggle).ToHaveCountAsync(0);
        }
        else
        {
            // Der Schalter erscheint erst mit verbundenem Circuit in der kompakten
            // Breite; das Warten auf Sichtbarkeit belegt beide Bedingungen ohne Wartezeit.
            await Assertions.Expect(navigationToggle).ToBeVisibleAsync(new() { Timeout = 30_000 });
            await Assertions.Expect(navigation).ToHaveCountAsync(0);
        }

        await MaskVolatileContentAsync(page);
        var actualPath = await CaptureActualScreenshotAsync(page, baselineFileName);
        AssertBaselineMatches(baselineFileName, actualPath);
    }

    private static async Task MaskVolatileContentAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
                const mask = document.createElement('style');
                mask.id = 'visual-smoke-mask';
                mask.textContent = '*, *::before, *::after { animation-duration: 0s !important; animation-delay: 0s !important; transition: none !important; caret-color: transparent !important; scroll-behavior: auto !important; }';
                document.head.appendChild(mask);
            }
            """);
        // Schriftladezustände abwarten, damit keine Font-Substitution eingefroren wird.
        await page.EvaluateAsync("() => document.fonts.ready");
    }

    private static async Task<string> CaptureActualScreenshotAsync(IPage page, string baselineFileName)
    {
        var actualPath = Path.Combine(
            TestRepositoryRoot.Resolve(),
            "temp",
            "visual-shell",
            $"{Path.GetFileNameWithoutExtension(baselineFileName)}.actual.png");
        Directory.CreateDirectory(Path.GetDirectoryName(actualPath)!);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = actualPath });
        return actualPath;
    }

    private static void AssertBaselineMatches(string baselineFileName, string actualPath)
    {
        var baselinePath = Path.Combine(
            TestRepositoryRoot.Resolve(),
            "tests",
            "KnowHowToAI.BrowserTests",
            "TestSupport",
            "Baselines",
            baselineFileName);

        if (!File.Exists(baselinePath))
        {
            Assert.Fail(
                $"Die visuelle Baseline '{baselineFileName}' fehlt unter '{Path.GetDirectoryName(baselinePath)}'. " +
                $"Nach einer bewussten Diff-Prüfung des aktuellen Screenshots unter '{actualPath}' kann er manuell als Baseline übernommen werden; eine automatische Aktualisierung im regulären Lauf ist verboten.");
        }

        var baselineBytes = File.ReadAllBytes(baselinePath);
        var actualBytes = File.ReadAllBytes(actualPath);
        if (!baselineBytes.AsSpan().SequenceEqual(actualBytes))
        {
            Assert.Fail(
                $"Der Shell-Screenshot '{actualPath}' weicht von der versionierten Baseline '{baselinePath}' ab. " +
                "Die Abweichung ist per Diff-Prüfung zu bewerten; eine beabsichtigte Änderung wird manuell in die Baseline übernommen, eine automatische Aktualisierung im regulären Lauf ist verboten.");
        }
    }
}
