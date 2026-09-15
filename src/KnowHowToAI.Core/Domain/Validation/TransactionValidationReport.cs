using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Validation;

/// <summary>Deterministischer Befund einer vollständigen Prüfung eines Working Snapshots.</summary>
public sealed record TransactionValidationReport(
    IReadOnlyList<DomainError> Errors,
    IReadOnlyList<DomainWarning> Warnings,
    IReadOnlyList<StaleContent> StaleContents,
    IReadOnlyList<RefactoringCandidate> RefactoringCandidates)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Ein expliziter abgeleiteter Content, dessen Provenienz nicht mehr aktuell ist.</summary>
public sealed record StaleContent(NodeId NodeId, RoleId RoleId, ContentRevisionId ContentRevisionId);

/// <summary>Ein Node, dessen Qualitätsbefunde eine bewusste, transaktionale Umstrukturierung nahelegen.</summary>
public sealed record RefactoringCandidate(NodeId NodeId, IReadOnlyList<string> ReasonCodes);
