using KnowHowToAI.Core.Application.Retrieval.Search;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Search;

[Trait("Category", "Unit")]
public sealed class LikeEscapingTests
{
    [Fact]
    public void Escape_NullOrEmpty_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, LikeEscaping.Escape(null));
        Assert.Equal(string.Empty, LikeEscaping.Escape(string.Empty));
    }

    [Fact]
    public void Escape_PlainTextWithoutWildcards_ReturnsUnchanged()
    {
        const string input = "HelloWorld123";
        Assert.Equal(input, LikeEscaping.Escape(input));
    }

    [Theory]
    [InlineData("%", @"\%")]
    [InlineData("_", @"\_")]
    [InlineData("[", @"\[")]
    [InlineData(@"\", @"\\")]
    [InlineData("100%", @"100\%")]
    [InlineData("user_name", @"user\_name")]
    [InlineData("[test]", @"\[test]")]
    [InlineData(@"C:\Path\To\File", @"C:\\Path\\To\\File")]
    public void Escape_SpecialWildcards_EscapesWithBackslash(string input, string expected)
    {
        Assert.Equal(expected, LikeEscaping.Escape(input));
    }

    [Fact]
    public void Escape_MixedComplexInput_EscapesAllSpecialCharacters()
    {
        const string input = @"100% discount on [item_1] in path C:\temp\dir";
        const string expected = @"100\% discount on \[item\_1] in path C:\\temp\\dir";

        Assert.Equal(expected, LikeEscaping.Escape(input));
    }
}
