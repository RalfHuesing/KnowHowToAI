using System.Text.RegularExpressions;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Extrahiert kompakte Snippets um einen Suchbegriff mit Maximallänge und Einzeilen-Normalisierung.
/// </summary>
public static class SnippetExtractor
{
    private const string Ellipsis = "...";
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Extrahiert einen formatierten Textausschnitt der maximalen Länge <paramref name="maxChars"/>.
    /// </summary>
    public static string ExtractSnippet(string? text, string? searchText, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text) || maxChars <= 0)
            return string.Empty;

        var normalized = WhitespaceRegex.Replace(text, " ").Trim();
        if (normalized.Length <= maxChars)
            return normalized;

        if (string.IsNullOrWhiteSpace(searchText))
            return TruncateEnd(normalized, maxChars);

        var matchIndex = normalized.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
            return TruncateEnd(normalized, maxChars);

        var matchLength = searchText.Length;
        if (matchLength >= maxChars)
            return normalized.Substring(matchIndex, maxChars);

        return BuildCenteredSnippet(normalized, matchIndex, matchLength, maxChars);
    }

    private static string BuildCenteredSnippet(string text, int matchIndex, int matchLength, int maxChars)
    {
        var (start, end) = CalculateInitialWindow(text.Length, matchIndex, matchLength, maxChars);
        var hasPrefix = start > 0;
        var hasSuffix = end < text.Length;

        var availableChars = maxChars - (hasPrefix ? Ellipsis.Length : 0) - (hasSuffix ? Ellipsis.Length : 0);
        if (end - start > availableChars)
        {
            (start, end) = ShrinkWindowToFit(start, end, matchIndex, matchLength, availableChars);
        }

        var snippetText = text[start..end].Trim();
        var result = (hasPrefix ? Ellipsis : string.Empty) + snippetText + (hasSuffix ? Ellipsis : string.Empty);
        return result.Length > maxChars ? result[..maxChars] : result;
    }

    private static (int Start, int End) CalculateInitialWindow(int textLength, int matchIndex, int matchLength, int maxChars)
    {
        var contextChars = maxChars - matchLength;
        var leadChars = contextChars / 2;
        var start = Math.Max(0, matchIndex - leadChars);
        var end = Math.Min(textLength, start + maxChars);

        if (end - start < maxChars && start > 0)
        {
            start = Math.Max(0, end - maxChars);
        }

        return (start, end);
    }

    private static (int Start, int End) ShrinkWindowToFit(int start, int end, int matchIndex, int matchLength, int targetLength)
    {
        var curStart = start;
        var curEnd = end;

        while (curEnd - curStart > targetLength && curEnd - curStart > matchLength)
        {
            if (curStart < matchIndex)
            {
                curStart++;
            }
            else if (curEnd > matchIndex + matchLength)
            {
                curEnd--;
            }
            else
            {
                break;
            }
        }

        return (curStart, curEnd);
    }

    private static string TruncateEnd(string text, int maxChars)
    {
        if (text.Length <= maxChars)
            return text;

        if (maxChars <= Ellipsis.Length)
            return text[..maxChars];

        return text[..(maxChars - Ellipsis.Length)].TrimEnd() + Ellipsis;
    }
}
