using System.Text.RegularExpressions;

namespace KnowHowToAI.Core.Documents;

// Regeln und Begründung: docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt "Slug-Regeln".
public static partial class SlugRules
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SegmentPattern();

    public static bool IsValidSegment(string segment) => SegmentPattern().IsMatch(segment);

    public static bool IsValidSlug(string slug) =>
        slug.Length > 0 && slug.Split('/').All(IsValidSegment);

    public static string? GetParentSlug(string slug)
    {
        var lastSeparator = slug.LastIndexOf('/');
        return lastSeparator < 0 ? null : slug[..lastSeparator];
    }

    public static string FromFilePath(string docsRootPath, string filePath)
    {
        var relative = Path.GetRelativePath(docsRootPath, filePath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
        return relative[..^Path.GetExtension(relative).Length];
    }
}
