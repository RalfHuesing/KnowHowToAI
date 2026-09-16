using KnowHowToAI.Core.Application.History;

namespace KnowHowToAI.Core.Tests.Application.History;

[Trait("Category", "Unit")]
public sealed class ReleaseCursorTests
{
    [Fact]
    public void Encode_And_TryDecode_Roundtrip_Succeeds()
    {
        var cursor = new ReleaseCursor(42);
        var encoded = cursor.Encode();

        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);

        var decoded = ReleaseCursor.TryDecode(encoded);
        Assert.NotNull(decoded);
        Assert.Equal(42, decoded.AfterReleaseId);
    }

    [Fact]
    public void TryDecode_PlainNumericString_ReturnsNullBecauseCursorMustBeOpaque()
    {
        var decoded = ReleaseCursor.TryDecode("123");
        Assert.Null(decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryDecode_NullOrWhitespace_ReturnsNull(string? input)
    {
        Assert.Null(ReleaseCursor.TryDecode(input));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc!@#")]
    [InlineData("not-a-cursor")]
    public void TryDecode_InvalidOrZero_ReturnsNull(string input)
    {
        Assert.Null(ReleaseCursor.TryDecode(input));
    }
}
