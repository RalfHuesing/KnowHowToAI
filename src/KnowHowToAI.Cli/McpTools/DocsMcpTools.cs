using System.ComponentModel;
using System.Text.Json;
using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Sync;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Cli.McpTools;

// Die drei MCP-Tools des Servers. SQL-Details: docs/02-Architektur-und-Techstack.md, Abschnitt 4.D.
// Jeder Aufruf loggt Tool-Name + Parameter sowie die Größe der Antwort in Bytes (nicht deren
// Inhalt) — Sichtbarkeit für den Betreiber ohne SQL Profiler, siehe docs/02, Abschnitt 4.D.
[McpServerToolType]
public sealed class DocsMcpTools(SqlDocumentsStore store, ILogger<DocsMcpTools> logger)
{
    [McpServerTool(Name = "list_children"), Description("Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug leer ist).")]
    public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
    {
        logger.LogInformation("list_children(parentSlug={ParentSlug})", parentSlug);
        var result = await store.ListChildrenAsync(parentSlug, cancellationToken);
        LogResponseSize("list_children", result);
        return result;
    }

    [McpServerTool(Name = "search_docs"), Description("Durchsucht Titel, Inhalt, Tags und Synonyme nach einem Suchbegriff.")]
    public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(string query, CancellationToken cancellationToken)
    {
        logger.LogInformation("search_docs(query={Query})", query);
        var result = await store.SearchDocsAsync(query, cancellationToken);
        LogResponseSize("search_docs", result);
        return result;
    }

    [McpServerTool(Name = "get_doc"), Description("Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs.")]
    public async Task<DocumentDetail?> GetDocAsync(string slug, CancellationToken cancellationToken)
    {
        logger.LogInformation("get_doc(slug={Slug})", slug);
        var result = await store.GetDocAsync(slug, cancellationToken);
        LogResponseSize("get_doc", result);
        return result;
    }

    private void LogResponseSize<T>(string toolName, T response) =>
        logger.LogInformation("{ToolName} response: {ByteCount} bytes", toolName, JsonSerializer.SerializeToUtf8Bytes(response).Length);
}
