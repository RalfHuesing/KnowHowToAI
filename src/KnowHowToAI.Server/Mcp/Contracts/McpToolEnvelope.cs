using System.Text.Json.Serialization;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Mcp.Contracts;

/// <summary>
/// Gemeinsamer Antwort-Envelope aller MCP-Tools. Fixiert JSON-Feldnamen,
/// Nullability und Feldgrenzen vor jedem Handlercode.
/// </summary>
/// <typeparam name="TData">Referenztyp des fachlichen Payloads oder <c>object</c> für payload-freie Tools.</typeparam>
public sealed record McpToolEnvelope<TData> where TData : class
{
    public McpToolEnvelope(
        string code,
        string? message = null,
        IReadOnlyDictionary<string, string>? details = null,
        IReadOnlyList<McpWarning>? warnings = null,
        TData? payload = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var isSuccess = string.Equals(code, ResultCodes.Success, StringComparison.Ordinal);
        if (isSuccess && message is not null)
            throw new ArgumentException("Erfolgsantworten tragen keine message.", nameof(message));
        if (!isSuccess && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Fehlerantworten erfordern eine message.", nameof(message));
        if (isSuccess && details is { Count: > 0 })
            throw new ArgumentException("Erfolgsantworten tragen keine details.", nameof(details));
        if (!isSuccess && payload is not null)
            throw new ArgumentException("Fehlerantworten tragen keine data.", nameof(payload));

        Code = code;
        Message = message;
        Details = details is { Count: > 0 } ? details : null;
        Warnings = warnings is { Count: > 0 } ? warnings : null;
        Data = payload;
    }

    [JsonPropertyName("code")]
    public string Code { get; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Details { get; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TData? Data { get; }

    [JsonPropertyName("warnings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<McpWarning>? Warnings { get; }

    [JsonIgnore]
    public bool IsSuccess => string.Equals(Code, ResultCodes.Success, StringComparison.Ordinal);

    /// <summary>Erfolgs-Envelope ohne message/details und ohne data (z. B. discard_transaction).</summary>
    public static McpToolEnvelope<TData> Success(TData? payload = null, IReadOnlyList<McpWarning>? warnings = null) =>
        new(ResultCodes.Success, warnings: warnings, payload: payload);

    /// <summary>Fehler-Envelope aus einem stabilen DomainError; data bleibt immer ungesetzt.</summary>
    public static McpToolEnvelope<TData> Failure(DomainError error, IReadOnlyList<McpWarning>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        var details = error.Details is { Count: > 0 }
            ? new Dictionary<string, string>(error.Details, StringComparer.Ordinal)
            : null;
        return new(error.Code, error.Message, details, warnings);
    }
}
