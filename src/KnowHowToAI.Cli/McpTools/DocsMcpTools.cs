using System.ComponentModel;
using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Logging;
using KnowHowToAI.Core.Sync;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Cli.McpTools;

// Die drei MCP-Tools des Servers. SQL-Details: docs/02-Architektur-und-Techstack.md, Abschnitt 4.D.
[McpServerToolType]
public sealed class DocsMcpTools(SqlDocumentsStore store, int maxQueryLength, int maxResults, ILogger<DocsMcpTools> logger)
{
    [McpServerTool(Name = "list_children"), Description("""
        Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn
        parentSlug weggelassen oder null ist). Sortierung: alphabetisch nach Slug.

        Edge Cases:
        - parentSlug = null oder weggelassen: listet die Root-Dokumente
        - parentSlug = "" (leerer String): wirft ArgumentException — nicht dasselbe wie null
        - parentSlug existiert nicht als Dokument: leere Liste, kein Fehler
        - parentSlug ist kein gültiger Slug (z.B. "Foo Bar"): wird vom Server akzeptiert
          und liefert eine leere Liste

        Beispiel:
        - list_children() → DocumentSummary[] der Root-Dokumente
        - list_children(parentSlug="it") → DocumentSummary[] der direkten Kinder von "it"
        - list_children(parentSlug="gibt-es-nicht") → []

        Es gibt keine Cap; bei sehr breiten Verzeichnissen sind ggf. >100 Treffer möglich.
        """)]
    public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
    {
        logger.LogInformation("list_children(parentSlug={ParentSlug})", parentSlug);
        var result = await store.ListChildrenAsync(parentSlug, cancellationToken);
        logger.LogInformation("list_children response: {Size}", ResponseSize.Measure(result));
        return result;
    }

    [McpServerTool(Name = "search_docs"), Description("""
        Durchsucht Titel, Inhalt, Tags und Synonyme nach einem Suchbegriff
        (Substring-Match). Liefert die Treffer als SearchResult.

        Response-Shape:
        - { results: DocumentSummary[], truncated: bool }
        - results: Slug + Title der gefundenen Dokumente
        - truncated: true, wenn es mehr Treffer gibt als MaxResults (Default 50,
          konfigurierbar via appsettings.json → KnowHowToAi.Search.MaxResults).
          In dem Fall die Suche verfeinern (präziserer Query) statt alle Treffer
          zu erwarten.

        Semantik:
        - SQL LIKE '%query%' gegen title, content, tags und synonyms
        - Wildcard-Zeichen (% _ [) im Query werden literal behandelt (Bracket-Escape),
          kein Wildcard-Smuggling
        - Sortierung: Title-Treffer zuerst, dann alphabetisch nach title

        Edge Cases:
        - query = null/leer/nur Whitespace: leere results, truncated=false
        - query länger als MaxQueryLength (Default 200, konfigurierbar via
          appsettings.json → KnowHowToAi.Search.MaxQueryLength): Tool-Error
        - Keine Treffer: leere results, truncated=false
        - Viele Treffer (> MaxResults): truncated=true in der Antwort
        """)]
    public async Task<SearchResult> SearchDocsAsync(string query, CancellationToken cancellationToken)
    {
        logger.LogInformation("search_docs(query={Query})", query);
        var result = await store.SearchDocsAsync(query, maxQueryLength, maxResults, cancellationToken);
        logger.LogInformation("search_docs response: {Size}", ResponseSize.Measure(result));
        return result;
    }

    [McpServerTool(Name = "get_doc"), Description("""
        Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs.

        Edge Cases:
        - slug existiert nicht: liefert null (kein Tool-Error) — das LLM hat dann
          den falschen Slug und sollte list_children oder search_docs erneut aufrufen
        - Inhalt ist NVARCHAR(MAX) ohne Trunkierung: bei sehr großen Dokumenten
          kann der Content das Token-Budget des LLM sprengen — ggf. das Dokument
          in mehrere kleinere Slugs aufteilen
        - YAML-Front-Matter ist nicht Teil des Contents (wurde beim Import entfernt)

        Beispiel:
        - get_doc(slug="it/netzwerk/routing") → DocumentDetail mit title und content
        - get_doc(slug="unbekannt") → null
        """)]
    public async Task<DocumentDetail?> GetDocAsync(string slug, CancellationToken cancellationToken)
    {
        logger.LogInformation("get_doc(slug={Slug})", slug);
        var result = await store.GetDocAsync(slug, cancellationToken);
        logger.LogInformation("get_doc response: {Size}", ResponseSize.Measure(result));
        return result;
    }
}
