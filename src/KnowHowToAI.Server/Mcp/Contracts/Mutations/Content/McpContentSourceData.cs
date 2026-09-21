using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Mutations.Content;

/// <summary>
/// Bezeichnet eine beim Ableiten verwendete explizite Source-Revision als
/// Tool-Argument und Antwortfeld.
/// </summary>
public sealed record McpContentSourceData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("audienceId")] string AudienceId,
    [property: JsonPropertyName("contentRevisionId")] string ContentRevisionId);
