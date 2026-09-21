using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.History;

/// <summary>
/// Ein einzelner Diff-Eintrag im MCP-Vertrag. Enthält Richtung und
/// die geänderten Schlüsselfelder; keine vollständigen Content-Dumps.
/// </summary>
public sealed record McpDiffEntryData(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("entityType")] string EntityType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("audienceId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AudienceId = null,
    [property: JsonPropertyName("sourceNodeId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SourceNodeId = null,
    [property: JsonPropertyName("sourceAudienceId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SourceAudienceId = null);

/// <summary>
/// Paginierter Netto-Diff zwischen zwei Snapshots (compare_snapshots, get_transaction_changes).
/// Enthält ausschließlich geänderte Einträge ohne Rekonstruktion eines Operation Logs.
/// </summary>
public sealed record McpSnapshotDiffData(
    [property: JsonPropertyName("baseSnapshotId")] string BaseSnapshotId,
    [property: JsonPropertyName("targetSnapshotId")] string TargetSnapshotId,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("items")] IReadOnlyList<McpDiffEntryData> Items,
    [property: JsonPropertyName("nextCursor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor = null);

/// <summary>
/// Ergebnis von get_transaction_changes: Diff-Daten plus Transaction-Metadaten.
/// </summary>
public sealed record McpTransactionChangesData(
    [property: JsonPropertyName("transactionId")] string TransactionId,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("baseSnapshotId")] string BaseSnapshotId,
    [property: JsonPropertyName("workingSnapshotId")] string WorkingSnapshotId,
    [property: JsonPropertyName("changes")] McpSnapshotDiffData Changes);
