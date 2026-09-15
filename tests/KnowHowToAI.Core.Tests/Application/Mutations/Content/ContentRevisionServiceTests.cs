using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Content;

[Trait("Category", "Unit")]
public sealed class ContentRevisionServiceTests
{
    private static readonly ContentRevisionId ExistingRevisionId = new(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
    private static readonly ContentRevisionId GeneratedRevisionId = new(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f"));

    [Fact]
    public void Assign_NewContent_GeneratesANewRevision()
    {
        var generator = new CountingIdentifierGenerator(GeneratedRevisionId);
        var service = new ContentRevisionService(generator);

        var assignment = service.Assign(existingContent: null, "Erster Inhalt\r\n");

        Assert.True(assignment.HasNewRevision);
        Assert.Equal(GeneratedRevisionId, assignment.ContentRevisionId);
        Assert.Equal("Erster Inhalt\n", assignment.NormalizedContentMd);
        Assert.Equal(1, generator.ContentRevisionIdRequests);
    }

    [Fact]
    public void Assign_UnchangedNormalizedText_RetainsRevisionWithoutGeneratingAnIdentifier()
    {
        var generator = new CountingIdentifierGenerator(GeneratedRevisionId);
        var service = new ContentRevisionService(generator);

        var assignment = service.Assign(Content("Inhalt\n", isDeleted: false), "Inhalt\r\n");

        Assert.False(assignment.HasNewRevision);
        Assert.Equal(ExistingRevisionId, assignment.ContentRevisionId);
        Assert.Equal("Inhalt\n", assignment.NormalizedContentMd);
        Assert.Equal(0, generator.ContentRevisionIdRequests);
    }

    [Fact]
    public void Assign_ChangedText_GeneratesANewRevision()
    {
        var generator = new CountingIdentifierGenerator(GeneratedRevisionId);
        var service = new ContentRevisionService(generator);

        var assignment = service.Assign(Content("Alt", isDeleted: false), "Neu");

        Assert.True(assignment.HasNewRevision);
        Assert.Equal(GeneratedRevisionId, assignment.ContentRevisionId);
        Assert.Equal(1, generator.ContentRevisionIdRequests);
    }

    [Fact]
    public void Assign_RecreatedDeletedContent_GeneratesANewRevisionForTheSameText()
    {
        var generator = new CountingIdentifierGenerator(GeneratedRevisionId);
        var service = new ContentRevisionService(generator);

        var assignment = service.Assign(Content("Unverändert", isDeleted: true), "Unverändert");

        Assert.True(assignment.HasNewRevision);
        Assert.Equal(GeneratedRevisionId, assignment.ContentRevisionId);
        Assert.Equal(1, generator.ContentRevisionIdRequests);
    }

    private static NodeContent Content(string content, bool isDeleted) =>
        new(
            new SnapshotId(1),
            new NodeId(Guid.Parse("342c9f4a-0a77-44be-8f98-f403444d3e9f")),
            new RoleId("Developer"),
            ExistingRevisionId,
            ContentMode.Derived,
            content,
            isDeleted);

    private sealed class CountingIdentifierGenerator(ContentRevisionId generatedRevisionId) : IIdentifierGenerator
    {
        public int ContentRevisionIdRequests { get; private set; }

        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId()
        {
            ContentRevisionIdRequests++;
            return generatedRevisionId;
        }
    }
}
