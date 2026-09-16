using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace KnowHowToAI.Exploration.Reporting;

public sealed record EnvelopeReport(string Json, int CompactByteLength);

/// <summary>
/// JSON-Seriatisierung und -Validierung für Tool-Antworten. Approximiert die
/// stdio-Repräsentation: jeder Envelope muss verlustfrei serialisierbar und
/// wieder parsebar sein und wird auf seine kompakte Übertragungsgröße vermessen.
/// </summary>
public static class EnvelopeJson
{
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions Compact = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ToJson<T>(T value) where T : class =>
        JsonSerializer.Serialize(value, Indented);

    public static EnvelopeReport Report<T>(T envelope) where T : class
    {
        var json = ToJson(envelope);
        var compactJson = JsonSerializer.Serialize(envelope, Compact);
        using var document = JsonDocument.Parse(json);
        return new EnvelopeReport(json, Encoding.UTF8.GetByteCount(compactJson));
    }
}
