using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>Rolle im MCP-Vertrag (list_roles).</summary>
public sealed record McpRoleData(
    [property: JsonPropertyName("roleId")] string RoleId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description);
