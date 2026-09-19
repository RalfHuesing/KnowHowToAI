using System.Net;
using KnowHowToAI.BrowserTests.TestSupport;
using Microsoft.Playwright;

namespace KnowHowToAI.BrowserTests.ReadOnly;

[Collection("Smoke-Host")]
[Trait("Category", "Integration")]
public sealed class HistorySmokeTests
{
    private readonly PublishedServerHost _host;

    public HistorySmokeTests(SmokeHostFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task History_TransfersContextsAndRendersPagedStructuredDiff()
    {
        await using var browser = await ChromeBrowser.LaunchAsync();
        var page = await browser.NewPageAsync();

        var response = await page.GotoAsync($"{_host.Address}/history", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.OK, response.Status);
        await CircuitProbe.WaitForInteractivityAsync(page);

        await Assertions.Expect(page.GetByTestId("history-page")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("history-working-separation")).ToContainTextAsync("Working Transactions gehören nicht");
        await Assertions.Expect(page.GetByTestId("snapshot-list")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("release-list")).ToContainTextAsync("Browser History Release");

        var baseSnapshot = await SelectSnapshotActionByPurposeAsync(page, "Browser-History-Basis", "snapshot-diff-base-");
        var targetSnapshot = await SelectSnapshotActionByPurposeAsync(page, "Browser-History-Diff", "snapshot-diff-target-");
        Assert.True(baseSnapshot.PageCount > 1, "Der Browserbestand muss eine zweite Snapshot-Seite erzwingen.");
        Assert.NotEqual(baseSnapshot.SnapshotId, targetSnapshot.SnapshotId);

        var diffList = page.GetByTestId("snapshot-diff-list");
        await Assertions.Expect(diffList).ToContainTextAsync("Rolle");
        await Assertions.Expect(diffList).ToContainTextAsync("Rollenauflösung");
        await Assertions.Expect(diffList).ToContainTextAsync("Knoten");
        await Assertions.Expect(page.GetByTestId("snapshot-diff-next")).ToBeVisibleAsync();

        await page.GetByTestId("snapshot-diff-next").ClickAsync();
        await Assertions.Expect(diffList).ToContainTextAsync("Inhalt");
        await Assertions.Expect(diffList).ToContainTextAsync("Abhängigkeit");

        var nodeEntry = page.Locator("[data-testid^='snapshot-diff-entry-Node-']").First;
        var nodeTestId = await nodeEntry.GetAttributeAsync("data-testid");
        var nodeId = nodeTestId?["snapshot-diff-entry-Node-".Length..]
            ?? throw new InvalidOperationException("Der Browser-Diff enthält keine filterbare Node-ID.");

        await page.GotoAsync($"{_host.Address}/history?baseSnapshotId={baseSnapshot.SnapshotId}&targetSnapshotId={targetSnapshot.SnapshotId}&nodeId={nodeId}");
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("snapshot-diff-summary")).ToContainTextAsync("gefiltert auf Knoten");
        Assert.Empty(await page.Locator("[data-testid^='snapshot-diff-entry-Role-'], [data-testid^='snapshot-diff-entry-RoleResolution-']").AllAsync());

        await page.GotoAsync($"{_host.Address}/history?baseSnapshotId={baseSnapshot.SnapshotId}&targetSnapshotId={targetSnapshot.SnapshotId}&nodeId=00000000-0000-0000-0000-000000000099");
        await CircuitProbe.WaitForInteractivityAsync(page);
        await Assertions.Expect(page.GetByTestId("snapshot-diff-empty")).ToBeVisibleAsync();

        await page.GotoAsync($"{_host.Address}/history");
        await CircuitProbe.WaitForInteractivityAsync(page);
        var contextSnapshot = await SelectSnapshotActionByPurposeAsync(page, "Browser-History-Basis", "snapshot-select-");
        await page.WaitForURLAsync("**/knowledge?*");
        Assert.Contains($"snapshotId={contextSnapshot.SnapshotId}", page.Url, StringComparison.Ordinal);

        await page.GotoAsync($"{_host.Address}/history");
        await CircuitProbe.WaitForInteractivityAsync(page);
        await page.Locator("li:has-text('Browser History Release') [data-testid^='release-select-']").ClickAsync();
        await page.WaitForURLAsync("**/knowledge?*");
        Assert.Contains("releaseId=", page.Url, StringComparison.Ordinal);
    }

    private static async Task<SnapshotSelection> SelectSnapshotActionByPurposeAsync(
        IPage page,
        string purpose,
        string actionPrefix)
    {
        for (var pageCount = 1; pageCount <= 25; pageCount++)
        {
            var item = page.Locator($"li:has-text('{purpose}')");
            if (await item.CountAsync() > 0)
            {
                var action = item.Locator($"[data-testid^='{actionPrefix}']");
                var testId = await action.GetAttributeAsync("data-testid");
                var snapshotId = long.TryParse(testId?[actionPrefix.Length..], out var parsed)
                    ? parsed
                    : throw new InvalidOperationException("Die Snapshot-Aktion enthält keine gültige Snapshot-ID.");
                await action.ClickAsync();
                return new SnapshotSelection(snapshotId, pageCount);
            }

            var next = page.GetByTestId("snapshot-list-next");
            if (await next.CountAsync() == 0)
                break;

            var previousItems = await page.GetByTestId("snapshot-list").TextContentAsync();
            await next.ClickAsync();
            await page.WaitForFunctionAsync(
                "previousItems => document.querySelector('[data-testid=\"snapshot-list\"]')?.textContent !== previousItems",
                previousItems);
        }

        throw new InvalidOperationException($"Der Browserbestand enthält keinen Snapshot mit Purpose '{purpose}'.");
    }

    private sealed record SnapshotSelection(long SnapshotId, int PageCount);
}
