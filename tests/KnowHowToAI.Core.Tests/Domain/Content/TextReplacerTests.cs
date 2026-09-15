using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Tests.Domain.Content;

[Trait("Category", "Unit")]
public sealed class TextReplacerTests
{
    [Fact]
    public void Replace_OneOrdinalMatch_ReplacesAndNormalizesTheResult()
    {
        var result = TextReplacer.Replace(new TextReplacementRequest(
            "Erster\r\nZweiter",
            "Erster\r\n",
            "Aktualisiert\r\n",
            "Knoten",
            WarnOnPossibleEmbeddedHeading: true));

        Assert.True(result.IsSuccess);
        Assert.Equal("Aktualisiert\nZweiter", result.Value);
    }

    [Fact]
    public void Replace_MissingText_ReturnsTextNotFound()
    {
        var result = TextReplacer.Replace(new TextReplacementRequest("Inhalt", "fehlt", "neu", "Knoten", true));

        Assert.False(result.IsSuccess);
        Assert.Equal(TextOperationCodes.TextNotFound, result.Code);
    }

    [Fact]
    public void Replace_OverlappingMatches_ReturnsMultipleTextMatches()
    {
        var result = TextReplacer.Replace(new TextReplacementRequest("aaa", "aa", "neu", "Knoten", true));

        Assert.False(result.IsSuccess);
        Assert.Equal(TextOperationCodes.MultipleTextMatches, result.Code);
        Assert.Equal("2", result.Details[TextOperationCodes.MatchCountDetail]);
    }

    [Theory]
    [InlineData("aaa", "a", 3)]
    [InlineData("aaaa", "aa", 3)]
    [InlineData("abababa", "aba", 3)]
    [InlineData("abc", "", 4)]
    public void Replace_EveryGeneratedMultipleMatchCase_ReturnsTheOrdinalMatchCount(
        string content,
        string oldText,
        int expectedMatchCount)
    {
        var result = TextReplacer.Replace(new TextReplacementRequest(content, oldText, "neu", "Knoten", true));

        Assert.False(result.IsSuccess);
        Assert.Equal(TextOperationCodes.MultipleTextMatches, result.Code);
        Assert.Equal(expectedMatchCount.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Details[TextOperationCodes.MatchCountDetail]);
    }

    [Fact]
    public void Replace_ResultWithHeading_ReturnsStructureError()
    {
        var result = TextReplacer.Replace(new TextReplacementRequest("Alt", "Alt", "# Nicht erlaubt", "Knoten", true));

        Assert.False(result.IsSuccess);
        Assert.Equal(ContentStructureCodes.HeadingNotAllowed, result.Code);
    }

    [Fact]
    public void Replace_PossibleEmbeddedHeading_ReturnsWarningWithoutBlockingTheReplacement()
    {
        var result = TextReplacer.Replace(new TextReplacementRequest("Alt", "Alt", "**Ersatztitel**", "Knoten", true));

        Assert.True(result.IsSuccess);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(ContentStructureCodes.PossibleEmbeddedHeading, warning.Code);
    }
}
