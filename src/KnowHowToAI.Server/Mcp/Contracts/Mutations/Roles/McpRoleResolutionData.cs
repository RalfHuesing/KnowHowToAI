using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Mutations.Roles;

/// <summary>
/// Ergebnis von set_role_resolution im MCP-Vertrag: die vollständige, nicht rekursive
/// Resolution Order der angefragten Rolle nach der Mutation
/// (verbindlich: docs/konzept/05-MCP-API.md, Abschnitt 64).
/// </summary>
public sealed record McpRoleResolutionData(
    [property: JsonPropertyName("requestedRoleId")] string RequestedRoleId,
    [property: JsonPropertyName("items")] IReadOnlyList<McpRoleResolutionItemData> Items);

/// <summary>Ein expliziter Kandidat der Resolution Order mit seiner 1-basierten Priorität.</summary>
public sealed record McpRoleResolutionItemData(
    [property: JsonPropertyName("candidateRoleId")] string CandidateRoleId,
    [property: JsonPropertyName("priority")] int Priority);
