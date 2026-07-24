namespace KnowHowToAI.Core.Documents;

public sealed record Document(
    string Slug,
    string Title,
    string Content,
    string? ParentSlug,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Synonyms);
