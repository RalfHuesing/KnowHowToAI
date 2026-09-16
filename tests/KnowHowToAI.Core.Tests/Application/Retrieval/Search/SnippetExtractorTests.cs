using KnowHowToAI.Core.Application.Retrieval.Search;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Search;

[Trait("Category", "Unit")]
public sealed class SnippetExtractorTests
{
    [Fact]
    public void ExtractSnippet_NullOrEmptyText_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SnippetExtractor.ExtractSnippet(null, "foo", 50));
        Assert.Equal(string.Empty, SnippetExtractor.ExtractSnippet(string.Empty, "foo", 50));
        Assert.Equal(string.Empty, SnippetExtractor.ExtractSnippet("   ", "foo", 50));
    }

    [Fact]
    public void ExtractSnippet_ZeroOrNegativeMaxChars_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SnippetExtractor.ExtractSnippet("Hello World", "World", 0));
        Assert.Equal(string.Empty, SnippetExtractor.ExtractSnippet("Hello World", "World", -5));
    }

    [Fact]
    public void ExtractSnippet_TextShorterThanMaxChars_ReturnsTrimmedWithoutEllipsis()
    {
        const string text = "Short text example.";
        var snippet = SnippetExtractor.ExtractSnippet(text, "text", 50);
        Assert.Equal(text, snippet);
    }

    [Fact]
    public void ExtractSnippet_NormalizesWhitespaceAndNewlines()
    {
        const string text = "Line 1\r\nLine 2\twith   extra   spaces.\nLine 3.";
        var snippet = SnippetExtractor.ExtractSnippet(text, "extra", 100);
        Assert.DoesNotContain("\r", snippet);
        Assert.DoesNotContain("\n", snippet);
        Assert.DoesNotContain("\t", snippet);
        Assert.DoesNotContain("   ", snippet);
        Assert.Contains("with extra spaces.", snippet);
    }

    [Fact]
    public void ExtractSnippet_SearchTextAtStart_HasSuffixEllipsisOnly()
    {
        const string text = "Target word is at the very start of this long sentence that exceeds max limit.";
        var snippet = SnippetExtractor.ExtractSnippet(text, "Target", 30);

        Assert.StartsWith("Target", snippet);
        Assert.EndsWith("...", snippet);
        Assert.True(snippet.Length <= 30);
    }

    [Fact]
    public void ExtractSnippet_SearchTextAtEnd_HasPrefixEllipsisOnly()
    {
        const string text = "This sentence leads all the way up to the very ending Target";
        var snippet = SnippetExtractor.ExtractSnippet(text, "Target", 30);

        Assert.StartsWith("...", snippet);
        Assert.EndsWith("Target", snippet);
        Assert.True(snippet.Length <= 30);
    }

    [Fact]
    public void ExtractSnippet_SearchTextInMiddle_CentersAroundMatch()
    {
        const string text = "Start prefix text before and then the Target keyword followed by suffix trailing text.";
        var snippet = SnippetExtractor.ExtractSnippet(text, "Target", 35);

        Assert.StartsWith("...", snippet);
        Assert.EndsWith("...", snippet);
        Assert.Contains("Target", snippet);
        Assert.True(snippet.Length <= 35);
    }

    [Fact]
    public void ExtractSnippet_CaseInsensitiveMatch_FindsAndExtractsSnippet()
    {
        const string text = "Overview of the DATABASE architecture and clustering options.";
        var snippet = SnippetExtractor.ExtractSnippet(text, "database", 30);

        Assert.Contains("DATABASE", snippet);
        Assert.True(snippet.Length <= 30);
    }

    [Fact]
    public void ExtractSnippet_SearchTextNotFound_TruncatesAtEnd()
    {
        const string text = "This is a long text where the requested search term cannot be found anywhere.";
        var snippet = SnippetExtractor.ExtractSnippet(text, "NonExistent", 25);

        Assert.EndsWith("...", snippet);
        Assert.True(snippet.Length <= 25);
    }
}
