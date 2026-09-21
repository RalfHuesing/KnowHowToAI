using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Mutations.Content;

/// <summary>
/// Ergebnis einer Content-Mutation im MCP-Vertrag: der gespeicherte Zielgruppen-Content
/// samt Revisions- und Freshness-Metadaten, ohne Rückgabe des Content-Textes.
/// </summary>
public sealed record McpContentMutationData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("audienceId")] string AudienceId,
    [property: JsonPropertyName("contentRevisionId")] string ContentRevisionId,
    [property: JsonPropertyName("contentMode")] string ContentMode,
    [property: JsonPropertyName("freshness")] string Freshness,
    [property: JsonPropertyName("snapshotId")] string SnapshotId,
    [property: JsonPropertyName("changeVersion")] long ChangeVersion);
