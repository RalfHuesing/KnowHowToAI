using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class RoleCursorTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly RoleId RoleId = new("Developer");

    [Fact]
    public void Encode_And_TryDecode_Roundtrip_PreservesAllFields()
    {
        var cursor = new RoleCursor(
            SnapshotId,
            ChangeVersion: 5,
            IncludeDeleted: true,
            RoleId);

        var encoded = cursor.Encode();
        Assert.False(string.IsNullOrWhiteSpace(encoded));

        var decoded = RoleCursor.TryDecode(encoded);
        Assert.NotNull(decoded);
        Assert.Equal(cursor.SnapshotId, decoded.SnapshotId);
        Assert.Equal(cursor.ChangeVersion, decoded.ChangeVersion);
        Assert.Equal(cursor.IncludeDeleted, decoded.IncludeDeleted);
        Assert.Equal(cursor.LastRoleId, decoded.LastRoleId);
    }

    [Fact]
    public void Encode_And_TryDecode_WithNullChangeVersion_Succeeds()
    {
        var cursor = new RoleCursor(
            SnapshotId,
            ChangeVersion: null,
            IncludeDeleted: false,
            RoleId);

        var encoded = cursor.Encode();
        var decoded = RoleCursor.TryDecode(encoded);

        Assert.NotNull(decoded);
        Assert.Null(decoded.ChangeVersion);
        Assert.Equal(cursor.SnapshotId, decoded.SnapshotId);
        Assert.Equal(cursor.LastRoleId, decoded.LastRoleId);
        Assert.False(decoded.IncludeDeleted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!NotBase64Url!!!")]
    [InlineData("YWJj")] // "abc" in Base64, not JSON
    [InlineData("e30=")] // "{}" in Base64, missing required fields
    public void TryDecode_InvalidOrCorruptString_ReturnsNull(string? invalidCursor)
    {
        var decoded = RoleCursor.TryDecode(invalidCursor);
        Assert.Null(decoded);
    }
}
