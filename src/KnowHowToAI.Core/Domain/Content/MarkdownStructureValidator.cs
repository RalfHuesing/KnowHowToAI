using System.Text.RegularExpressions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace KnowHowToAI.Core.Domain.Content;

/// <summary>
/// Prüft, dass persistierter Content keine eigene Dokumentstruktur erzeugt.
/// </summary>
public static partial class MarkdownStructureValidator
{
    public static ValidationReport Validate(
        string content,
        string nodeTitle,
        bool warnOnPossibleEmbeddedHeading)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(nodeTitle);

        var errors = new List<DomainError>();
        var warnings = new List<DomainWarning>();
        var frontMatterEndPosition = FindFrontMatterEndPosition(content);
        var document = Markdown.Parse(frontMatterEndPosition is null
            ? content
            : MaskFrontMatter(content, frontMatterEndPosition.Value));

        AddFrontMatterError(frontMatterEndPosition, errors);
        AddMarkdownHeadingErrors(content, document, errors);
        AddHtmlHeadingErrors(content, document, errors);

        if (warnOnPossibleEmbeddedHeading)
            AddEmbeddedHeadingWarnings(content, nodeTitle, document, warnings);

        return new ValidationReport(errors, warnings);
    }

    private static void AddFrontMatterError(int? frontMatterEndPosition, ICollection<DomainError> errors)
    {
        if (frontMatterEndPosition is null)
            return;

        errors.Add(CreateError(
            ContentStructureCodes.FrontMatterNotAllowed,
            "Persistiertes System-Front-Matter ist nicht erlaubt.",
            line: 1,
            column: 1,
            kind: "FrontMatter"));
    }

    private static void AddMarkdownHeadingErrors(
        string content,
        MarkdownDocument document,
        ICollection<DomainError> errors)
    {
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            errors.Add(CreateError(
                ContentStructureCodes.HeadingNotAllowed,
                "Markdown-Überschriften sind im gespeicherten Content nicht erlaubt.",
                GetPosition(content, heading.Span.Start).Line,
                GetPosition(content, heading.Span.Start).Column,
                heading.IsSetext ? "SetextHeading" : "AtxHeading"));
        }
    }

    private static void AddHtmlHeadingErrors(string content, MarkdownDocument document, ICollection<DomainError> errors)
    {
        foreach (var htmlInline in document.Descendants<HtmlInline>())
            AddHtmlHeadingErrors(htmlInline.Tag, htmlInline.Span.Start, content, errors);

        foreach (var htmlBlock in document.Descendants<HtmlBlock>())
        {
            var blockContent = ExtractSpanContent(content, htmlBlock.Span.Start, htmlBlock.Span.End);
            AddHtmlHeadingErrors(blockContent, htmlBlock.Span.Start, content, errors);
        }
    }

    private static void AddHtmlHeadingErrors(
        string html,
        int htmlStartPosition,
        string content,
        ICollection<DomainError> errors)
    {
        foreach (Match match in HtmlHeadingTagRegex().Matches(html))
        {
            var (line, column) = GetPosition(content, htmlStartPosition + match.Index);
            errors.Add(CreateError(
                ContentStructureCodes.HeadingNotAllowed,
                "HTML-Überschriften sind im gespeicherten Content nicht erlaubt.",
                line,
                column,
                "HtmlHeading"));
        }
    }

    private static void AddEmbeddedHeadingWarnings(
        string content,
        string nodeTitle,
        MarkdownDocument document,
        ICollection<DomainWarning> warnings)
    {
        foreach (var paragraph in document.Descendants<ParagraphBlock>())
        {
            if (IsStandaloneEmphasis(paragraph, out var emphasisKind))
            {
                warnings.Add(CreateWarning(content, paragraph, emphasisKind));
                continue;
            }

            var paragraphContent = ExtractSpanContent(content, paragraph.Span.Start, paragraph.Span.End).Trim();
            if (string.Equals(paragraphContent, nodeTitle.Trim(), StringComparison.Ordinal))
                warnings.Add(CreateWarning(content, paragraph, "NodeTitle"));
        }
    }

    private static bool IsStandaloneEmphasis(ParagraphBlock paragraph, out string kind)
    {
        if (paragraph.Inline?.FirstChild is EmphasisInline emphasis && emphasis.NextSibling is null)
        {
            kind = emphasis.DelimiterCount >= 2 ? "StrongEmphasis" : "Emphasis";
            return true;
        }

        kind = string.Empty;
        return false;
    }

    private static DomainWarning CreateWarning(string content, MarkdownObject markdownObject, string kind)
    {
        var (line, column) = GetPosition(content, markdownObject.Span.Start);
        return new DomainWarning(
            ContentStructureCodes.PossibleEmbeddedHeading,
            "Der Content enthält einen möglichen eingebetteten Ersatztitel.",
            new Dictionary<string, string>
            {
                [ContentStructureCodes.LineDetail] = line.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [ContentStructureCodes.ColumnDetail] = column.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [ContentStructureCodes.KindDetail] = kind
            });
    }

    private static DomainError CreateError(string code, string message, int line, int column, string kind) =>
        new(
            code,
            message,
            new Dictionary<string, string>
            {
                [ContentStructureCodes.LineDetail] = line.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [ContentStructureCodes.ColumnDetail] = column.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [ContentStructureCodes.KindDetail] = kind
            });

    private static int? FindFrontMatterEndPosition(string content)
    {
        var lines = content.Split('\n');
        if (lines.Length < 3 || !string.Equals(lines[0].Trim(), "---", StringComparison.Ordinal))
            return null;

        var containsMetadata = false;
        var position = lines[0].Length + 1;
        for (var index = 1; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (string.Equals(line, "---", StringComparison.Ordinal) || string.Equals(line, "...", StringComparison.Ordinal))
                return containsMetadata ? position + lines[index].Length - 1 : null;

            containsMetadata |= line.Contains(':', StringComparison.Ordinal);
            position += lines[index].Length + 1;
        }

        return null;
    }

    private static string MaskFrontMatter(string content, int frontMatterEndPosition)
    {
        var maskedContent = content.ToCharArray();
        for (var index = 0; index <= frontMatterEndPosition; index++)
        {
            if (maskedContent[index] is not '\r' and not '\n')
                maskedContent[index] = ' ';
        }

        return new string(maskedContent);
    }

    private static string ExtractSpanContent(string content, int start, int end)
    {
        if (start < 0 || end < start || start >= content.Length)
            return string.Empty;

        var length = Math.Min(end, content.Length - 1) - start + 1;
        return content.Substring(start, length);
    }

    private static (int Line, int Column) GetPosition(string content, int position)
    {
        var boundedPosition = Math.Clamp(position, 0, content.Length);
        var line = 1;
        var column = 1;

        for (var index = 0; index < boundedPosition; index++)
        {
            if (content[index] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    [GeneratedRegex(@"<\s*h[1-6]\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlHeadingTagRegex();
}
