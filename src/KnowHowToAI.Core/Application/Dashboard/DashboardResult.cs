using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Dashboard;

/// <summary>
/// Aggregiertes Ergebnis der Dashboard-Abfrage.
/// </summary>
public sealed record DashboardResult(
    Snapshot CurrentSnapshot,
    Release? LatestRelease,
    IReadOnlyList<OpenTransactionSummary> OpenTransactions,
    CurrentQualitySummary QualitySummary,
    IReadOnlyList<RecentNodeChange> RecentNodeChanges);
