using KnowHowToAI.Core.Sync;

namespace KnowHowToAI.Core.Tests;

public class BuildLikePatternTests
{
    [Fact]
    public void BuildLikePattern_AllowsNormalSubstring() =>
        Assert.Equal("%routing%", SqlDocumentsStore.BuildLikePattern("routing"));

    [Fact]
    public void BuildLikePattern_EscapesPercent() =>
        Assert.Equal("%50[%]%", SqlDocumentsStore.BuildLikePattern("50%"));

    [Fact]
    public void BuildLikePattern_EscapesUnderscore() =>
        Assert.Equal("%a[_]b%", SqlDocumentsStore.BuildLikePattern("a_b"));

    [Fact]
    public void BuildLikePattern_EscapesOpeningBracket() =>
        Assert.Equal("%[[]abc%", SqlDocumentsStore.BuildLikePattern("[abc"));

    [Fact]
    public void BuildLikePattern_EmptyInput_ReturnsPercentPercent() =>
        Assert.Equal("%%", SqlDocumentsStore.BuildLikePattern(string.Empty));

    [Fact]
    public void BuildLikePattern_OrderOfEscapesDoesNotDoubleEscape() =>
        Assert.Equal("%[[][%]]%", SqlDocumentsStore.BuildLikePattern("[%]"));

    [Fact]
    public void BuildLikePattern_AllThreeWildcardsInOneInput_AllEscaped() =>
        Assert.Equal("%[%]a[_]b[[]c]%", SqlDocumentsStore.BuildLikePattern("%a_b[c]"));
}
