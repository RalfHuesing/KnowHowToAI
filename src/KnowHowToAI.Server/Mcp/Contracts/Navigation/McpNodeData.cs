using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// Node mit aufgelöstem Zielgruppen-Content im MCP-Vertrag (get_root, get_node).
/// Alle IDs sind Strings im Format der Tool-Ausgaben und ohne Umformatierung
/// als Folgeparameter verwendbar.
/// </summary>
public sealed record McpNodeData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("requestedAudienceId")] string RequestedAudienceId,
    [property: JsonPropertyName("resolvedAudienceId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ResolvedAudienceId,
    [property: JsonPropertyName("fallbackUsed")] bool FallbackUsed,
    [property: JsonPropertyName("availability")] string Availability,
    [property: JsonPropertyName("freshness")] string Freshness,
    [property: JsonPropertyName("contentRevisionId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContentRevisionId = null,
    [property: JsonPropertyName("content")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Content = null,
    [property: JsonPropertyName("sourceRevisions")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<McpSourceRevisionData>? SourceRevisions = null);
