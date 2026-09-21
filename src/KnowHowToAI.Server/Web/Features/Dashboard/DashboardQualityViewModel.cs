namespace KnowHowToAI.Server.Web.Features.Dashboard;

/// <summary>
/// UI-ViewModel für die Qualitätsübersicht des aktuellen Snapshots.
/// </summary>
public sealed record DashboardQualityViewModel(
    int StaleContentCount,
    IReadOnlyList<StaleContentItemViewModel> StaleContents,
    int WarningCount,
    IReadOnlyList<DashboardDiagnosticViewModel> Warnings,
    int RefactoringCandidateCount);

/// <summary>
/// UI-ViewModel für einen veralteten (stale) abgeleiteten Inhalt.
/// </summary>
public sealed record StaleContentItemViewModel(
    Guid NodeId,
    string AudienceId,
    Guid ContentRevisionId);

/// <summary>Strukturierte, für die Oberfläche sichere Darstellung einer fachlichen Diagnose.</summary>
public sealed record DashboardDiagnosticViewModel(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string> Details);
