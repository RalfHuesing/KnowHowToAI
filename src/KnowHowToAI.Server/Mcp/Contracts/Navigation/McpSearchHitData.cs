using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// Einzelner Suchtreffer im MCP-Vertrag (search). Liefert schlanke Metadaten und
/// einen kleinen Trefferkontext, niemals automatisch den vollständigen Content.
/// </summary>
public sealed record McpSearchHitData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    [property: JsonPropertyName("snippet")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Snippet,
    [property: JsonPropertyName("hitField")] string HitField,
    [property: JsonPropertyName("availability")] string Availability,
    [property: JsonPropertyName("resolvedAudienceId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ResolvedAudienceId,
    [property: JsonPropertyName("freshness")] string Freshness);
