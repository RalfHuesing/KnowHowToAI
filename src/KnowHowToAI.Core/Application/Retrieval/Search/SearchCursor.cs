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
    NodeId LastNodeId)
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
            LastNodeId.Value);

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
            byte[] bytes;
            try
            {
                bytes = Base64Url.DecodeFromChars(cursor);
            }
            catch (FormatException)
            {
                bytes = Convert.FromBase64String(cursor);
            }

            var dto = JsonSerializer.Deserialize<SearchCursorDto>(bytes);
            if (dto is null || string.IsNullOrWhiteSpace(dto.QueryText))
                return null;

            return new SearchCursor(
                new SnapshotId(dto.SnapshotId),
                dto.ChangeVersion,
                dto.QueryText,
                dto.RoleId is not null ? new RoleId(dto.RoleId) : null,
                dto.LastRank,
                dto.LastSortOrder,
                new NodeId(dto.LastNodeId));
        }
        catch
        {
            return null;
        }
    }

    private sealed record SearchCursorDto(
        long SnapshotId,
        long? ChangeVersion,
        string QueryText,
        string? RoleId,
        int LastRank,
        int LastSortOrder,
        Guid LastNodeId);
}
