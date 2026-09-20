using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>Rolle im MCP-Vertrag (list_roles).</summary>
public sealed record McpRoleData(
    [property: JsonPropertyName("roleId")] string RoleId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    [property: JsonPropertyName("snapshotId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SnapshotId = null,
    [property: JsonPropertyName("changeVersion")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? ChangeVersion = null);
