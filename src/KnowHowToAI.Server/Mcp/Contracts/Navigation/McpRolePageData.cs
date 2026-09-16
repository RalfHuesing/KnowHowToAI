using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// Paginiertes list_roles-Ergebnis im MCP-Vertrag. Der <c>nextCursor</c> bleibt
/// opak und wird unverändert als Folgeparameter weitergereicht.
/// </summary>
public sealed record McpRolePageData(
    [property: JsonPropertyName("items")] IReadOnlyList<McpRoleData> Items,
    [property: JsonPropertyName("nextCursor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor = null);
