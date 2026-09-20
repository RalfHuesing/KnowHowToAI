using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Wendet Content-Mutationen ausschließlich auf explizite Zielgruppen-Contents an.
/// </summary>
public sealed class ContentMutationService(ContentRevisionService revisionService)
{
    private readonly ContentRevisionService _revisionService = revisionService ?? throw new ArgumentNullException(nameof(revisionService));

    public Result<ContentMutationResult> ReplaceContent(
        IEnumerable<NodeContent> existingContents,
        IEnumerable<ContentDependency> existingDependencies,
        ReplaceContentCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingContents);
        ArgumentNullException.ThrowIfNull(existingDependencies);
        ArgumentNullException.ThrowIfNull(command);

        var contents = existingContents.ToArray();
        var dependencies = existingDependencies.ToArray();
        var existingContent = contents.SingleOrDefault(content =>
            !content.IsDeleted && content.NodeId == command.NodeId && content.AudienceId == command.AudienceId);
        var assignment = _revisionService.Assign(existingContent, command.ContentMd);
        var structureReport = MarkdownStructureValidator.Validate(
            assignment.NormalizedContentMd,
            command.NodeTitle,
            command.WarnOnPossibleEmbeddedHeading);
        if (!structureReport.IsValid)
            return Result<ContentMutationResult>.Failure(structureReport.Errors[0], structureReport.Warnings);

        var changedContent = new NodeContent(
            command.SnapshotId,
            command.NodeId,
            command.AudienceId,
            assignment.ContentRevisionId,
            command.ContentMode,
            assignment.NormalizedContentMd,
            IsDeleted: false);
        var updatedContents = existingContent is null
            ? Array.AsReadOnly(contents.Append(changedContent).ToArray())
            : Replace(contents, changedContent);
        var unchangedDependencies = dependencies.Where(dependency =>
            dependency.SnapshotId != command.SnapshotId
            || dependency.TargetNodeId != command.NodeId
            || dependency.TargetAudienceId != command.AudienceId);
        var updatedDependencies = Array.AsReadOnly(unchangedDependencies.Concat(command.Dependencies).ToArray());
        var dependencyReport = DependencyValidator.ValidateNewOrChangedDependencies(
            updatedContents,
            updatedDependencies,
            command.Dependencies);
        if (!dependencyReport.IsValid)
            return Result<ContentMutationResult>.Failure(dependencyReport.Errors[0], structureReport.Warnings);

        return Result<ContentMutationResult>.Success(
            new ContentMutationResult(changedContent, updatedContents),
            structureReport.Warnings);
    }

    public Result<ContentMutationResult> ReplaceText(
        IEnumerable<NodeContent> existingContents,
        ReplaceTextCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingContents);
        ArgumentNullException.ThrowIfNull(command);

        var contents = existingContents.ToArray();
        var explicitContentResult = FindSingleActiveExplicitContent(contents, command.NodeId, command.AudienceId);
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

    public Result<ContentDeletionResult> DeleteContent(
        IEnumerable<NodeContent> existingContents,
        IEnumerable<ContentDependency> existingDependencies,
        DeleteContentCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingContents);
        ArgumentNullException.ThrowIfNull(existingDependencies);
        ArgumentNullException.ThrowIfNull(command);

        var contents = existingContents.ToArray();
        var dependencies = existingDependencies.ToArray();
        var explicitContentResult = FindSingleActiveExplicitContent(contents, command.NodeId, command.AudienceId);
        if (!explicitContentResult.IsSuccess)
            return Result<ContentDeletionResult>.Failure(explicitContentResult.Error!);

        var deletedContent = explicitContentResult.Value! with { IsDeleted = true };
        var updatedContents = Replace(contents, deletedContent);
        var updatedDependencies = Array.AsReadOnly(dependencies
            .Where(dependency => dependency.SnapshotId != deletedContent.SnapshotId
                || dependency.TargetNodeId != deletedContent.NodeId
                || dependency.TargetAudienceId != deletedContent.AudienceId)
            .ToArray());

        return Result<ContentDeletionResult>.Success(
            new ContentDeletionResult(deletedContent, updatedContents, updatedDependencies));
    }

    private static Result<NodeContent> FindSingleActiveExplicitContent(
        IEnumerable<NodeContent> contents,
        NodeId nodeId,
        AudienceId audienceId)
    {
        var matchingContents = contents
            .Where(content => !content.IsDeleted && content.NodeId == nodeId && content.AudienceId == audienceId)
            .ToArray();

        return matchingContents.Length == 1
            ? Result<NodeContent>.Success(matchingContents[0])
            : Result<NodeContent>.Failure(CreateContentNotFoundError(nodeId, audienceId));
    }

    private static IReadOnlyList<NodeContent> Replace(IEnumerable<NodeContent> contents, NodeContent replacement) =>
        Array.AsReadOnly(contents.Select(content =>
            content.SnapshotId == replacement.SnapshotId
                && content.NodeId == replacement.NodeId
                && content.AudienceId == replacement.AudienceId
                ? replacement
                : content).ToArray());

    private static DomainError CreateContentNotFoundError(NodeId nodeId, AudienceId audienceId) =>
        new(
            TextOperationCodes.ExplicitContentNotFound,
            "Für die angefragte Node und Zielgruppe existiert kein aktiver expliziter Content.",
            new Dictionary<string, string>
            {
                [TextOperationCodes.NodeIdDetail] = nodeId.ToString(),
                [TextOperationCodes.AudienceIdDetail] = audienceId.ToString()
            });
}
