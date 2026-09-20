using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Startet Google Chrome Stable headless über Playwright und verwaltet den
/// Lifecycle von Browser und Playwright-Instanz.
/// </summary>
public sealed class ChromeBrowser : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;

    private ChromeBrowser(IPlaywright playwright, IBrowser browser)
    {
        _playwright = playwright;
        _browser = browser;
    }

    public static async Task<ChromeBrowser> LaunchAsync(BrowserTypeLaunchOptions? options = null)
    {
        var launchOptions = options ?? new BrowserTypeLaunchOptions();
        launchOptions.Channel = "chrome";
        launchOptions.Headless = true;

        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(launchOptions);
            return new ChromeBrowser(playwright, browser);
        }
        catch
        {
            playwright.Dispose();
            throw;
        }
    }

    public Task<IPage> NewPageAsync(BrowserNewPageOptions? options = null) =>
        _browser.NewPageAsync(options);

    public string Version => _browser.Version;

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}
