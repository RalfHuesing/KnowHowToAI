using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// Paginiertes search-Ergebnis im MCP-Vertrag. Der <c>nextCursor</c> bleibt opak
/// und wird unverändert als Folgeparameter weitergereicht.
/// </summary>
public sealed record McpSearchPageData(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("items")] IReadOnlyList<McpSearchHitData> Items,
    [property: JsonPropertyName("nextCursor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor = null);
