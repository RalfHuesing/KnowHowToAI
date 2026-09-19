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
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? CommittedAtUtc = null,
    [property: JsonPropertyName("transactionId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TransactionId = null,
    [property: JsonPropertyName("actor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Actor = null,
    [property: JsonPropertyName("client")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Client = null,
    [property: JsonPropertyName("purpose")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Purpose = null,
    [property: JsonPropertyName("commitMessage")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CommitMessage = null);
