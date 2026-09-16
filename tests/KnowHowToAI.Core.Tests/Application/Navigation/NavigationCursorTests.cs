using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NavigationCursorTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId ParentNodeId = new(Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"));
    private static readonly NodeId LastNodeId = new(Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"));
    private static readonly RoleId RoleId = new("Developer");

    [Fact]
    public void Encode_And_TryDecode_Roundtrip_PreservesAllFields()
    {
        var cursor = new NavigationCursor(
            SnapshotId,
            ChangeVersion: 7,
            ParentNodeId,
            RoleId,
            IncludeDeleted: true,
            LastNodeId,
            LastSortOrder: 15);

        var encoded = cursor.Encode();
        Assert.False(string.IsNullOrWhiteSpace(encoded));

        var decoded = NavigationCursor.TryDecode(encoded);
        Assert.NotNull(decoded);
        Assert.Equal(cursor.SnapshotId, decoded.SnapshotId);
        Assert.Equal(cursor.ChangeVersion, decoded.ChangeVersion);
        Assert.Equal(cursor.ParentNodeId, decoded.ParentNodeId);
        Assert.Equal(cursor.RoleId, decoded.RoleId);
        Assert.Equal(cursor.IncludeDeleted, decoded.IncludeDeleted);
        Assert.Equal(cursor.LastNodeId, decoded.LastNodeId);
        Assert.Equal(cursor.LastSortOrder, decoded.LastSortOrder);
    }

    [Fact]
    public void Encode_And_TryDecode_WithNullOptionalFields_Succeeds()
    {
        var cursor = new NavigationCursor(
            SnapshotId,
            ChangeVersion: null,
            ParentNodeId: null,
            RoleId,
            IncludeDeleted: false,
            LastNodeId,
            LastSortOrder: 0);

        var encoded = cursor.Encode();
        var decoded = NavigationCursor.TryDecode(encoded);

        Assert.NotNull(decoded);
        Assert.Null(decoded.ChangeVersion);
        Assert.Null(decoded.ParentNodeId);
        Assert.Equal(cursor.SnapshotId, decoded.SnapshotId);
        Assert.Equal(cursor.RoleId, decoded.RoleId);
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
        var decoded = NavigationCursor.TryDecode(invalidCursor);
        Assert.Null(decoded);
    }

    [Fact]
    public void TryDecode_EmptyLastNodeId_ReturnsNull()
    {
        var encoded = new NavigationCursor(
            SnapshotId,
            ChangeVersion: null,
            ParentNodeId: null,
            RoleId,
            IncludeDeleted: false,
            new NodeId(Guid.Empty),
            LastSortOrder: 0).Encode();

        Assert.Null(NavigationCursor.TryDecode(encoded));
    }
}
