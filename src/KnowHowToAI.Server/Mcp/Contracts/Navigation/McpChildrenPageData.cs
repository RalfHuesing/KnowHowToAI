using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// Paginiertes list_children-Ergebnis im MCP-Vertrag. Der <c>nextCursor</c> bleibt
/// opak und wird unverändert als Folgeparameter weitergereicht.
/// </summary>
public sealed record McpChildrenPageData(
    [property: JsonPropertyName("parentNodeId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ParentNodeId,
    [property: JsonPropertyName("items")] IReadOnlyList<McpChildNodeData> Items,
    [property: JsonPropertyName("nextCursor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor = null);
