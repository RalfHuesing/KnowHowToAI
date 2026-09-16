using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Transactions;

/// <summary>
/// Metadaten einer KnowHowTo-AI-Transaction im MCP-Vertrag. Alle IDs sind Strings
/// im Format der Tool-Ausgaben und ohne Umformatierung als Folgeparameter verwendbar
/// (verbindlich: docs/konzept/05-MCP-API.md, Abschnitt 64).
/// </summary>
public sealed record McpTransactionData(
    [property: JsonPropertyName("transactionId")] string TransactionId,
    [property: JsonPropertyName("baseSnapshotId")] string BaseSnapshotId,
    [property: JsonPropertyName("workingSnapshotId")] string WorkingSnapshotId,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("createdAtUtc")] DateTimeOffset CreatedAtUtc,
    [property: JsonPropertyName("committedAtUtc")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? CommittedAtUtc = null,
    [property: JsonPropertyName("purpose")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Purpose = null,
    [property: JsonPropertyName("actor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Actor = null,
    [property: JsonPropertyName("client")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Client = null,
    [property: JsonPropertyName("commitMessage")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CommitMessage = null);
