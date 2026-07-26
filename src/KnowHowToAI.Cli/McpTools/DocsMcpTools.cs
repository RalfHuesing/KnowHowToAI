using System.ComponentModel;
using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Logging;
using KnowHowToAI.Core.Sync;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Cli.McpTools;

// Die drei MCP-Tools des Servers. SQL-Details: docs/02-Architektur-und-Techstack.md, Abschnitt 4.D.
[McpServerToolType]
public sealed class DocsMcpTools(SqlDocumentsStore store, int maxQueryLength, ILogger<DocsMcpTools> logger)
{
    [McpServerTool(Name = "list_children"), Description("Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug leer ist).")]
    public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
    {
        logger.LogInformation("list_children(parentSlug={ParentSlug})", parentSlug);
        var result = await store.ListChildrenAsync(parentSlug, cancellationToken);
        logger.LogInformation("list_children response: {Size}", ResponseSize.Measure(result));
        return result;
    }

    [McpServerTool(Name = "search_docs"), Description("Durchsucht Titel, Inhalt, Tags und Synonyme nach einem Suchbegriff.")]
    public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(string query, CancellationToken cancellationToken)
    {
        logger.LogInformation("search_docs(query={Query})", query);
        var result = await store.SearchDocsAsync(query, maxQueryLength, cancellationToken);
        logger.LogInformation("search_docs response: {Size}", ResponseSize.Measure(result));
        return result;
    }

    [McpServerTool(Name = "get_doc"), Description("Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs.")]
    public async Task<DocumentDetail?> GetDocAsync(string slug, CancellationToken cancellationToken)
    {
        logger.LogInformation("get_doc(slug={Slug})", slug);
        var result = await store.GetDocAsync(slug, cancellationToken);
        logger.LogInformation("get_doc response: {Size}", ResponseSize.Measure(result));
        return result;
    }
}
