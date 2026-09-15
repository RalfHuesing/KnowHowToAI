namespace KnowHowToAI.Core.Domain.Content;

/// <summary>
/// Entscheidet ausschließlich anhand des normalisierten Texts, ob eine neue Revision benötigt wird.
/// </summary>
public static class ContentRevisionPlanner
{
    public static ContentRevisionDecision Decide(NodeContent? existingContent, string submittedContent)
    {
        var normalizedContent = ContentNormalizer.Normalize(submittedContent);
        var requiresNewRevision = existingContent is null
            || existingContent.IsDeleted
            || !StringComparer.Ordinal.Equals(existingContent.ContentMd, normalizedContent);

        return new ContentRevisionDecision(normalizedContent, requiresNewRevision);
    }
}
