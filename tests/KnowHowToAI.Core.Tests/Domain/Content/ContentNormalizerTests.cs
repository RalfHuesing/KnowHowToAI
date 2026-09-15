using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Tests.Domain.Content;

[Trait("Category", "Unit")]
public sealed class ContentNormalizerTests
{
    [Theory]
    [InlineData("Eine Zeile\r\nNoch eine Zeile", "Eine Zeile\nNoch eine Zeile")]
    [InlineData("Eine Zeile\rNoch eine Zeile", "Eine Zeile\nNoch eine Zeile")]
    [InlineData("Eine Zeile\nNoch eine Zeile", "Eine Zeile\nNoch eine Zeile")]
    public void Normalize_UsesLfForEveryLineEnding(string content, string expected)
    {
        var normalizedContent = ContentNormalizer.Normalize(content);

        Assert.Equal(expected, normalizedContent);
    }

    [Fact]
    public void Normalize_PreservesContentOtherThanLineEndings()
    {
        const string content = "  `C#`\r\n\r\n- Eintrag  \r\n";

        var normalizedContent = ContentNormalizer.Normalize(content);

        Assert.Equal("  `C#`\n\n- Eintrag  \n", normalizedContent);
    }
}
