using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Mutations.Nodes;

/// <summary>
/// Ergebnis einer globalen Strukturänderung im MCP-Vertrag. Alle IDs sind Strings
/// im Format der Tool-Ausgaben und ohne Umformatierung als Folgeparameter verwendbar.
/// Die Content-Felder sind nur gesetzt, wenn der Aufruf gleichzeitig Zielgruppen-Content
/// gesetzt hat (kombinierter create_node-Aufruf).
/// </summary>
public sealed record McpNodeMutationData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("parentNodeId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ParentNodeId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("snapshotId")] string SnapshotId,
    [property: JsonPropertyName("changeVersion")] long ChangeVersion,
    [property: JsonPropertyName("affectedNodeIds")] IReadOnlyList<string> AffectedNodeIds,
    [property: JsonPropertyName("audienceId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AudienceId = null,
    [property: JsonPropertyName("contentRevisionId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContentRevisionId = null,
    [property: JsonPropertyName("contentMode")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContentMode = null,
    [property: JsonPropertyName("freshness")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Freshness = null);
