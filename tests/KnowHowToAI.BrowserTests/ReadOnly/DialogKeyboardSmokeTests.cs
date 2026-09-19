using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class DialogKeyboardSmokeTests
{
    private readonly PublishedServerHost _host;

    public DialogKeyboardSmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task DialogKeyboardSequenceTrapsFocusAndReturnsItOnEscape()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();
        var observedRequests = new List<string>();
        page.Request += (_, request) => observedRequests.Add(request.Url);

        var response = await page.GotoAsync(_host.Address, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "KnowHowToAI" })).ToBeVisibleAsync();

        // Die Dialogprüfung ersetzt die Shell durch eine Prüfseite, die
        // ausschließlich Ressourcen des real gestarteten Servers lädt
        // (app.css und AppDialog.razor.js); es gibt keine Demo-Route im Produkt.
        await page.SetContentAsync(BuildHarnessHtml(_host.Address));
        await page.WaitForFunctionAsync("() => window.__dialogHarnessReady === true");

        var opener = page.Locator("#dialog-opener");
        var dialog = page.Locator("#dialog-under-test");
        var firstAction = page.Locator("#dialog-first");
        var lastAction = page.Locator("#dialog-last");

        await opener.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(firstAction).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(lastAction).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");
        await Assertions.Expect(firstAction).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(firstAction).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(dialog).Not.ToBeVisibleAsync();
        await Assertions.Expect(opener).ToBeFocusedAsync();

        Assert.NotEmpty(observedRequests);
        Assert.All(observedRequests, request => Assert.StartsWith(_host.Address, request, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildHarnessHtml(string hostAddress) => $$"""
        <!DOCTYPE html>
        <html lang="de">
        <head>
            <meta charset="utf-8" />
            <title>Dialog-Tastaturprüfung</title>
            <link rel="stylesheet" href="{{hostAddress}}/css/app.css" />
        </head>
        <body>
            <button id="dialog-opener" type="button">Dialog öffnen</button>
            <dialog id="dialog-under-test" aria-labelledby="dialog-title">
                <h2 id="dialog-title">Beispieldialog</h2>
                <button id="dialog-first" type="button">Erste Aktion</button>
                <button id="dialog-last" type="button">Dialog schließen</button>
            </dialog>
            <script type="module">
                import { initialize, show } from "{{hostAddress}}/Web/Components/Shared/Dialogs/AppDialog.razor.js";
                const dialog = document.getElementById("dialog-under-test");
                initialize(dialog, { invokeMethodAsync: () => Promise.resolve() });
                document.getElementById("dialog-opener").addEventListener("click", () => show(dialog));
                window.__dialogHarnessReady = true;
            </script>
        </body>
        </html>
        """;
}
