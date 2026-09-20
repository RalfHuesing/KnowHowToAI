using Markdig;

namespace KnowHowToAI.Core.Tests.Domain.Content;

[Trait("Category", "Unit")]
public sealed class GoldenMasterTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    [Fact]
    public void GoldenMaster_ContainsAllSupportedConstructsAndRemainsMarkdigSemanticallyStableForFiveCycles()
    {
        var markdown = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "GoldenMaster.md"));

        Assert.True(markdown.Length >= 4096);
        var baseline = SemanticProjection(markdown);
        var current = markdown;

        for (var cycle = 1; cycle <= 5; cycle++)
        {
            current = Canonicalize(current);
            Assert.Equal(baseline, SemanticProjection(current));
        }

        Assert.Contains("<strong>", baseline, StringComparison.Ordinal);
        Assert.Contains("<em>", baseline, StringComparison.Ordinal);
        Assert.Contains("<del>", baseline, StringComparison.Ordinal);
        Assert.Contains("<a href=\"https://example.test/docs\">", baseline, StringComparison.Ordinal);
        Assert.Contains("<ol>", baseline, StringComparison.Ordinal);
        Assert.Contains("<ul>", baseline, StringComparison.Ordinal);
        Assert.Contains("<table>", baseline, StringComparison.Ordinal);
        Assert.Contains("<code>", baseline, StringComparison.Ordinal);
        Assert.Contains("<blockquote>", baseline, StringComparison.Ordinal);
        Assert.Contains("😀", baseline, StringComparison.Ordinal);
    }

    private static string SemanticProjection(string markdown) => Markdown.ToHtml(markdown, Pipeline);

    private static string Canonicalize(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return string.Join('\n', normalized.Split('\n').Select(line => line.TrimEnd()));
    }
}
