using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Content;

[Trait("Category", "Unit")]
public sealed class ContentMutationServiceTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId NodeId = new(Guid.Parse("342c9f4a-0a77-44be-8f98-f403444d3e9f"));
    private static readonly AudienceId DeveloperAudienceId = new("Developer");
    private static readonly AudienceId ConsultantAudienceId = new("Consultant");
    private static readonly ContentRevisionId ExistingRevisionId = new(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
    private static readonly ContentRevisionId GeneratedRevisionId = new(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f"));

    [Fact]
    public void ReplaceText_ExplicitContent_ReplacesOnlyTheRequestedAudienceAndAssignsARevision()
    {
        var service = CreateService();
        var developerContent = Content(DeveloperAudienceId, "Alt");
        var consultantContent = Content(ConsultantAudienceId, "Alt");

        var result = service.ReplaceText(
            [developerContent, consultantContent],
            new ReplaceTextCommand(NodeId, DeveloperAudienceId, "Alt", "Neu", "Knoten", true));

        Assert.True(result.IsSuccess);
        Assert.Equal("Neu", result.Value!.ChangedContent.ContentMd);
        Assert.Equal(GeneratedRevisionId, result.Value.ChangedContent.ContentRevisionId);
        Assert.Equal("Neu", Find(result.Value.Contents, DeveloperAudienceId).ContentMd);
        Assert.Equal("Alt", Find(result.Value.Contents, ConsultantAudienceId).ContentMd);
    }

    [Fact]
    public void ReplaceText_ContentOnlyAvailableForAnotherAudience_DoesNotUseFallback()
    {
        var service = CreateService();

        var result = service.ReplaceText(
            [Content(ConsultantAudienceId, "Alt")],
            new ReplaceTextCommand(NodeId, DeveloperAudienceId, "Alt", "Neu", "Knoten", true));

        Assert.False(result.IsSuccess);
        Assert.Equal(TextOperationCodes.ExplicitContentNotFound, result.Code);
        Assert.Equal(NodeId.ToString(), result.Details[TextOperationCodes.NodeIdDetail]);
        Assert.Equal(DeveloperAudienceId.ToString(), result.Details[TextOperationCodes.AudienceIdDetail]);
    }

    [Fact]
    public void DeleteContent_TombstonesOnlyTheRequestedExplicitAudienceContent()
    {
        var service = CreateService();
        var developerContent = Content(DeveloperAudienceId, "Entwicklung");
        var consultantContent = Content(ConsultantAudienceId, "Beratung");

        var result = service.DeleteContent(
            [developerContent, consultantContent],
            [],
            new DeleteContentCommand(NodeId, DeveloperAudienceId));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.ChangedContent.IsDeleted);
        Assert.True(Find(result.Value.Contents, DeveloperAudienceId).IsDeleted);
        Assert.False(Find(result.Value.Contents, ConsultantAudienceId).IsDeleted);
        Assert.Equal("Beratung", Find(result.Value.Contents, ConsultantAudienceId).ContentMd);
    }

    [Fact]
    public void DeleteContent_DeletedOrMissingExplicitContent_ReturnsContentNotFoundWithoutChangingOtherContents()
    {
        var service = CreateService();
        var developerContent = Content(DeveloperAudienceId, "Entwicklung") with { IsDeleted = true };
        var consultantContent = Content(ConsultantAudienceId, "Beratung");

        var result = service.DeleteContent(
            [developerContent, consultantContent],
            [],
            new DeleteContentCommand(NodeId, DeveloperAudienceId));

        Assert.False(result.IsSuccess);
        Assert.Equal(TextOperationCodes.ExplicitContentNotFound, result.Code);
        Assert.True(developerContent.IsDeleted);
        Assert.False(consultantContent.IsDeleted);
    }

    [Fact]
    public void DeleteContent_DeletesTargetDependenciesAndPreservesSourceProvenanceAsStale()
    {
        var service = CreateService();
        var sourceNodeId = new NodeId(Guid.Parse("17c9148d-063c-4c4c-8c24-48f6584b6351"));
        var endUserAudienceId = new AudienceId("EndUser");
        var source = Content(DeveloperAudienceId, "Quelle") with { NodeId = sourceNodeId };
        var derived = Content(endUserAudienceId, "Abgeleitet") with { ContentMode = ContentMode.Derived };
        var dependency = new ContentDependency(
            SnapshotId,
            derived.NodeId,
            derived.AudienceId,
            source.NodeId,
            source.AudienceId,
            source.ContentRevisionId);

        var deletingTarget = service.DeleteContent(
            [source, derived],
            [dependency],
            new DeleteContentCommand(derived.NodeId, derived.AudienceId));

        Assert.True(deletingTarget.IsSuccess);
        Assert.Empty(deletingTarget.Value!.Dependencies);
        Assert.True(DependencyValidator.ValidateSnapshot(
            deletingTarget.Value.Contents,
            deletingTarget.Value.Dependencies).IsValid);

        var deletingSource = service.DeleteContent(
            [source, derived],
            [dependency],
            new DeleteContentCommand(source.NodeId, source.AudienceId));

        Assert.True(deletingSource.IsSuccess);
        Assert.Equal([dependency], deletingSource.Value!.Dependencies);
        Assert.True(DependencyValidator.ValidateSnapshot(
            deletingSource.Value.Contents,
            deletingSource.Value.Dependencies).IsValid);
        Assert.Equal(
            Freshness.Stale,
            FreshnessEvaluator.Evaluate(derived, deletingSource.Value.Contents, deletingSource.Value.Dependencies));
    }

    private static ContentMutationService CreateService() =>
        new(new ContentRevisionService(new FixedIdentifierGenerator { FixedContentRevisionId = GeneratedRevisionId }));

    private static NodeContent Content(AudienceId audienceId, string content) =>
        new(SnapshotId, NodeId, audienceId, ExistingRevisionId, ContentMode.Independent, content, IsDeleted: false);

    private static NodeContent Find(IEnumerable<NodeContent> contents, AudienceId audienceId) =>
        Assert.Single(contents.Where(content => content.AudienceId == audienceId));
}
