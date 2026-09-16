using System.ComponentModel;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Retrieval;

/// <summary>
/// Dünne MCP-Handler der Retrieval-Use-Cases (search, export_tree): ausschließlich
/// Mapping und Delegation an die transportneutralen Services (verbindlich:
/// docs/konzept/05-MCP-API.md, Abschnitte 61 und 64).
/// </summary>
[McpServerToolType]
internal sealed class RetrievalTools
{
    private readonly SearchService _searchService;
    private readonly MarkdownExportService _exportService;
    private readonly RetrievalPolicy _retrievalPolicy;

    public RetrievalTools(
        SearchService searchService,
        MarkdownExportService exportService,
        RetrievalPolicy retrievalPolicy)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _retrievalPolicy = retrievalPolicy ?? throw new ArgumentNullException(nameof(retrievalPolicy));
    }

    [McpServerTool(Name = "search", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Deterministische Substring-Suche über Titel, Description und rollenaufgelösten " +
        "Content mit schlanken Treffermetadaten und Snippets (Metadata-First).")]
    public async Task<McpToolEnvelope<McpSearchPageData>> Search(
        [Description("Suchtext (exakte Substring-Suche).")] string text,
        [Description("Optionale angefragte Rolle; ohne Rolle werden nur Title und Description durchsucht.")]
        string? roleId = null,
        [Description("Optionaler Transaction-Selektor (Working Snapshot).")] string? transactionId = null,
        [Description("Optionaler Snapshot-Selektor (historischer Stand).")] string? snapshotId = null,
        [Description("Optional: gelöschte Fachobjekte einbeziehen (Standard false).")] bool? includeDeleted = null,
        [Description("Optionale Seitengröße; fehlend oder ≤ 0 ergibt die konfigurierte Standardseitengröße, " +
            "Werte oberhalb des Maximums werden geklemmt.")] int? limit = null,
        [Description("Optionaler opaker Folgecursor aus einer vorherigen Antwort.")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var context = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(transactionId, snapshotId, includeDeleted));
        if (!context.IsSuccess)
            return McpToolEnvelope<McpSearchPageData>.Failure(context.Error!);

        var query = new SearchQuery(
            text,
            McpPagingMapper.NormalizeLimit(
                limit, _retrievalPolicy.SearchPageSize, _retrievalPolicy.SearchMaximumPageSize),
            cursor,
            roleId is null ? null : new RoleId(roleId));
        var result = await _searchService.SearchAsync(query, context.Value!, cancellationToken).ConfigureAwait(false);
        return McpRetrievalMapper.ToEnvelope(result);
    }

    [McpServerTool(Name = "export_tree", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Exportiert den Teilbaum ab einer Node als Markdown. Der ausgewählte Root ist " +
        "Heading-Level 1; Qualitätswarnungen bleiben auf Envelope-Ebene sichtbar.")]
    public async Task<McpToolEnvelope<McpExportTreeData>> ExportTree(
        [Description("Node-ID des Export-Roots (GUID-String).")] string rootNodeId,
        [Description("Angefragte Rolle (roleId aus list_roles).")] string roleId,
        [Description("Optionaler Transaction-Selektor (Working Snapshot).")] string? transactionId = null,
        [Description("Optionaler Snapshot-Selektor (historischer Stand).")] string? snapshotId = null,
        [Description("Optional: gelöschte Fachobjekte einbeziehen (Standard false).")] bool? includeDeleted = null,
        CancellationToken cancellationToken = default)
    {
        var context = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(transactionId, snapshotId, includeDeleted));
        if (!context.IsSuccess)
            return McpToolEnvelope<McpExportTreeData>.Failure(context.Error!);

        var parsedRootNodeId = McpNavigationMapper.ParseOptionalNodeId(rootNodeId);
        if (!parsedRootNodeId.IsSuccess)
            return McpToolEnvelope<McpExportTreeData>.Failure(parsedRootNodeId.Error!);

        var result = await _exportService
            .ExportTreeAsync(parsedRootNodeId.Value!.Value, context.Value!, new RoleId(roleId), cancellationToken)
            .ConfigureAwait(false);
        return McpRetrievalMapper.ToExportEnvelope(result);
    }
}
