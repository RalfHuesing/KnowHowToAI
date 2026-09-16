using System.Buffers.Text;
using System.Text.Json;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Opaker, URL-sicherer Cursor für paginierte Snapshot-Diffs (<see cref="HistoryService.CompareSnapshotsAsync"/>
/// und <see cref="HistoryService.GetTransactionChangesAsync"/>).
/// Bindet den Fortsetzungszustand an BaseSnapshotId, TargetSnapshotId,
/// bei offenen Transaktionen an ChangeVersion und an den fortlaufenden Item-Offset.
/// </summary>
public sealed record DiffCursor(
    SnapshotId BaseSnapshotId,
    SnapshotId TargetSnapshotId,
    long? ChangeVersion,
    int NextOffset)
{
    /// <summary>Serialisiert und kodiert den Cursor als opaken Base64Url-String.</summary>
    public string Encode()
    {
        var dto = new DiffCursorDto(
            BaseSnapshotId.Value,
            TargetSnapshotId.Value,
            ChangeVersion,
            NextOffset);

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dto);
        return Base64Url.EncodeToString(jsonBytes);
    }

    /// <summary>
    /// Versucht, einen opaken Cursor-String zu parsen.
    /// Liefert <c>null</c>, wenn der String ungültig, manipuliert oder nicht dekodierbar ist.
    /// </summary>
    public static DiffCursor? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var bytes = Base64Url.DecodeFromChars(cursor);
            var dto = JsonSerializer.Deserialize<DiffCursorDto>(bytes);
            if (dto is null
                || dto.BaseSnapshotId <= 0
                || dto.TargetSnapshotId <= 0
                || dto.ChangeVersion is < 0
                || dto.NextOffset < 0)
                return null;

            return new DiffCursor(
                new SnapshotId(dto.BaseSnapshotId),
                new SnapshotId(dto.TargetSnapshotId),
                dto.ChangeVersion,
                dto.NextOffset);
        }
        catch
        {
            return null;
        }
    }

    private sealed record DiffCursorDto(
        long BaseSnapshotId,
        long TargetSnapshotId,
        long? ChangeVersion,
        int NextOffset);
}
