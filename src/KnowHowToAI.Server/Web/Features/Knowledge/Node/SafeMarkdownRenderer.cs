using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>
/// Rendert Markdown-Text zu sicherem HTML gemäß O-020:
/// Kein Raw-HTML-Rendering (DisableHtml im Pipeline), keine externen
/// Ressourcen-Requests, keine JavaScript-Links. Nur sichere Inline- und Block-Formate.
/// </summary>
public static class SafeMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = BuildPipeline();

    /// <summary>
    /// Wandelt Markdown-Text in ein sicheres <see cref="MarkupString"/> um.
    /// Gibt leeres Markup zurück, wenn <paramref name="markdown"/> leer oder null ist.
    /// </summary>
    public static MarkupString Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return default;

        var document = Markdown.Parse(markdown, Pipeline);
        StripUnsafeLinks(document);

        using var writer = new StringWriter();
        // Normaler HtmlRenderer – DisableHtml() in der Pipeline verhindert
        // Raw-HTML-Passthrough. EnableHtmlForBlock/Inline bleiben true, damit
        // echte Markdown-Blöcke und Inlines korrekt gerendert werden.
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        return new MarkupString(writer.ToString());
    }

    private static MarkdownPipeline BuildPipeline()
    {
        // DisableHtml() verhindert Raw-HTML-Passthrough (O-020).
        // Pipe-Tables, Grid-Tables, Listen-Extras und Task-Listen sind Standard-Features.
        return new MarkdownPipelineBuilder()
            .UseAutoIdentifiers()
            .UseEmphasisExtras()
            .UsePipeTables()
            .UseListExtras()
            .UseTaskLists()
            .DisableHtml()
            .Build();
    }

    /// <summary>
    /// Neutralisiert unsichere Links und externe Bilder gemäß O-020.
    /// Wird nach dem Parsen, vor dem Rendern durchgeführt.
    /// </summary>
    private static void StripUnsafeLinks(MarkdownDocument document)
    {
        foreach (var descendant in document.Descendants())
        {
            if (descendant is LinkInline link)
            {
                if (link.IsImage)
                {
                    // Externe Bilder: Quelle auf # setzen, sodass kein Netzwerkrequest
                    // ausgelöst wird. Gemäß O-020.
                    link.Url = "#";
                }
                else
                {
                    // Unsichere Link-Protokolle verweigern
                    var url = link.Url;
                    if (!string.IsNullOrEmpty(url) && IsUnsafeUrl(url))
                        link.Url = "#";
                }
            }
        }
    }

    private static bool IsUnsafeUrl(string url)
    {
        if (url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            return true;
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return true;
        if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return true;
        // Netzwerkpfade // (ohne Protokoll, nicht local #)
        if (url.StartsWith("//", StringComparison.Ordinal)
            && !url.StartsWith("///", StringComparison.Ordinal))
            return true;
        return false;
    }
}
