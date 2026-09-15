using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Content;

/// <summary>
/// Ersetzt genau einen ordinalen Texttreffer und prüft das daraus entstehende Content-Dokument.
/// </summary>
public static class TextReplacer
{
    public static Result<string> Replace(TextReplacementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedContent = ContentNormalizer.Normalize(request.ExistingContent);
        var normalizedOldText = ContentNormalizer.Normalize(request.OldText);
        var normalizedNewText = ContentNormalizer.Normalize(request.NewText);
        var matchCount = CountOrdinalMatches(normalizedContent, normalizedOldText);
        if (matchCount == 0)
            return Result<string>.Failure(CreateTextNotFoundError());

        if (matchCount > 1)
            return Result<string>.Failure(CreateMultipleTextMatchesError(matchCount));

        var replacementStart = normalizedContent.IndexOf(normalizedOldText, StringComparison.Ordinal);
        var replacedContent = ContentNormalizer.Normalize(
            string.Concat(
                normalizedContent.AsSpan(0, replacementStart),
                normalizedNewText,
                normalizedContent.AsSpan(replacementStart + normalizedOldText.Length)));
        var validation = MarkdownStructureValidator.Validate(
            replacedContent,
            request.NodeTitle,
            request.WarnOnPossibleEmbeddedHeading);

        return validation.IsValid
            ? Result<string>.Success(replacedContent, validation.Warnings)
            : Result<string>.Failure(validation.Errors[0], validation.Warnings);
    }

    private static int CountOrdinalMatches(string content, string searchText)
    {
        if (searchText.Length == 0)
            return content.Length + 1;

        var matchCount = 0;
        var searchStart = 0;
        while (searchStart <= content.Length - searchText.Length)
        {
            var matchStart = content.IndexOf(searchText, searchStart, StringComparison.Ordinal);
            if (matchStart < 0)
                break;

            matchCount++;
            searchStart = matchStart + 1;
        }

        return matchCount;
    }

    private static DomainError CreateTextNotFoundError() =>
        new(
            TextOperationCodes.TextNotFound,
            "Der zu ersetzende Text kommt im expliziten Content nicht vor.");

    private static DomainError CreateMultipleTextMatchesError(int matchCount) =>
        new(
            TextOperationCodes.MultipleTextMatches,
            "Der zu ersetzende Text muss im expliziten Content genau einmal vorkommen.",
            new Dictionary<string, string>
            {
                [TextOperationCodes.MatchCountDetail] = matchCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
}
