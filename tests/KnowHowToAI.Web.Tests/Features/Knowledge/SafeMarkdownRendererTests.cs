using KnowHowToAI.Server.Web.Features.Knowledge;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class SafeMarkdownRendererTests
{
    [Fact]
    public void Render_NullOrEmpty_ReturnsEmpty()
    {
        Assert.True(string.IsNullOrEmpty(SafeMarkdownRenderer.Render(null).Value));
        Assert.True(string.IsNullOrEmpty(SafeMarkdownRenderer.Render("").Value));
        Assert.True(string.IsNullOrEmpty(SafeMarkdownRenderer.Render("   ").Value));
    }

    [Fact]
    public void Render_Paragraph_ProducesHtmlParagraph()
    {
        var result = SafeMarkdownRenderer.Render("Einfacher Text.");
        Assert.Contains("<p>Einfacher Text.</p>", result.Value);
    }

    [Fact]
    public void Render_Bold_ProducesStrongTag()
    {
        var result = SafeMarkdownRenderer.Render("**fett**");
        Assert.Contains("<strong>fett</strong>", result.Value);
    }

    [Fact]
    public void Render_InlineCode_ProducesCodeTag()
    {
        var result = SafeMarkdownRenderer.Render("`code`");
        Assert.Contains("<code>code</code>", result.Value);
    }

    [Fact]
    public void Render_FencedCodeBlock_ProducesPreAndCode()
    {
        var result = SafeMarkdownRenderer.Render("```\nvar x = 1;\n```");
        Assert.Contains("<pre>", result.Value);
        Assert.Contains("<code>", result.Value);
    }

    [Fact]
    public void Render_RawHtml_IsNotExecuted()
    {
        // O-020: kein Raw-HTML-Rendering
        var result = SafeMarkdownRenderer.Render("<script>alert('xss')</script>");
        Assert.DoesNotContain("<script>", result.Value);
    }

    [Fact]
    public void Render_HtmlTags_AreNotPassedThrough()
    {
        // DisableHtml() verhindert direkte HTML-Tags in der Ausgabe
        var result = SafeMarkdownRenderer.Render("<b>bold</b>");
        // Der Text kann als Escape oder weggelassen werden – nicht als aktives <b> gerendert
        Assert.DoesNotContain("<b>", result.Value);
    }

    [Fact]
    public void Render_JavaScriptLink_IsNeutralized()
    {
        // O-020: JavaScript-Links werden nicht ausgeführt
        var result = SafeMarkdownRenderer.Render("[Klick](javascript:alert('xss'))");
        Assert.DoesNotContain("javascript:", result.Value);
        // Link-Text wird noch gerendert, aber href ist #
        Assert.Contains("Klick", result.Value);
    }

    [Fact]
    public void Render_DataUriLink_IsNeutralized()
    {
        var result = SafeMarkdownRenderer.Render("[Link](data:text/html,<script>alert(1)</script>)");
        Assert.DoesNotContain("data:", result.Value);
    }

    [Fact]
    public void Render_ExternalImage_DoesNotProduceExternalSrc()
    {
        // O-020: externe Bilder lösen im Browser keinen Request aus
        var result = SafeMarkdownRenderer.Render("![Alt](https://evil.example.com/tracker.gif)");
        Assert.DoesNotContain("https://evil.example.com", result.Value);
    }

    [Fact]
    public void Render_SafeHttpLink_IsPreserved()
    {
        // Normale http-Links sind zulässig (O-020 verbietet nur JavaScript, data, file)
        var result = SafeMarkdownRenderer.Render("[Seite](https://example.com)");
        Assert.Contains("https://example.com", result.Value);
    }

    [Fact]
    public void Render_Table_ProducesHtmlTable()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";
        var result = SafeMarkdownRenderer.Render(markdown);
        Assert.Contains("<table>", result.Value);
        Assert.Contains("<td>", result.Value);
    }
}
