using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Wartet auf den vom Shell-Lifecycle gemeldeten interaktiven Circuitzustand.
/// </summary>
public static class CircuitProbe
{
    public static async Task WaitForInteractivityAsync(IPage page)
    {
        await Assertions.Expect(page.GetByTestId("shell-root")).ToHaveAttributeAsync(
            "data-ktai-interactive",
            "true",
            new() { Timeout = 30_000 });
    }
}
