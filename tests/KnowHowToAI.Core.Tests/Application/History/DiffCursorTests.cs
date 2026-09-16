using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Application.History;

[Trait("Category", "Unit")]
public sealed class DiffCursorTests
{
    [Fact]
    public void EncodeAndDecode_AllFieldsSet_RoundtripsSuccessfully()
    {
        var cursor = new DiffCursor(
            new SnapshotId(10),
            new SnapshotId(20),
            42L,
            15);

        var encoded = cursor.Encode();
        Assert.NotNull(encoded);

        var decoded = DiffCursor.TryDecode(encoded);
        Assert.NotNull(decoded);
        Assert.Equal(cursor.BaseSnapshotId, decoded.BaseSnapshotId);
        Assert.Equal(cursor.TargetSnapshotId, decoded.TargetSnapshotId);
        Assert.Equal(cursor.ChangeVersion, decoded.ChangeVersion);
        Assert.Equal(cursor.NextOffset, decoded.NextOffset);
    }

    [Fact]
    public void EncodeAndDecode_WithoutChangeVersion_RoundtripsSuccessfully()
    {
        var cursor = new DiffCursor(
            new SnapshotId(1),
            new SnapshotId(2),
            null,
            50);

        var encoded = cursor.Encode();
        var decoded = DiffCursor.TryDecode(encoded);

        Assert.NotNull(decoded);
        Assert.Null(decoded.ChangeVersion);
        Assert.Equal(cursor.BaseSnapshotId, decoded.BaseSnapshotId);
        Assert.Equal(cursor.TargetSnapshotId, decoded.TargetSnapshotId);
        Assert.Equal(50, decoded.NextOffset);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-token!")]
    [InlineData("e30=")] // "{}" -> missing required fields / invalid
    public void TryDecode_InvalidString_ReturnsNull(string? invalidCursor)
    {
        var result = DiffCursor.TryDecode(invalidCursor);
        Assert.Null(result);
    }
}
