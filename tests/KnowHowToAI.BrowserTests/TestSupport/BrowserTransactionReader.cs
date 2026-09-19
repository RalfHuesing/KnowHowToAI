using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.TestSupport;

internal static class BrowserTransactionReader
{
    public static async Task<Guid> ReadTransactionIdAsync(IPage page)
    {
        var rawValue = await page.GetByTestId("tx-id").TextContentAsync();
        return Guid.TryParse(rawValue?.Replace("ID:", string.Empty, StringComparison.Ordinal).Trim(), out var transactionId)
            ? transactionId
            : throw new InvalidOperationException("Die Transaction-Detailseite enthält keine gültige Transaction-ID.");
    }
}
