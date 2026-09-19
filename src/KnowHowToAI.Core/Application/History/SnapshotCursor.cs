using System.Buffers.Text;
using System.Text.Json;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.History;

/// <summary>Opaker Keyset-Cursor für absteigend paginierte committed Snapshots.</summary>
public sealed record SnapshotCursor(SnapshotId BeforeSnapshotId)
{
    public string Encode() => Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(
        new SnapshotCursorDto(BeforeSnapshotId.Value)));

    public static SnapshotCursor? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var dto = JsonSerializer.Deserialize<SnapshotCursorDto>(Base64Url.DecodeFromChars(cursor));
            return dto is null || dto.BeforeSnapshotId <= 0
                ? null
                : new SnapshotCursor(new SnapshotId(dto.BeforeSnapshotId));
        }
        catch
        {
            return null;
        }
    }

    private sealed record SnapshotCursorDto(long BeforeSnapshotId);
}
