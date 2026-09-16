using System.Text.RegularExpressions;
using KnowHowToAI.Server.Mcp.Contracts;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Synchronisationstest: der im Code angelegte stabile Fehler- und Warncode-Katalog
/// muss exakt dem verbindlichen Katalog in docs/Roadmap.md entsprechen.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpErrorCatalogTests
{
    private const string SectionStartMarker = "## Stabiler Fehlercode-Katalog";
    private const string SectionEndMarker = "## Bewusst außerhalb von V1";
    private const string WarningMarker = "Warncodes wie";

    [Fact]
    public void ErrorAndWarningCodes_MatchTheBindingRoadmapCatalog()
    {
        var (roadmapErrorCodes, roadmapWarningCodes) = ReadRoadmapCatalog();

        Assert.Equal(
            roadmapErrorCodes.OrderBy(code => code, StringComparer.Ordinal),
            McpErrorCatalog.KnownErrorCodes.OrderBy(code => code, StringComparer.Ordinal));
        Assert.Equal(
            roadmapWarningCodes.OrderBy(code => code, StringComparer.Ordinal),
            McpErrorCatalog.KnownWarningCodes.OrderBy(code => code, StringComparer.Ordinal));
    }

    [Fact]
    public void Catalog_ContainsNoDuplicateCodes()
    {
        Assert.Equal(
            McpErrorCatalog.KnownErrorCodes.Count,
            McpErrorCatalog.KnownErrorCodes.Distinct().Count());
        Assert.Equal(
            McpErrorCatalog.KnownWarningCodes.Count,
            McpErrorCatalog.KnownWarningCodes.Distinct().Count());
    }

    private static (IReadOnlyList<string> ErrorCodes, IReadOnlyList<string> WarningCodes) ReadRoadmapCatalog()
    {
        var roadmap = File.ReadAllText(FindRoadmapPath());
        var sectionStart = roadmap.IndexOf(SectionStartMarker, StringComparison.Ordinal);
        var sectionEnd = roadmap.IndexOf(SectionEndMarker, StringComparison.Ordinal);
        Assert.True(sectionStart >= 0 && sectionEnd > sectionStart, "Roadmap-Katalogabschnitt nicht gefunden.");

        var section = roadmap[sectionStart..sectionEnd];
        var warningIndex = section.IndexOf(WarningMarker, StringComparison.Ordinal);
        Assert.True(warningIndex > 0, "Warncode-Marker im Roadmap-Katalog nicht gefunden.");

        return (ExtractCodes(section[..warningIndex]), ExtractCodes(section[warningIndex..]));
    }

    private static IReadOnlyList<string> ExtractCodes(string text) =>
        [.. Regex.Matches(text, "`([A-Za-z][A-Za-z0-9]+)`").Select(match => match.Groups[1].Value).Distinct()];

    private static string FindRoadmapPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
               && !File.Exists(Path.Combine(directory.FullName, "docs", "Roadmap.md")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "docs", "Roadmap.md");
    }
}
