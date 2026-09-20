using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Content;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Nodes;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Roles;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Bildet die transportneutralen Ergebnisse der Node-, Content- und Rollen-Mutationen
/// auf den gemeinsamen MCP-Antwort-Envelope und die Mutations-DTOs ab.
/// Enthält keine Fachlogik.
/// </summary>
internal static class McpMutationMapper
{
    public static McpToolEnvelope<McpNodeMutationData> ToEnvelope(Result<NodeMutationResult> result)
    {
        var warnings = MapWarnings(result);
        return result.IsSuccess
            ? McpToolEnvelope<McpNodeMutationData>.Success(ToData(result.Value!), warnings)
            : McpToolEnvelope<McpNodeMutationData>.Failure(result.Error!, warnings);
    }

    public static McpToolEnvelope<McpContentMutationData> ToEnvelope(Result<ContentMutationUseCaseResult> result)
    {
        var warnings = MapWarnings(result);
        return result.IsSuccess
            ? McpToolEnvelope<McpContentMutationData>.Success(ToData(result.Value!), warnings)
            : McpToolEnvelope<McpContentMutationData>.Failure(result.Error!, warnings);
    }

    /// <summary>
    /// Führt das Node-Mutationsergebnis und das Content-Mutationsergebnis des
    /// kombinierten create_node-Aufrufs zu einem Envelope zusammen; Snapshot und
    /// ChangeVersion stammen aus dem späteren Content-Schritt, die Warnungen
    /// sind die Vereinigung beider Ergebnisse.
    /// </summary>
    public static McpToolEnvelope<McpNodeMutationData> ToMergedEnvelope(
        Result<NodeMutationResult> node,
        Result<ContentMutationUseCaseResult> content)
    {
        var warnings = MapWarnings(node).Concat(MapWarnings(content)).ToArray();
        return content.IsSuccess
            ? McpToolEnvelope<McpNodeMutationData>.Success(ToMergedData(node.Value!, content.Value!), warnings)
            : McpToolEnvelope<McpNodeMutationData>.Failure(content.Error!, warnings);
    }

    public static McpToolEnvelope<McpRoleData> ToRoleMutationEnvelope(Result<RoleMutationResult> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpRoleData>.Success(ToRoleData(result.Value!))
            : McpToolEnvelope<McpRoleData>.Failure(result.Error!, MapWarnings(result));

    public static McpToolEnvelope<McpRoleResolutionData> ToRoleResolutionMutationEnvelope(
        Result<RoleResolutionMutationResult> result)
    {
        var warnings = MapWarnings(result);
        return result.IsSuccess
            ? McpToolEnvelope<McpRoleResolutionData>.Success(ToResolutionData(result.Value!), warnings)
            : McpToolEnvelope<McpRoleResolutionData>.Failure(result.Error!, warnings);
    }

    /// <summary>
    /// Parst das ContentMode-Argument exakt im Enum-Namen-Format. Ein unbekannter Wert
    /// ist eine harte Dependency-Verletzung und führt zu <c>InvalidDependency</c>.
    /// </summary>
    public static Result<ContentMode> ParseContentMode(string? contentMode)
    {
        if (string.Equals(contentMode, nameof(ContentMode.Independent), StringComparison.Ordinal))
            return Result<ContentMode>.Success(ContentMode.Independent);
        if (string.Equals(contentMode, nameof(ContentMode.Derived), StringComparison.Ordinal))
            return Result<ContentMode>.Success(ContentMode.Derived);

        return Result<ContentMode>.Failure(new DomainError(
            DependencyErrorCodes.InvalidDependency,
            "Das Argument 'contentMode' muss exakt 'Independent' oder 'Derived' sein.",
            new Dictionary<string, string> { ["contentMode"] = contentMode ?? string.Empty }));
    }

    /// <summary>
    /// Mappt die Source-Revisionen des replace_content-Arguments auf den
    /// transportneutralen Vertrag. IDs werden exakt im Format der Tool-Ausgaben
    /// erwartet; ein nicht parsebarer Wert kann keine existierende Revision bezeichnen
    /// und führt zu <c>InvalidDependency</c> mit dem Rohwert in den Details.
    /// </summary>
    public static Result<IReadOnlyList<ContentDependencySource>> ParseSources(McpContentSourceData[]? sources)
    {
        if (sources is null || sources.Length == 0)
            return Result<IReadOnlyList<ContentDependencySource>>.Success([]);

        var mappedSources = new List<ContentDependencySource>(sources.Length);
        foreach (var source in sources)
        {
            var parsed = ParseSource(source);
            if (!parsed.IsSuccess)
                return Result<IReadOnlyList<ContentDependencySource>>.Failure(parsed.Error!);

            mappedSources.Add(parsed.Value!);
        }

        return Result<IReadOnlyList<ContentDependencySource>>.Success(
            Array.AsReadOnly<ContentDependencySource>(mappedSources.ToArray()));
    }

    private static Result<ContentDependencySource> ParseSource(McpContentSourceData source)
    {
        if (string.IsNullOrWhiteSpace(source.RoleId))
        {
            return Result<ContentDependencySource>.Failure(CreateInvalidSourceError(
                DependencyErrorCodes.SourceRoleIdDetail, source.RoleId ?? string.Empty));
        }

        if (!Guid.TryParseExact(source.NodeId, "D", out var sourceNodeId))
        {
            return Result<ContentDependencySource>.Failure(CreateInvalidSourceError(
                DependencyErrorCodes.SourceNodeIdDetail, source.NodeId ?? string.Empty));
        }

        if (!Guid.TryParseExact(source.ContentRevisionId, "D", out var revisionId))
        {
            return Result<ContentDependencySource>.Failure(CreateInvalidSourceError(
                "sourceContentRevisionId", source.ContentRevisionId ?? string.Empty));
        }

        return Result<ContentDependencySource>.Success(new ContentDependencySource(
            new NodeId(sourceNodeId),
            new RoleId(source.RoleId),
            new ContentRevisionId(revisionId)));
    }

    private static DomainError CreateInvalidSourceError(string detailName, string rawValue) => new(
        DependencyErrorCodes.InvalidDependency,
        "Eine Source-Revision des Arguments 'sources' ist kein gültiger Verweis.",
        new Dictionary<string, string> { [detailName] = rawValue });

    private static McpNodeMutationData ToData(NodeMutationResult result) => new(
        result.Node.NodeId.ToString(),
        result.Node.ParentNodeId?.ToString(),
        result.Node.Title,
        result.SnapshotId.ToString(),
        result.ChangeVersion,
        result.AffectedNodeIds.Select(static nodeId => nodeId.ToString()).ToArray());

    private static McpNodeMutationData ToMergedData(NodeMutationResult node, ContentMutationUseCaseResult content) => new(
        node.Node.NodeId.ToString(),
        node.Node.ParentNodeId?.ToString(),
        node.Node.Title,
        content.SnapshotId.ToString(),
        content.ChangeVersion,
        node.AffectedNodeIds.Select(static nodeId => nodeId.ToString()).ToArray(),
        content.Content.RoleId.ToString(),
        content.Content.ContentRevisionId.ToString(),
        content.Content.ContentMode.ToString(),
        content.Freshness.ToString());

    private static McpContentMutationData ToData(ContentMutationUseCaseResult result) => new(
        result.Content.NodeId.ToString(),
        result.Content.RoleId.ToString(),
        result.Content.ContentRevisionId.ToString(),
        result.Content.ContentMode.ToString(),
        result.Freshness.ToString(),
        result.SnapshotId.ToString(),
        result.ChangeVersion);

    private static McpRoleData ToRoleData(RoleMutationResult result) => new(
        result.Role.RoleId.ToString(),
        result.Role.Name,
        result.Role.Description,
        result.SnapshotId.ToString(),
        result.ChangeVersion);

    private static McpRoleResolutionData ToResolutionData(RoleResolutionMutationResult result) => new(
        result.RequestedRoleId.ToString(),
        result.Resolutions.Select(static resolution => new McpRoleResolutionItemData(
            resolution.CandidateRoleId.ToString(),
            resolution.Priority)).ToArray(),
        result.SnapshotId.ToString(),
        result.ChangeVersion);

    private static IReadOnlyList<McpWarning> MapWarnings<T>(Result<T> result) =>
        result.Warnings.Select(McpResultMapper.ToWarning).ToArray();
}
