using Markdig;

namespace KnowHowToAI.Core.Tests.Domain.Content;

[Trait("Category", "Unit")]
public sealed class GoldenMasterTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    [Fact]
    public void GoldenMaster_ContainsAllSupportedConstructsAndProvidesTheMarkdigBaseline()
    {
        var markdown = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "GoldenMaster.md"));

        Assert.True(markdown.Length >= 4096);
        var baseline = SemanticProjection(markdown);
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

}
