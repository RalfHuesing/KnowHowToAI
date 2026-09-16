using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.History;

/// <summary>
/// Release-Metadaten im MCP-Vertrag (create_release, list_releases).
/// Alle IDs sind Strings im Format der Tool-Ausgaben und ohne Umformatierung
/// als Folgeparameter verwendbar.
/// </summary>
public sealed record McpReleaseData(
    [property: JsonPropertyName("releaseId")] string ReleaseId,
    [property: JsonPropertyName("snapshotId")] string SnapshotId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("releasedAtUtc")] DateTimeOffset ReleasedAtUtc,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description = null);

/// <summary>
/// Paginiertes Ergebnis von list_releases.
/// </summary>
public sealed record McpReleasePageData(
    [property: JsonPropertyName("items")] IReadOnlyList<McpReleaseData> Items,
    [property: JsonPropertyName("nextCursor")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor = null);
