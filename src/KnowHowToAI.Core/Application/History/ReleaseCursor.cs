using System.Buffers.Text;
using System.Globalization;
using System.Text.Json;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Opaker, URL-sicherer Keyset-Cursor für paginierte Releases (<see cref="ReleaseService.ListReleasesAsync"/>).
/// Bindet den Fortsetzungszustand an die zuletzt gesehene ReleaseId.
/// </summary>
public sealed record ReleaseCursor(long AfterReleaseId)
{
    /// <summary>Serialisiert und kodiert den Cursor als opaken Base64Url-String.</summary>
    public string Encode()
    {
        var dto = new ReleaseCursorDto(AfterReleaseId);
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dto);
        return Base64Url.EncodeToString(jsonBytes);
    }

    /// <summary>
    /// Versucht, einen Cursor-String zu parsen (unterstützt opaken Base64Url-String sowie numerischen Fallback).
    /// Liefert <c>null</c>, wenn der String ungültig, manipuliert oder nicht dekodierbar ist.
    /// </summary>
    public static ReleaseCursor? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        if (long.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var directId) && directId > 0)
            return new ReleaseCursor(directId);

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

            var dto = JsonSerializer.Deserialize<ReleaseCursorDto>(bytes);
            if (dto is null || dto.AfterReleaseId <= 0)
                return null;

            return new ReleaseCursor(dto.AfterReleaseId);
        }
        catch
        {
            return null;
        }
    }

    private sealed record ReleaseCursorDto(long AfterReleaseId);
}
