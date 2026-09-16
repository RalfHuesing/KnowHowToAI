using System.Text.Json.Serialization;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Mcp.Contracts;

/// <summary>
/// Maschinenlesbarer Qualitätshinweis im MCP-Vertrag, der ein fachlich gültiges
/// Ergebnis nicht verhindert. Spiegelt den Warncode-Katalog der Roadmap.
/// </summary>
public sealed record McpWarning
{
    [JsonConstructor]
    public McpWarning(string code, string message, IReadOnlyDictionary<string, string>? details = null)
        : this(new DomainWarning(code, message, details))
    {
    }

    public McpWarning(DomainWarning warning) => Warning = warning;

    [JsonIgnore]
    public DomainWarning Warning { get; }

    [JsonPropertyName("code")]
    public string Code => Warning.Code;

    [JsonPropertyName("message")]
    public string Message => Warning.Message;

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Details =>
        Warning.Details is { Count: > 0 } ? Warning.Details : null;
}
