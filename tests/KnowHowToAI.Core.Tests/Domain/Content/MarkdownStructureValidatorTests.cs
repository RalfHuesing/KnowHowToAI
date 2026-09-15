using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Tests.Domain.Content;

[Trait("Category", "Unit")]
public sealed class MarkdownStructureValidatorTests
{
    [Theory]
    [InlineData("# Installation", "AtxHeading")]
    [InlineData("Installation\n============", "SetextHeading")]
    public void Validate_MarkdownHeading_ReturnsHardErrorWithPositionAndKind(string content, string expectedKind)
    {
        var report = MarkdownStructureValidator.Validate(content, "Installation", warnOnPossibleEmbeddedHeading: true);

        var error = Assert.Single(report.Errors);
        Assert.False(report.IsValid);
        Assert.Equal(ContentStructureCodes.HeadingNotAllowed, error.Code);
        Assert.Equal(expectedKind, error.Details["kind"]);
        Assert.Equal("1", error.Details["line"]);
        Assert.Equal("1", error.Details["column"]);
    }

    [Theory]
    [InlineData("<h2>Installation</h2>")]
    [InlineData("<H6 class=\"headline\">Installation</H6>")]
    public void Validate_HtmlHeading_RejectsCaseInsensitively(string content)
    {
        var report = MarkdownStructureValidator.Validate(content, "Installation", warnOnPossibleEmbeddedHeading: true);

        var error = Assert.Single(report.Errors);
        Assert.Equal(ContentStructureCodes.HeadingNotAllowed, error.Code);
        Assert.Equal("HtmlHeading", error.Details["kind"]);
    }

    [Fact]
    public void Validate_HeadingLikeSyntaxInCodeEscapesAndNormalText_IsAllowed()
    {
        const string content = """
            C# bleibt Text.
            \# Kein Heading
            `# Inline-Code`

                # Eingerückter Code

            ```csharp
            # Fenced Code
            <h2>Code</h2>
            #if DEBUG
            #endif
            ```
            """;

        var report = MarkdownStructureValidator.Validate(content, "Installation", warnOnPossibleEmbeddedHeading: true);

        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
        Assert.Empty(report.Warnings);
    }

    [Theory]
    [InlineData("**Installation**", "StrongEmphasis")]
    [InlineData("*Installation*", "Emphasis")]
    public void Validate_StandaloneEmphasis_EmitsWarningWithoutRejectingContent(string content, string expectedKind)
    {
        var report = MarkdownStructureValidator.Validate(content, "Anderer Titel", warnOnPossibleEmbeddedHeading: true);

        var warning = Assert.Single(report.Warnings);
        Assert.True(report.IsValid);
        Assert.Equal(ContentStructureCodes.PossibleEmbeddedHeading, warning.Code);
        Assert.Equal(expectedKind, warning.Details["kind"]);
    }

    [Fact]
    public void Validate_StandaloneNodeTitle_EmitsWarningButAFlowingMentionDoesNot()
    {
        var standaloneReport = MarkdownStructureValidator.Validate("Installation", "Installation", warnOnPossibleEmbeddedHeading: true);
        var flowingReport = MarkdownStructureValidator.Validate(
            "Die Installation erfolgt über das Setup.",
            "Installation",
            warnOnPossibleEmbeddedHeading: true);

        var warning = Assert.Single(standaloneReport.Warnings);
        Assert.Equal("NodeTitle", warning.Details["kind"]);
        Assert.Empty(flowingReport.Warnings);
    }

    [Fact]
    public void Validate_FrontMatter_ReturnsHardError()
    {
        const string content = """
            ---
            nodeId: 42
            role: Developer
            ---
            Inhalt
            """;

        var report = MarkdownStructureValidator.Validate(content, "Installation", warnOnPossibleEmbeddedHeading: true);

        var error = Assert.Single(report.Errors);
        Assert.False(report.IsValid);
        Assert.Equal(ContentStructureCodes.FrontMatterNotAllowed, error.Code);
        Assert.Equal("FrontMatter", error.Details["kind"]);
    }

    [Fact]
    public void Validate_DisabledEmbeddedHeadingPolicy_DoesNotEmitHeuristicWarnings()
    {
        var report = MarkdownStructureValidator.Validate("**Installation**", "Installation", warnOnPossibleEmbeddedHeading: false);

        Assert.True(report.IsValid);
        Assert.Empty(report.Warnings);
    }
}
