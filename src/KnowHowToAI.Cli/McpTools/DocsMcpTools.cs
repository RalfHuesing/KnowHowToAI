using System.ComponentModel;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Cli.McpTools;

// Die drei MCP-Tools des Servers. SQL-Details: docs/02-Architektur-und-Techstack.md, Abschnitt 4.D.
[McpServerToolType]
public sealed class DocsMcpTools
{
    [McpServerTool(Name = "list_children"), Description("Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug leer ist).")]
    public Task<string> ListChildrenAsync(string? parentSlug = null)
    {
        throw new NotImplementedException();
    }

    [McpServerTool(Name = "search_docs"), Description("Durchsucht Titel, Inhalt, Tags und Synonyme per SQL Server Full-Text Search.")]
    public Task<string> SearchDocsAsync(string query)
    {
        throw new NotImplementedException();
    }

    [McpServerTool(Name = "get_doc"), Description("Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs.")]
    public Task<string> GetDocAsync(string slug)
    {
        throw new NotImplementedException();
    }
}
