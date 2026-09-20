using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Search;

[Trait("Category", "Unit")]
public sealed class SearchCursorTests
{
    [Fact]
    public void EncodeAndDecode_AllFieldsSet_RoundtripsSuccessfully()
    {
        var cursor = new SearchCursor(
            new SnapshotId(42),
            123L,
            "search phrase",
            new AudienceId("Developer"),
            1,
            10,
            new NodeId(Guid.Parse("11111111-2222-3333-4444-555555555555")));

        var encoded = cursor.Encode();
        Assert.NotNull(encoded);

        var decoded = SearchCursor.TryDecode(encoded);
        Assert.NotNull(decoded);
        Assert.Equal(cursor.SnapshotId, decoded.SnapshotId);
        Assert.Equal(cursor.ChangeVersion, decoded.ChangeVersion);
        Assert.Equal(cursor.QueryText, decoded.QueryText);
        Assert.Equal(cursor.AudienceId, decoded.AudienceId);
        Assert.Equal(cursor.LastRank, decoded.LastRank);
        Assert.Equal(cursor.LastSortOrder, decoded.LastSortOrder);
        Assert.Equal(cursor.LastNodeId, decoded.LastNodeId);
    }

    [Fact]
    public void EncodeAndDecode_OptionalFieldsNull_RoundtripsSuccessfully()
    {
        var cursor = new SearchCursor(
            new SnapshotId(1),
            null,
            "minimal",
            null,
            3,
            0,
            new NodeId(Guid.NewGuid()));

        var encoded = cursor.Encode();
        var decoded = SearchCursor.TryDecode(encoded);

        Assert.NotNull(decoded);
        Assert.Null(decoded.ChangeVersion);
        Assert.Null(decoded.AudienceId);
        Assert.Equal(cursor.SnapshotId, decoded.SnapshotId);
        Assert.Equal(cursor.QueryText, decoded.QueryText);
        Assert.Equal(cursor.LastRank, decoded.LastRank);
        Assert.Equal(cursor.LastNodeId, decoded.LastNodeId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-base64-!!!")]
    [InlineData("e30=")] // "{}" -> empty JSON object without required fields
    public void TryDecode_InvalidOrCorruptString_ReturnsNull(string? invalidCursor)
    {
        var result = SearchCursor.TryDecode(invalidCursor);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void TryDecode_InvalidHitRank_ReturnsNull(int rank)
    {
        var encoded = new SearchCursor(
            new SnapshotId(1),
            null,
            "query",
            null,
            rank,
            0,
            new NodeId(Guid.Parse("11111111-2222-3333-4444-555555555555"))).Encode();

        Assert.Null(SearchCursor.TryDecode(encoded));
    }
}
