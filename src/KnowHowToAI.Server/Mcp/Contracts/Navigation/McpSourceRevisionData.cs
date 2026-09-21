using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>Metadata-first provenance of one source revision for derived content.</summary>
public sealed record McpSourceRevisionData(
    [property: JsonPropertyName("sourceNodeId")] string SourceNodeId,
    [property: JsonPropertyName("sourceAudienceId")] string SourceAudienceId,
    [property: JsonPropertyName("sourceContentRevisionId")] string SourceContentRevisionId,
    [property: JsonPropertyName("freshness")] string Freshness);
