using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.History;

/// <summary>
/// Metadaten eines Snapshots im MCP-Vertrag (get_snapshot).
/// Alle IDs sind Strings im Format der Tool-Ausgaben und ohne Umformatierung
/// als Folgeparameter verwendbar.
/// </summary>
public sealed record McpSnapshotData(
    [property: JsonPropertyName("snapshotId")] string SnapshotId,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("createdAtUtc")] DateTimeOffset CreatedAtUtc,
    [property: JsonPropertyName("baseSnapshotId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BaseSnapshotId = null,
    [property: JsonPropertyName("committedAtUtc")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? CommittedAtUtc = null);
