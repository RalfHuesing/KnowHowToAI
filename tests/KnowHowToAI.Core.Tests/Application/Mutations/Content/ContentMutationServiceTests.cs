using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Content;

[Trait("Category", "Unit")]
public sealed class ContentMutationServiceTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId NodeId = new(Guid.Parse("342c9f4a-0a77-44be-8f98-f403444d3e9f"));
    private static readonly RoleId DeveloperRoleId = new("Developer");
    private static readonly RoleId ConsultantRoleId = new("Consultant");
    private static readonly ContentRevisionId ExistingRevisionId = new(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
    private static readonly ContentRevisionId GeneratedRevisionId = new(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f"));

    [Fact]
    public void ReplaceText_ExplicitContent_ReplacesOnlyTheRequestedRoleAndAssignsARevision()
    {
        var service = CreateService();
        var developerContent = Content(DeveloperRoleId, "Alt");
        var consultantContent = Content(ConsultantRoleId, "Alt");

        var result = service.ReplaceText(
            [developerContent, consultantContent],
            new ReplaceTextCommand(NodeId, DeveloperRoleId, "Alt", "Neu", "Knoten", true));

        Assert.True(result.IsSuccess);
        Assert.Equal("Neu", result.Value!.ChangedContent.ContentMd);
        Assert.Equal(GeneratedRevisionId, result.Value.ChangedContent.ContentRevisionId);
        Assert.Equal("Neu", Find(result.Value.Contents, DeveloperRoleId).ContentMd);
        Assert.Equal("Alt", Find(result.Value.Contents, ConsultantRoleId).ContentMd);
    }

    [Fact]
    public void ReplaceText_ContentOnlyAvailableForAnotherRole_DoesNotUseFallback()
    {
        var service = CreateService();

        var result = service.ReplaceText(
            [Content(ConsultantRoleId, "Alt")],
            new ReplaceTextCommand(NodeId, DeveloperRoleId, "Alt", "Neu", "Knoten", true));

        Assert.False(result.IsSuccess);
        Assert.Equal(TextOperationCodes.ContentNotFound, result.Code);
        Assert.Equal(NodeId.ToString(), result.Details[TextOperationCodes.NodeIdDetail]);
        Assert.Equal(DeveloperRoleId.ToString(), result.Details[TextOperationCodes.RoleIdDetail]);
    }

    [Fact]
    public void DeleteContent_TombstonesOnlyTheRequestedExplicitRoleContent()
    {
        var service = CreateService();
        var developerContent = Content(DeveloperRoleId, "Entwicklung");
        var consultantContent = Content(ConsultantRoleId, "Beratung");

        var result = service.DeleteContent(
            [developerContent, consultantContent],
            new DeleteContentCommand(NodeId, DeveloperRoleId));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.ChangedContent.IsDeleted);
        Assert.True(Find(result.Value.Contents, DeveloperRoleId).IsDeleted);
        Assert.False(Find(result.Value.Contents, ConsultantRoleId).IsDeleted);
        Assert.Equal("Beratung", Find(result.Value.Contents, ConsultantRoleId).ContentMd);
    }

    [Fact]
    public void DeleteContent_DeletedOrMissingExplicitContent_ReturnsContentNotFoundWithoutChangingOtherContents()
    {
        var service = CreateService();
        var developerContent = Content(DeveloperRoleId, "Entwicklung") with { IsDeleted = true };
        var consultantContent = Content(ConsultantRoleId, "Beratung");

        var result = service.DeleteContent(
            [developerContent, consultantContent],
            new DeleteContentCommand(NodeId, DeveloperRoleId));

        Assert.False(result.IsSuccess);
        Assert.Equal(TextOperationCodes.ContentNotFound, result.Code);
        Assert.True(developerContent.IsDeleted);
        Assert.False(consultantContent.IsDeleted);
    }

    private static ContentMutationService CreateService() =>
        new(new ContentRevisionService(new FixedIdentifierGenerator(GeneratedRevisionId)));

    private static NodeContent Content(RoleId roleId, string content) =>
        new(SnapshotId, NodeId, roleId, ExistingRevisionId, ContentMode.Independent, content, IsDeleted: false);

    private static NodeContent Find(IEnumerable<NodeContent> contents, RoleId roleId) =>
        Assert.Single(contents.Where(content => content.RoleId == roleId));

    private sealed class FixedIdentifierGenerator(ContentRevisionId contentRevisionId) : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId() => contentRevisionId;
    }
}
