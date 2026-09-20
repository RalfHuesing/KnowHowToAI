using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Domain.Common;

[Trait("Category", "Unit")]
public sealed class KnowledgeIdentifiersTests
{
    [Fact]
    public void Identifiers_PreserveTheirTypedValuesAndTransportRepresentation()
    {
        var guid = Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f");

        Assert.Equal(42, new SnapshotId(42).Value);
        Assert.Equal("42", new SnapshotId(42).ToString());
        Assert.Equal(guid, new TransactionId(guid).Value);
        Assert.Equal("9f4a2c43-0a77-44be-8f98-f403444d3e9f", new TransactionId(guid).ToString());
        Assert.Equal(guid, new NodeId(guid).Value);
        Assert.Equal("Developer", new AudienceId("Developer").ToString());
        Assert.Equal(guid, new ContentRevisionId(guid).Value);
        Assert.Equal("7", new ReleaseId(7).ToString());
    }

    [Fact]
    public void IdentifierKinds_UseTheirOwnValueEquality()
    {
        var first = new NodeId(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
        var same = new NodeId(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
        var other = new NodeId(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f"));

        Assert.Equal(first, same);
        Assert.NotEqual(first, other);
    }
}
