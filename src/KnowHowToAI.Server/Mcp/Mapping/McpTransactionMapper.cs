using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Transactions;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Bildet die transportneutralen Ergebnisse der Transaction-Engine auf den
/// gemeinsamen MCP-Antwort-Envelope und die Transaktions-DTOs ab.
/// Enthält keine Fachlogik.
/// </summary>
internal static class McpTransactionMapper
{
    public static McpTransactionData ToData(KnowledgeTransaction transaction) => new(
        transaction.TransactionId.ToString(),
        transaction.BaseSnapshotId.ToString(),
        transaction.WorkingSnapshotId.ToString(),
        transaction.State.ToString(),
        transaction.CreatedAtUtc,
        transaction.CommittedAtUtc,
        transaction.Purpose,
        transaction.Actor,
        transaction.Client,
        transaction.CommitMessage);

    public static McpValidationReportData ToData(TransactionValidationReport report) => new(
        report.IsValid,
        report.Errors.Select(ToIssue).ToArray(),
        report.Warnings.Select(McpResultMapper.ToWarning).ToArray(),
        report.StaleContents.Select(static stale => new McpStaleContentData(
            stale.NodeId.ToString(),
            stale.RoleId.ToString(),
            stale.ContentRevisionId.ToString())).ToArray(),
        report.RefactoringCandidates.Select(static candidate => new McpRefactoringCandidateData(
            candidate.NodeId.ToString(),
            candidate.ReasonCodes)).ToArray());

    public static McpToolEnvelope<McpTransactionData> ToEnvelope(Result<KnowledgeTransaction> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpTransactionData>.Success(ToData(result.Value!))
            : McpToolEnvelope<McpTransactionData>.Failure(result.Error!);

    public static McpToolEnvelope<McpValidationReportData> ToEnvelope(Result<TransactionValidationReport> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpValidationReportData>.Success(ToData(result.Value!))
            : McpToolEnvelope<McpValidationReportData>.Failure(result.Error!);

    /// <summary>
    /// Mappt ein Commit-Ergebnis. Befunde des Validierungsberichts bleiben auch bei
    /// einer fachlichen Ablehnung als Envelope-Warnungen sichtbar.
    /// </summary>
    public static McpToolEnvelope<McpTransactionData> ToEnvelope(CommitTransactionResult result)
    {
        var warnings = result.ValidationReport is null
            ? null
            : result.ValidationReport.Warnings.Select(McpResultMapper.ToWarning).ToArray();
        return result.IsCommitted
            ? McpToolEnvelope<McpTransactionData>.Success(ToData(result.Transaction!), warnings)
            : McpToolEnvelope<McpTransactionData>.Failure(result.Error!, warnings);
    }

    /// <summary>
    /// Parst einen Transaction-ID-String exakt im Format der Tool-Ausgaben (GUID "D").
    /// Ein nicht parsebarer Wert kann keine existierende Transaction bezeichnen und
    /// führt daher zu <c>TransactionNotFound</c> mit dem Rohwert in den Details.
    /// </summary>
    public static Result<TransactionId> ParseTransactionId(string? transactionId)
    {
        if (transactionId is not null && Guid.TryParseExact(transactionId, "D", out var parsed))
            return Result<TransactionId>.Success(new TransactionId(parsed));

        return Result<TransactionId>.Failure(new DomainError(
            TransactionValidationErrorCodes.TransactionNotFound,
            "Die angefragte Transaction existiert nicht.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId ?? string.Empty
            }));
    }

    private static McpValidationIssueData ToIssue(DomainError error) =>
        new(error.Code, error.Message, NonEmpty(error.Details));

    private static IReadOnlyDictionary<string, string>? NonEmpty(IReadOnlyDictionary<string, string> details) =>
        details is { Count: > 0 } ? details : null;
}
