namespace KnowHowToAI.Core.Documents;

public sealed class Document
{
    public required string Slug { get; init; }
    public string? ParentSlug { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<string> Synonyms { get; init; } = [];
}
