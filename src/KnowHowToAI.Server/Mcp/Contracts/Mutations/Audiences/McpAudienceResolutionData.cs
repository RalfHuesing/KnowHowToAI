using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Mutations.Audiences;

/// <summary>
/// Ergebnis von set_audience_resolution im MCP-Vertrag: die vollständige, nicht rekursive
/// Resolution Order der angefragten Zielgruppe nach der Mutation.
/// </summary>
public sealed record McpAudienceResolutionData(
    [property: JsonPropertyName("requestedAudienceId")] string RequestedAudienceId,
    [property: JsonPropertyName("items")] IReadOnlyList<McpAudienceResolutionItemData> Items,
    [property: JsonPropertyName("snapshotId")] string? SnapshotId = null,
    [property: JsonPropertyName("changeVersion")] long? ChangeVersion = null);

/// <summary>Ein expliziter Kandidat der Resolution Order mit seiner 1-basierten Priorität.</summary>
public sealed record McpAudienceResolutionItemData(
    [property: JsonPropertyName("candidateAudienceId")] string CandidateAudienceId,
    [property: JsonPropertyName("priority")] int Priority);
