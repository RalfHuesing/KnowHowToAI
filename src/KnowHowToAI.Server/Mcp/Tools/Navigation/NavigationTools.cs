using System.ComponentModel;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Navigation;

/// <summary>
/// Dünne MCP-Handler der Navigation-Use-Cases: ausschließlich Mapping und Delegation
/// an den transportneutralen <see cref="NavigationService"/>.
/// </summary>
[McpServerToolType]
internal sealed class NavigationTools
{
    private readonly NavigationService _navigationService;
    private readonly RetrievalPolicy _retrievalPolicy;

    public NavigationTools(NavigationService navigationService, RetrievalPolicy retrievalPolicy)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _retrievalPolicy = retrievalPolicy ?? throw new ArgumentNullException(nameof(retrievalPolicy));
    }

    [McpServerTool(Name = "get_root", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Liefert den aktiven Root-Node des ausgewählten Wissensstandes mit aufgelöstem " +
        "Rollen-Content. Ein Snapshot ohne Root bleibt ein Erfolg ohne data.")]
    public async Task<McpToolEnvelope<McpNodeData>> GetRoot(
        [Description("Angefragte Rolle (roleId aus list_roles).")] string roleId,
        [Description("Optionaler Transaction-Selektor (Working Snapshot).")] string? transactionId = null,
        [Description("Optionaler Snapshot-Selektor (historischer Stand).")] string? snapshotId = null,
        [Description("Optional: gelöschte Fachobjekte einbeziehen (Standard false).")] bool? includeDeleted = null,
        CancellationToken cancellationToken = default)
    {
        var context = MapContext(transactionId, snapshotId, includeDeleted);
        if (!context.IsSuccess)
            return McpToolEnvelope<McpNodeData>.Failure(context.Error!);

        var result = await _navigationService
            .GetRootAsync(context.Value!, new AudienceId(roleId), cancellationToken)
            .ConfigureAwait(false);
        return McpNavigationMapper.ToEnvelope(result);
    }

    [McpServerTool(Name = "get_node", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Liefert eine einzelne Node mit aufgelöstem Rollen-Content, Availability und Freshness.")]
    public async Task<McpToolEnvelope<McpNodeData>> GetNode(
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Angefragte Rolle (roleId aus list_roles).")] string roleId,
        [Description("Optionaler Transaction-Selektor (Working Snapshot).")] string? transactionId = null,
        [Description("Optionaler Snapshot-Selektor (historischer Stand).")] string? snapshotId = null,
        [Description("Optional: gelöschte Fachobjekte einbeziehen (Standard false).")] bool? includeDeleted = null,
        CancellationToken cancellationToken = default)
    {
        var context = MapContext(transactionId, snapshotId, includeDeleted);
        if (!context.IsSuccess)
            return McpToolEnvelope<McpNodeData>.Failure(context.Error!);

        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedNodeId.IsSuccess)
            return McpToolEnvelope<McpNodeData>.Failure(parsedNodeId.Error!);

        var result = await _navigationService
            .GetNodeAsync(parsedNodeId.Value!.Value, context.Value!, new AudienceId(roleId), cancellationToken)
            .ConfigureAwait(false);
        return McpNavigationMapper.ToEnvelope(result);
    }

    [McpServerTool(Name = "list_children", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Liefert paginierte, deterministisch sortierte Kind-Nodes als Metadaten " +
        "(Metadata-First), ohne vollständigen Content.")]
    public async Task<McpToolEnvelope<McpChildrenPageData>> ListChildren(
        [Description("Angefragte Rolle (roleId aus list_roles).")] string roleId,
        [Description("Optionale Node-ID des Elternknotens (GUID-String); ohne Wert werden die " +
            "Kinder der Wurzelebene geliefert.")] string? parentNodeId = null,
        [Description("Optionaler Transaction-Selektor (Working Snapshot).")] string? transactionId = null,
        [Description("Optionaler Snapshot-Selektor (historischer Stand).")] string? snapshotId = null,
        [Description("Optional: gelöschte Fachobjekte einbeziehen (Standard false).")] bool? includeDeleted = null,
        [Description("Optionale Seitengröße; fehlend oder ≤ 0 ergibt die konfigurierte Standardseitengröße, " +
            "Werte oberhalb des Maximums werden geklemmt.")] int? limit = null,
        [Description("Optionaler opaker Folgecursor aus einer vorherigen Antwort.")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var context = MapContext(transactionId, snapshotId, includeDeleted);
        if (!context.IsSuccess)
            return McpToolEnvelope<McpChildrenPageData>.Failure(context.Error!);

        var parsedParentNodeId = McpNavigationMapper.ParseOptionalNodeId(parentNodeId);
        if (!parsedParentNodeId.IsSuccess)
            return McpToolEnvelope<McpChildrenPageData>.Failure(parsedParentNodeId.Error!);

        var query = new ListChildrenQuery(
            parsedParentNodeId.Value,
            context.Value!,
            new AudienceId(roleId),
            McpPagingMapper.NormalizeLimit(limit, _retrievalPolicy.DefaultPageSize, _retrievalPolicy.MaximumPageSize),
            cursor);
        var result = await _navigationService.ListChildrenAsync(query, cancellationToken).ConfigureAwait(false);
        return McpNavigationMapper.ToEnvelope(result);
    }

    [McpServerTool(Name = "list_roles", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Liefert paginierte, deterministisch nach roleId sortierte aktive Rollen.")]
    public async Task<McpToolEnvelope<McpRolePageData>> ListRoles(
        [Description("Optionaler Transaction-Selektor (Working Snapshot).")] string? transactionId = null,
        [Description("Optionaler Snapshot-Selektor (historischer Stand).")] string? snapshotId = null,
        [Description("Optional: gelöschte Rollen einbeziehen (Standard false).")] bool? includeDeleted = null,
        [Description("Optionale Seitengröße; fehlend oder ≤ 0 ergibt die konfigurierte Standardseitengröße, " +
            "Werte oberhalb des Maximums werden geklemmt.")] int? limit = null,
        [Description("Optionaler opaker Folgecursor aus einer vorherigen Antwort.")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var context = MapContext(transactionId, snapshotId, includeDeleted);
        if (!context.IsSuccess)
            return McpToolEnvelope<McpRolePageData>.Failure(context.Error!);

        var query = new ListAudiencesQuery(
            context.Value!,
            McpPagingMapper.NormalizeLimit(limit, _retrievalPolicy.DefaultPageSize, _retrievalPolicy.MaximumPageSize),
            cursor);
        var result = await _navigationService.ListAudiencesAsync(query, cancellationToken).ConfigureAwait(false);
        return McpNavigationMapper.ToEnvelope(result);
    }

    private static Result<ReadContext> MapContext(string? transactionId, string? snapshotId, bool? includeDeleted) =>
        McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(transactionId, snapshotId, includeDeleted));
}
