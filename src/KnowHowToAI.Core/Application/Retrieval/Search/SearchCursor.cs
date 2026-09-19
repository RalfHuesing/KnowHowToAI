using System.Buffers.Text;
using System.Text.Json;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Opaker, URL-sicherer Keyset-Cursor für <see cref="SearchService.SearchAsync"/>.
/// Bindet die Fortsetzungsposition an SnapshotId, Suchtext, RoleId und bei
/// Working-Snapshot-Reads an die ChangeVersion zur Erkennung veralteter Resultsets.
/// </summary>
public sealed record SearchCursor(
    SnapshotId SnapshotId,
    long? ChangeVersion,
    string QueryText,
    RoleId? RoleId,
    int LastRank,
    int LastSortOrder,
    NodeId LastNodeId,
    string? FilterFingerprint = null)
{
    /// <summary>Serialisiert und kodiert den Cursor als opaken Base64Url-String.</summary>
    public string Encode()
    {
        var dto = new SearchCursorDto(
            SnapshotId.Value,
            ChangeVersion,
            QueryText,
            RoleId?.Value,
            LastRank,
            LastSortOrder,
            LastNodeId.Value,
            FilterFingerprint);

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dto);
        return Base64Url.EncodeToString(jsonBytes);
    }

    /// <summary>
    /// Versucht, einen opaken Cursor-String zu parsen.
    /// Liefert <c>null</c>, wenn der String ungültig, manipuliert oder nicht dekodierbar ist.
    /// </summary>
    public static SearchCursor? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var bytes = Base64Url.DecodeFromChars(cursor);
            var dto = JsonSerializer.Deserialize<SearchCursorDto>(bytes);
            if (!IsValid(dto))
                return null;

            var validDto = dto!;
            return new SearchCursor(
                new SnapshotId(validDto.SnapshotId),
                validDto.ChangeVersion,
                validDto.QueryText,
                validDto.RoleId is not null ? new RoleId(validDto.RoleId) : null,
                validDto.LastRank,
                validDto.LastSortOrder,
                new NodeId(validDto.LastNodeId),
                validDto.FilterFingerprint);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValid(SearchCursorDto? dto) =>
        dto is not null
        && dto.SnapshotId > 0
        && dto.ChangeVersion is null or >= 0
        && !string.IsNullOrWhiteSpace(dto.QueryText)
        && (dto.RoleId is null || !string.IsNullOrWhiteSpace(dto.RoleId))
        && dto.LastRank is >= 1 and <= 3
        && dto.LastSortOrder >= 0
        && dto.LastNodeId != Guid.Empty;

    private sealed record SearchCursorDto(
        long SnapshotId,
        long? ChangeVersion,
        string QueryText,
        string? RoleId,
        int LastRank,
        int LastSortOrder,
        Guid LastNodeId,
        string? FilterFingerprint = null);
}
