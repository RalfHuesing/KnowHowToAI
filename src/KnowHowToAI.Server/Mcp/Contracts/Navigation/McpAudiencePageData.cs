using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// Paginiertes list_audiences-Ergebnis im MCP-Vertrag. Der <c>nextCursor</c> bleibt
/// opak und wird unverändert als Folgeparameter weitergereicht.
/// </summary>
public sealed record McpAudiencePageData(
    [property: JsonPropertyName("items")] IReadOnlyList<McpAudienceData> Items,
    [property: JsonPropertyName("nextCursor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor = null);
