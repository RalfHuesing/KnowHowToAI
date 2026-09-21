using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>Zielgruppe im MCP-Vertrag (list_audiences).</summary>
public sealed record McpAudienceData(
    [property: JsonPropertyName("audienceId")] string AudienceId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    [property: JsonPropertyName("snapshotId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SnapshotId = null,
    [property: JsonPropertyName("changeVersion")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? ChangeVersion = null);
