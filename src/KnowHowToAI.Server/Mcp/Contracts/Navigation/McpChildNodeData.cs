using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// Metadaten-Zusammenfassung einer Kind-Node im MCP-Vertrag (list_children).
/// Enthält bewusst keinen vollständigen Content (Metadata-First-Prinzip).
/// </summary>
public sealed record McpChildNodeData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("childCount")] int ChildCount,
    [property: JsonPropertyName("contentSizeBytes")] int ContentSizeBytes,
    [property: JsonPropertyName("availability")] string Availability,
    [property: JsonPropertyName("resolvedRole")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ResolvedRole,
    [property: JsonPropertyName("freshness")] string Freshness);
