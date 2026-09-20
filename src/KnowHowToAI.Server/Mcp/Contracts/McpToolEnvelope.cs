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
        var code = ToExternalRoleCode(error.Code);
        var details = error.Details.ToDictionary(
            pair => ToExternalRoleDetail(pair.Key),
            pair => pair.Value,
            StringComparer.Ordinal);
        var message = ToExternalRoleText(error.Message);
        return new(code, message, details, warnings);
    }

    private static string ToExternalRoleCode(string code) => code switch
    {
        "AudienceNotFound" => "RoleNotFound",
        "AudienceInUse" => "RoleInUse",
        "AudienceNameRequired" => "RoleNameRequired",
        "AudienceIdRequired" => "RoleIdRequired",
        "CandidateAudienceDeleted" => "CandidateRoleDeleted",
        "CandidateAudienceNotFound" => "CandidateRoleNotFound",
        "DuplicateCandidateAudience" => "DuplicateCandidateRole",
        "RequestedAudienceDeleted" => "RequestedRoleDeleted",
        "RequestedAudienceNotFound" => "RequestedRoleNotFound",
        _ => code
    };

    private static string ToExternalRoleDetail(string detail) => detail switch
    {
        "audienceId" => "roleId",
        "audienceName" => "roleName",
        "candidateAudienceId" => "candidateRoleId",
        "requestedAudienceId" => "requestedRoleId",
        "targetAudienceId" => "targetRoleId",
        "sourceAudienceId" => "sourceRoleId",
        _ => detail
    };

    private static string ToExternalRoleText(string text) => text
        .Replace("Audience", "Role", StringComparison.Ordinal)
        .Replace("audience", "role", StringComparison.Ordinal)
        .Replace("Zielgruppe", "Rolle", StringComparison.Ordinal)
        .Replace("zielgruppe", "rolle", StringComparison.Ordinal);
}
