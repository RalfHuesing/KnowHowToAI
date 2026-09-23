using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Server.Web.Features.Drafts;

/// <summary>Darstellungsmodell für den read-only Validierungsbefund einer Transaction.</summary>
public sealed record TransactionValidationViewModel(
    bool IsValid,
    IReadOnlyList<TransactionValidationIssueViewModel> Errors,
    IReadOnlyList<TransactionValidationIssueViewModel> Warnings,
    IReadOnlyList<TransactionValidationStaleContentViewModel> StaleContents,
    IReadOnlyList<TransactionValidationRefactoringViewModel> RefactoringCandidates,
    long ChangeVersion);

/// <summary>Ein handlungsorientierter Fehler- oder Warnbefund.</summary>
public sealed record TransactionValidationIssueViewModel(
    string Code,
    string Message,
    Guid? NodeId,
    string? audienceId);

/// <summary>Ein nicht mehr aktueller abgeleiteter Inhalt.</summary>
public sealed record TransactionValidationStaleContentViewModel(
    Guid NodeId,
    string AudienceId,
    Guid ContentRevisionId);

/// <summary>Eine durch Qualitätswarnungen abgeleitete Strukturpflegeempfehlung.</summary>
public sealed record TransactionValidationRefactoringViewModel(
    Guid NodeId,
    IReadOnlyList<string> ReasonCodes);

/// <summary>Mappt den transportneutralen Validierungsbefund für die Transaction-Oberfläche.</summary>
public static class TransactionValidationMapper
{
    public static TransactionValidationViewModel ToViewModel(TransactionValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new TransactionValidationViewModel(
            report.IsValid,
            report.Errors.Select(ToIssue).ToArray(),
            report.Warnings.Select(ToIssue).ToArray(),
            report.StaleContents.Select(static content => new TransactionValidationStaleContentViewModel(
                content.NodeId.Value,
                content.AudienceId.Value,
                content.ContentRevisionId.Value)).ToArray(),
            report.RefactoringCandidates.Select(static candidate => new TransactionValidationRefactoringViewModel(
                candidate.NodeId.Value,
                candidate.ReasonCodes)).ToArray(),
            report.ChangeVersion);
    }

    private static TransactionValidationIssueViewModel ToIssue(DomainIssue issue) =>
        new(
            issue.Code,
            issue.Message,
            TryGetNodeId(issue.Details),
            issue.Details.GetValueOrDefault(TransactionValidationCodes.AudienceIdDetail));

    private static Guid? TryGetNodeId(IReadOnlyDictionary<string, string> details) =>
        details.TryGetValue(TransactionValidationCodes.NodeIdDetail, out var value)
            && Guid.TryParse(value, out var nodeId)
                ? nodeId
                : null;
}
