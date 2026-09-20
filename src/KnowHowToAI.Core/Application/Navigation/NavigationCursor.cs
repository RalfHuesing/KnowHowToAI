using System.Text.Json;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Opaker, URL-sicherer Cursor für <see cref="NavigationService.ListChildrenAsync"/>.
/// Bindet die Cursor-Position an Snapshot, Query/Filter (ParentNodeId, AudienceId, IncludeDeleted)
/// und bei Working Reads an <see cref="ChangeVersion"/> zur Erkennung abgelaufener Resultsets.
/// </summary>
public sealed record NavigationCursor(
    SnapshotId SnapshotId,
    long? ChangeVersion,
    NodeId? ParentNodeId,
    AudienceId AudienceId,
    bool IncludeDeleted,
    NodeId LastNodeId,
    int LastSortOrder)
{
    /// <summary>Serialisiert und kodiert den Cursor als opaken Base64Url-String.</summary>
    public string Encode()
    {
        var dto = new NavigationCursorDto(
            SnapshotId.Value,
            ChangeVersion,
            ParentNodeId?.Value,
            AudienceId.Value,
            IncludeDeleted,
            LastNodeId.Value,
            LastSortOrder);

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dto);
        return System.Buffers.Text.Base64Url.EncodeToString(jsonBytes);
    }

    /// <summary>
    /// Versucht, einen opaken Cursor-String zu parsen.
    /// Liefert <c>null</c>, wenn der String ungültig, manipuliert oder nicht dekodierbar ist.
    /// </summary>
    public static NavigationCursor? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var bytes = System.Buffers.Text.Base64Url.DecodeFromChars(cursor);
            var dto = JsonSerializer.Deserialize<NavigationCursorDto>(bytes);
            if (dto is null
                || dto.SnapshotId <= 0
                || dto.ChangeVersion is < 0
                || string.IsNullOrWhiteSpace(dto.AudienceId)
                || dto.ParentNodeId == Guid.Empty
                || dto.LastNodeId == Guid.Empty
                || dto.LastSortOrder < 0)
                return null;

            return new NavigationCursor(
                new SnapshotId(dto.SnapshotId),
                dto.ChangeVersion,
                dto.ParentNodeId.HasValue ? new NodeId(dto.ParentNodeId.Value) : null,
                new AudienceId(dto.AudienceId),
                dto.IncludeDeleted,
                new NodeId(dto.LastNodeId),
                dto.LastSortOrder);
        }
        catch
        {
            return null;
        }
    }

    private sealed record NavigationCursorDto(
        long SnapshotId,
        long? ChangeVersion,
        Guid? ParentNodeId,
        string AudienceId,
        bool IncludeDeleted,
        Guid LastNodeId,
        int LastSortOrder);
}
