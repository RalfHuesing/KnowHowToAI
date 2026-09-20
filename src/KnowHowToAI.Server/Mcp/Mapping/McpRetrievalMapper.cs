using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Bildet die transportneutralen Ergebnisse der Retrieval-Use-Cases (search,
/// export_tree) auf den gemeinsamen MCP-Antwort-Envelope und die Retrieval-DTOs ab.
/// Enthält keine Fachlogik.
/// </summary>
internal static class McpRetrievalMapper
{
    public static McpToolEnvelope<McpSearchPageData> ToEnvelope(Result<SearchResultPage> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpSearchPageData>.Success(ToSearchData(result.Value!), MapWarnings(result))
            : McpToolEnvelope<McpSearchPageData>.Failure(result.Error!, MapWarnings(result));

    public static McpToolEnvelope<McpExportTreeData> ToExportEnvelope(Result<string> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpExportTreeData>.Success(
                new McpExportTreeData(result.Value!),
                MapWarnings(result))
            : McpToolEnvelope<McpExportTreeData>.Failure(result.Error!, MapWarnings(result));

    private static McpSearchPageData ToSearchData(SearchResultPage page) => new(
        page.Query,
        page.Items.Select(static hit => new McpSearchHitData(
            hit.NodeId.ToString(),
            hit.Title,
            hit.Description,
            hit.Snippet,
            hit.HitField,
            hit.Availability.ToString(),
            hit.ResolvedAudienceId?.ToString(),
            hit.Freshness.ToString())).ToArray(),
        page.NextCursor);

    private static IReadOnlyList<McpWarning> MapWarnings<T>(Result<T> result) =>
        result.Warnings.Select(McpResultMapper.ToWarning).ToArray();
}
