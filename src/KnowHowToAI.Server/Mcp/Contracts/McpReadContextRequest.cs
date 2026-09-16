using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts;

/// <summary>
/// Gemeinsame Selektor-Felder aller Read-Tools. Genau ein Selektor darf gesetzt sein
/// (Working Transaction, historischer Snapshot oder ohne Selektor der Current Snapshot);
/// transactionId und snapshotId schließen sich gegenseitig aus
/// (verbindlich: docs/konzept/05-MCP-API.md, Abschnitt 64).
/// </summary>
public sealed record McpReadContextRequest(
    [property: JsonPropertyName("transactionId")] string? TransactionId = null,
    [property: JsonPropertyName("snapshotId")] string? SnapshotId = null,
    [property: JsonPropertyName("includeDeleted")] bool? IncludeDeleted = null);
