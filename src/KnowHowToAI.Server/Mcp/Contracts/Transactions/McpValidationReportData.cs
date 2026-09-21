using System.Text.Json.Serialization;
using KnowHowToAI.Server.Mcp.Contracts;

namespace KnowHowToAI.Server.Mcp.Contracts.Transactions;

/// <summary>
/// Ergebnis der vollständigen Working-Snapshot-Prüfung einer offenen Transaction
/// im MCP-Vertrag. Harte Fehler blockieren den Commit, Warnungen und Hinweise nicht.
/// </summary>
public sealed record McpValidationReportData(
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("errors")] IReadOnlyList<McpValidationIssueData> Errors,
    [property: JsonPropertyName("warnings")] IReadOnlyList<McpWarning> Warnings,
    [property: JsonPropertyName("staleContents")] IReadOnlyList<McpStaleContentData> StaleContents,
    [property: JsonPropertyName("refactoringCandidates")] IReadOnlyList<McpRefactoringCandidateData> RefactoringCandidates);

/// <summary>Strukturierter harter Fehlerbefund der Prüfung mit stabilem Code.</summary>
public sealed record McpValidationIssueData(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("details")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string>? Details = null);

/// <summary>Ein Derived Content, dessen Provenienz nicht mehr aktuell ist.</summary>
public sealed record McpStaleContentData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("audienceId")] string AudienceId,
    [property: JsonPropertyName("contentRevisionId")] string ContentRevisionId);

/// <summary>Ein Node mit Qualitätsbefunden als bewusster Refactoring-Kandidat.</summary>
public sealed record McpRefactoringCandidateData(
    [property: JsonPropertyName("nodeId")] string NodeId,
    [property: JsonPropertyName("reasonCodes")] IReadOnlyList<string> ReasonCodes);
