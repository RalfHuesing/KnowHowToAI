using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Wartet auf die Interaktivität des Blazor-Circuits über die Testschaltfläche der Shell.
/// </summary>
public static class CircuitProbe
{
    public static async Task WaitForInteractivityAsync(IPage page)
    {
        // Der Klick kann ankommen, bevor der Circuit das Ereignis verdrahtet
        // hat (Warmup nach dem Serverstart). Deshalb klicken wir erneut, bis
        // der beobachtbare Statuswechsel die Interaktivität belegt.
        var interactionStatus = page.GetByTestId("interaction-status");
        for (var attempt = 1; ; attempt++)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Interaktivität prüfen" }).ClickAsync();
            try
            {
                await Assertions.Expect(interactionStatus).ToHaveTextAsync(
                    "Interaktivität ist verfügbar.",
                    new() { Timeout = 2_000 });
                break;
            }
            catch (PlaywrightException) when (attempt < 10)
            {
                // Circuit noch nicht verbunden; erneut klicken.
            }
        }
    }
}
