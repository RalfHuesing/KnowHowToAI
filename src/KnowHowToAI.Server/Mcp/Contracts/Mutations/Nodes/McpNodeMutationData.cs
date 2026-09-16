using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Mutations.Nodes;

/// <summary>
/// Ergebnis einer globalen Strukturänderung im MCP-Vertrag. Alle IDs sind Strings
/// im Format der Tool-Ausgaben und ohne Umformatierung als Folgeparameter verwendbar.
/// </summary>
public sealed record McpNodeMutationData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("parentNodeId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ParentNodeId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("snapshotId")] string SnapshotId,
    [property: JsonPropertyName("changeVersion")] long ChangeVersion,
    [property: JsonPropertyName("affectedNodeIds")] IReadOnlyList<string> AffectedNodeIds);
