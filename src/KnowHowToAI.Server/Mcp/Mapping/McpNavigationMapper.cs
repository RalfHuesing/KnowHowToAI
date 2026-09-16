using System.Runtime.CompilerServices;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Bildet die transportneutralen Navigationsergebnisse auf den gemeinsamen
/// MCP-Antwort-Envelope und die Navigation-DTOs ab. Enthält keine Fachlogik.
/// </summary>
internal static class McpNavigationMapper
{
    /// <summary>
    /// Mappt get_root/get_node. Ein leerer Snapshot ohne Root bleibt ein Erfolg
    /// ohne <c>data</c>; Warnungen bleiben auf Envelope-Ebene sichtbar.
    /// </summary>
    public static McpToolEnvelope<McpNodeData> ToEnvelope(Result<NodeWithContent> result)
    {
        var warnings = result.Warnings.Select(McpResultMapper.ToWarning).ToArray();
        if (!result.IsSuccess)
            return McpToolEnvelope<McpNodeData>.Failure(result.Error!, warnings);

        return result.Value!.Node is null
            ? McpToolEnvelope<McpNodeData>.Success(warnings: warnings)
            : McpToolEnvelope<McpNodeData>.Success(ToNodeData(result.Value), warnings);
    }

    public static McpToolEnvelope<McpChildrenPageData> ToEnvelope(Result<ChildrenPage> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpChildrenPageData>.Success(ToChildrenData(result.Value!))
            : McpToolEnvelope<McpChildrenPageData>.Failure(result.Error!, MapWarnings(result));

    public static McpToolEnvelope<McpRolePageData> ToEnvelope(Result<RolePage> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpRolePageData>.Success(ToRolePageData(result.Value!))
            : McpToolEnvelope<McpRolePageData>.Failure(result.Error!, MapWarnings(result));

    /// <summary>
    /// Parst einen Node-ID-String exakt im Format der Tool-Ausgaben (GUID "D").
    /// Ein nicht parsebarer Wert ist ein Parameterfehler und führt daher zu
    /// <c>InvalidNodeId</c> mit Parametername und Rohwert in den Details.
    /// </summary>
    public static Result<NodeId?> ParseOptionalNodeId(
        string? nodeId,
        [CallerArgumentExpression(nameof(nodeId))] string? parameterName = null)
    {
        if (nodeId is null)
            return Result<NodeId?>.Success(null);

        if (!Guid.TryParseExact(nodeId, "D", out var parsed))
            return Result<NodeId?>.Failure(CreateInvalidNodeId(nodeId, parameterName));

        return Result<NodeId?>.Success(new NodeId(parsed));
    }

    private static McpNodeData ToNodeData(NodeWithContent node) => new(
        node.Node!.NodeId.ToString(),
        node.Node.Title,
        node.Node.Description,
        node.Node.SortOrder,
        node.RequestedRoleId.ToString(),
        node.ResolvedRoleId?.ToString(),
        node.FallbackUsed,
        node.Availability.ToString(),
        node.Freshness.ToString(),
        node.Content?.ContentRevisionId.ToString(),
        node.Content?.ContentMd);

    private static McpChildrenPageData ToChildrenData(ChildrenPage page) => new(
        page.ParentNodeId?.ToString(),
        page.Items.Select(static item => new McpChildNodeData(
            item.NodeId.ToString(),
            item.Title,
            item.Description,
            item.SortOrder,
            item.ChildCount,
            item.ContentSizeBytes,
            item.Availability.ToString(),
            item.ResolvedRoleId?.ToString(),
            item.Freshness.ToString())).ToArray(),
        page.NextCursor);

    private static McpRolePageData ToRolePageData(RolePage page) => new(
        page.Items.Select(static role => new McpRoleData(
            role.RoleId.ToString(),
            role.Name,
            role.Description)).ToArray(),
        page.NextCursor);

    private static DomainError CreateInvalidNodeId(string rawValue, string? parameterName)
    {
        var detailName = string.IsNullOrWhiteSpace(parameterName)
            ? NavigationErrorCodes.NodeIdDetail
            : parameterName;
        return new DomainError(
            NavigationErrorCodes.InvalidNodeId,
            $"Der Wert des Parameters '{detailName}' ist keine gültige Node-ID (GUID im Format der Tool-Ausgaben).",
            new Dictionary<string, string> { [detailName] = rawValue });
    }

    private static IReadOnlyList<McpWarning> MapWarnings<T>(Result<T> result) =>
        result.Warnings.Select(McpResultMapper.ToWarning).ToArray();
}
