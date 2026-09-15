using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Tests.Domain.Content;

[Trait("Category", "Unit")]
public sealed class ContentRevisionPlannerTests
{
    [Fact]
    public void Decide_NewContent_RequiresANewRevision()
    {
        var decision = ContentRevisionPlanner.Decide(existingContent: null, "Erster Inhalt.\r\n");

        Assert.True(decision.RequiresNewRevision);
        Assert.Equal("Erster Inhalt.\n", decision.NormalizedContentMd);
    }

    [Fact]
    public void Decide_OnlyDifferentLineEndings_RetainsExistingRevision()
    {
        var existingContent = Content("Erster Inhalt.\n", isDeleted: false);

        var decision = ContentRevisionPlanner.Decide(existingContent, "Erster Inhalt.\r\n");

        Assert.False(decision.RequiresNewRevision);
        Assert.Equal(existingContent.ContentMd, decision.NormalizedContentMd);
    }

    [Fact]
    public void Decide_ChangedNormalizedText_RequiresANewRevision()
    {
        var decision = ContentRevisionPlanner.Decide(Content("Alt", isDeleted: false), "Neu");

        Assert.True(decision.RequiresNewRevision);
        Assert.Equal("Neu", decision.NormalizedContentMd);
    }

    [Fact]
    public void Decide_RecreatedDeletedContent_RequiresANewRevisionEvenWhenTextMatches()
    {
        var decision = ContentRevisionPlanner.Decide(Content("Unverändert", isDeleted: true), "Unverändert");

        Assert.True(decision.RequiresNewRevision);
    }

    private static NodeContent Content(string content, bool isDeleted) =>
        new(
            new SnapshotId(1),
            new NodeId(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f")),
            new RoleId("Developer"),
            new ContentRevisionId(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f")),
            ContentMode.Independent,
            content,
            isDeleted);
}
