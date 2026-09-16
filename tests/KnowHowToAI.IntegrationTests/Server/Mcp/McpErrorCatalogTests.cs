using System.Text.RegularExpressions;
using KnowHowToAI.Server.Mcp.Contracts;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Synchronisationstest: der im Code angelegte stabile Fehler- und Warncode-Katalog
/// muss exakt dem Katalog in der Dokumentation (docs/McpApi.md) entsprechen.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpErrorCatalogTests
{
    private const string CatalogStartMarker = "<!-- mcp-catalog-start -->";
    private const string CatalogEndMarker = "<!-- mcp-catalog-end -->";
    private const string WarningMarker = "Warncodes wie";

    [Fact]
    public void ErrorAndWarningCodes_MatchTheDocumentedCatalog()
    {
        var (documentedErrorCodes, documentedWarningCodes) = ReadDocumentedCatalog();

        Assert.Equal(
            documentedErrorCodes.OrderBy(code => code, StringComparer.Ordinal),
            McpErrorCatalog.KnownErrorCodes.OrderBy(code => code, StringComparer.Ordinal));
        Assert.Equal(
            documentedWarningCodes.OrderBy(code => code, StringComparer.Ordinal),
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

    private static (IReadOnlyList<string> ErrorCodes, IReadOnlyList<string> WarningCodes) ReadDocumentedCatalog()
    {
        var documentation = File.ReadAllText(FindCatalogPath());
        var sectionStart = documentation.IndexOf(CatalogStartMarker, StringComparison.Ordinal);
        var sectionEnd = documentation.IndexOf(CatalogEndMarker, StringComparison.Ordinal);
        Assert.True(sectionStart >= 0 && sectionEnd > sectionStart, "Doku-Katalogabschnitt nicht gefunden.");

        var section = documentation[sectionStart..sectionEnd];
        var warningIndex = section.IndexOf(WarningMarker, StringComparison.Ordinal);
        Assert.True(warningIndex > 0, "Warncode-Marker im Doku-Katalog nicht gefunden.");

        return (ExtractCodes(section[..warningIndex]), ExtractCodes(section[warningIndex..]));
    }

    private static IReadOnlyList<string> ExtractCodes(string text) =>
        [.. Regex.Matches(text, "`([A-Za-z][A-Za-z0-9]+)`").Select(match => match.Groups[1].Value).Distinct()];

    private static string FindCatalogPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
               && !File.Exists(Path.Combine(directory.FullName, "docs", "McpApi.md")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "docs", "McpApi.md");
    }
}
