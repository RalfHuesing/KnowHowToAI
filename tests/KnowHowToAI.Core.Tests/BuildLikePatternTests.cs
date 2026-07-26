using KnowHowToAI.Core.Sync;

namespace KnowHowToAI.Core.Tests;

public class BuildLikePatternTests
{
    [Theory]
    [InlineData("routing", "%routing%")]
    [InlineData("50%", "%50[%]%")]
    [InlineData("a_b", "%a[_]b%")]
    [InlineData("[abc", "%[[]abc%")]
    [InlineData("", "%%")]
    public void BuildLikePattern_EscapesSqlWildcardsAndWraps(string input, string expected) =>
        Assert.Equal(expected, SqlDocumentsStore.BuildLikePattern(input));

    [Theory]
    [InlineData("[%]", "%[[][%]]%")]
    [InlineData("%a_b[c]", "%[%]a[_]b[[]c]%")]
    public void BuildLikePattern_PreservesEscapingOrder(string input, string expected) =>
        Assert.Equal(expected, SqlDocumentsStore.BuildLikePattern(input));
}
