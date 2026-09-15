using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Wendet Content-Mutationen ausschließlich auf explizite Rollen-Contents an.
/// </summary>
public sealed class ContentMutationService(ContentRevisionService revisionService)
{
    private readonly ContentRevisionService _revisionService = revisionService ?? throw new ArgumentNullException(nameof(revisionService));

    public Result<ContentMutationResult> ReplaceText(
        IEnumerable<NodeContent> existingContents,
        ReplaceTextCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingContents);
        ArgumentNullException.ThrowIfNull(command);

        var contents = existingContents.ToArray();
        var explicitContentResult = FindSingleActiveExplicitContent(contents, command.NodeId, command.RoleId);
        if (!explicitContentResult.IsSuccess)
            return Result<ContentMutationResult>.Failure(explicitContentResult.Error!);

        var explicitContent = explicitContentResult.Value!;
        var replacementResult = TextReplacer.Replace(new TextReplacementRequest(
            explicitContent.ContentMd,
            command.OldText,
            command.NewText,
            command.NodeTitle,
            command.WarnOnPossibleEmbeddedHeading));
        if (!replacementResult.IsSuccess)
            return Result<ContentMutationResult>.Failure(replacementResult.Error!, replacementResult.Warnings);

        var assignment = _revisionService.Assign(explicitContent, replacementResult.Value!);
        var changedContent = explicitContent with
        {
            ContentMd = assignment.NormalizedContentMd,
            ContentRevisionId = assignment.ContentRevisionId
        };

        return Result<ContentMutationResult>.Success(
            new ContentMutationResult(changedContent, Replace(contents, changedContent)),
            replacementResult.Warnings);
    }

    public Result<ContentMutationResult> DeleteContent(
        IEnumerable<NodeContent> existingContents,
        DeleteContentCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingContents);
        ArgumentNullException.ThrowIfNull(command);

        var contents = existingContents.ToArray();
        var explicitContentResult = FindSingleActiveExplicitContent(contents, command.NodeId, command.RoleId);
        if (!explicitContentResult.IsSuccess)
            return Result<ContentMutationResult>.Failure(explicitContentResult.Error!);

        var deletedContent = explicitContentResult.Value! with { IsDeleted = true };
        return Result<ContentMutationResult>.Success(
            new ContentMutationResult(deletedContent, Replace(contents, deletedContent)));
    }

    private static Result<NodeContent> FindSingleActiveExplicitContent(
        IEnumerable<NodeContent> contents,
        NodeId nodeId,
        RoleId roleId)
    {
        var matchingContents = contents
            .Where(content => !content.IsDeleted && content.NodeId == nodeId && content.RoleId == roleId)
            .ToArray();

        return matchingContents.Length == 1
            ? Result<NodeContent>.Success(matchingContents[0])
            : Result<NodeContent>.Failure(CreateContentNotFoundError(nodeId, roleId));
    }

    private static IReadOnlyList<NodeContent> Replace(IEnumerable<NodeContent> contents, NodeContent replacement) =>
        Array.AsReadOnly(contents.Select(content =>
            content.SnapshotId == replacement.SnapshotId
                && content.NodeId == replacement.NodeId
                && content.RoleId == replacement.RoleId
                ? replacement
                : content).ToArray());

    private static DomainError CreateContentNotFoundError(NodeId nodeId, RoleId roleId) =>
        new(
            TextOperationCodes.ContentNotFound,
            "Für die angefragte Node und Rolle existiert kein aktiver expliziter Content.",
            new Dictionary<string, string>
            {
                [TextOperationCodes.NodeIdDetail] = nodeId.ToString(),
                [TextOperationCodes.RoleIdDetail] = roleId.ToString()
            });
}
