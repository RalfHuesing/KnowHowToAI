using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Mutations.Content;

/// <summary>
/// Bezeichnet eine beim Ableiten verwendete explizite Source-Revision als
/// Tool-Argument und Antwortfeld (verbindlich: docs/konzept/05-MCP-API.md, Abschnitt 64).
/// </summary>
public sealed record McpContentSourceData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("roleId")] string RoleId,
    [property: JsonPropertyName("contentRevisionId")] string ContentRevisionId);
