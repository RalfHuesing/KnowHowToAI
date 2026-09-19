using KnowHowToAI.Server.Web.Features.History;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

/// <summary>
/// Aggregiertes UI-ViewModel für das Wissensdashboard.
/// </summary>
public sealed record DashboardViewModel(
    SnapshotViewModel CurrentSnapshot,
    ReleaseItemViewModel? LatestRelease,
    IReadOnlyList<OpenTransactionItemViewModel> OpenTransactions,
    DashboardQualityViewModel QualitySummary,
    IReadOnlyList<RecentNodeChangeViewModel> RecentChanges);
