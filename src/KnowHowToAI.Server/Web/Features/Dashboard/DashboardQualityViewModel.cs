namespace KnowHowToAI.Server.Web.Features.Dashboard;

/// <summary>
/// UI-ViewModel für die Qualitätsübersicht des aktuellen Snapshots.
/// </summary>
public sealed record DashboardQualityViewModel(
    int StaleContentCount,
    IReadOnlyList<StaleContentItemViewModel> StaleContents,
    int WarningCount,
    IReadOnlyList<string> WarningMessages,
    int RefactoringCandidateCount);

/// <summary>
/// UI-ViewModel für einen veralteten (stale) abgeleiteten Inhalt.
/// </summary>
public sealed record StaleContentItemViewModel(
    Guid NodeId,
    string RoleId,
    Guid ContentRevisionId);
