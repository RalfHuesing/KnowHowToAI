using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.TestSupport;

internal static class BrowserTransactionReader
{
    public static async Task<Guid> ReadTransactionIdAsync(IPage page)
    {
        var currentUri = new Uri(page.Url);
        var routeSegments = currentUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (routeSegments.Length == 2
            && string.Equals(routeSegments[0], "drafts", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(routeSegments[1], out var routedTransactionId))
        {
            return routedTransactionId;
        }

        var rawValue = await page.GetByTestId("tx-id").TextContentAsync();
        return Guid.TryParse(rawValue?.Replace("ID:", string.Empty, StringComparison.Ordinal).Trim(), out var transactionId)
            ? transactionId
            : throw new InvalidOperationException("Die Transaction-Detailseite enthält keine gültige Transaction-ID.");
    }
}
