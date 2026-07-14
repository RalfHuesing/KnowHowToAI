using System.ComponentModel;
using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Sync;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Cli.McpTools;

// Die drei MCP-Tools des Servers. SQL-Details: docs/02-Architektur-und-Techstack.md, Abschnitt 4.D.
[McpServerToolType]
public sealed class DocsMcpTools(SqlDocumentsStore store)
{
    [McpServerTool(Name = "list_children"), Description("Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug leer ist).")]
    public Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken) =>
        store.ListChildrenAsync(parentSlug, cancellationToken);

    [McpServerTool(Name = "search_docs"), Description("Durchsucht Titel, Inhalt, Tags und Synonyme nach einem Suchbegriff.")]
    public Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(string query, CancellationToken cancellationToken) =>
        store.SearchDocsAsync(query, cancellationToken);

    [McpServerTool(Name = "get_doc"), Description("Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs.")]
    public Task<DocumentDetail?> GetDocAsync(string slug, CancellationToken cancellationToken) =>
        store.GetDocAsync(slug, cancellationToken);
}
