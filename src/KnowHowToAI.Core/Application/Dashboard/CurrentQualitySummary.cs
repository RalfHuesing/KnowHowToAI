using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Dashboard;

/// <summary>
/// Qualitätsübersicht des aktuellen Snapshots über alle Rollen.
/// Committed Snapshots besitzen definitionsgemäß keine harten Validierungsfehler.
/// </summary>
public sealed record CurrentQualitySummary(
    IReadOnlyList<StaleContent> StaleContents,
    IReadOnlyList<DomainWarning> Warnings,
    IReadOnlyList<RefactoringCandidate> RefactoringCandidates);
