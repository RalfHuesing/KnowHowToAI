using System.Buffers.Text;
using System.Text.Json;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Opaker, URL-sicherer Keyset-Cursor für <see cref="NavigationService.ListAudiencesAsync"/>.
/// Bindet die Cursor-Position an Snapshot, Filter (IncludeDeleted) und bei
/// Working Reads an <see cref="ChangeVersion"/> zur Erkennung abgelaufener Resultsets.
/// </summary>
public sealed record AudienceCursor(
    SnapshotId SnapshotId,
    long? ChangeVersion,
    bool IncludeDeleted,
    AudienceId LastAudienceId)
{
    /// <summary>Serialisiert und kodiert den Cursor als opaken Base64Url-String.</summary>
    public string Encode()
    {
        var dto = new AudienceCursorDto(
            SnapshotId.Value,
            ChangeVersion,
            IncludeDeleted,
            LastAudienceId.Value);

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dto);
        return Base64Url.EncodeToString(jsonBytes);
    }

    /// <summary>
    /// Versucht, einen opaken Base64Url-Cursor zu parsen.
    /// Liefert <c>null</c>, wenn der String ungültig, manipuliert oder nicht dekodierbar ist.
    /// </summary>
    public static AudienceCursor? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var bytes = Base64Url.DecodeFromChars(cursor);
            var dto = JsonSerializer.Deserialize<AudienceCursorDto>(bytes);
            if (dto is null
                || dto.SnapshotId <= 0
                || dto.ChangeVersion is < 0
                || string.IsNullOrWhiteSpace(dto.LastAudienceId))
                return null;

            return new AudienceCursor(
                new SnapshotId(dto.SnapshotId),
                dto.ChangeVersion,
                dto.IncludeDeleted,
                new AudienceId(dto.LastAudienceId));
        }
        catch
        {
            return null;
        }
    }

    private sealed record AudienceCursorDto(
        long SnapshotId,
        long? ChangeVersion,
        bool IncludeDeleted,
        string LastAudienceId);
}
