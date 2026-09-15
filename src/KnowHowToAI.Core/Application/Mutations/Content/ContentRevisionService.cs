using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Bindet die fachliche Revisionsentscheidung an die kontrollierbare ID-Erzeugung.
/// </summary>
public sealed class ContentRevisionService(IIdentifierGenerator identifierGenerator)
{
    private readonly IIdentifierGenerator _identifierGenerator = identifierGenerator ?? throw new ArgumentNullException(nameof(identifierGenerator));

    public ContentRevisionAssignment Assign(NodeContent? existingContent, string submittedContent)
    {
        var decision = ContentRevisionPlanner.Decide(existingContent, submittedContent);
        var revisionId = decision.RequiresNewRevision
            ? _identifierGenerator.CreateContentRevisionId()
            : existingContent!.ContentRevisionId;

        return new ContentRevisionAssignment(
            decision.NormalizedContentMd,
            revisionId,
            decision.RequiresNewRevision);
    }
}
