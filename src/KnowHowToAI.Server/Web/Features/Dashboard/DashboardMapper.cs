using KnowHowToAI.Core.Application.Dashboard;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Features.History;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

/// <summary>
/// Statische Mapper-Methoden zur Überführung des Dashboard-Ergebnisses in UI-ViewModels.
/// Stellt sicher, dass Domain-Typen nicht im Rendering verwendet werden und Fehler
/// sowie Warnungen vollständig erhalten bleiben.
/// </summary>
public static class DashboardMapper
{
    public static DashboardViewModel ToDashboardViewModel(DashboardResult result, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        var currentSnapshot = HistoryMapper.ToSnapshotViewModel(result.CurrentSnapshot);
        var latestRelease = result.LatestRelease is not null
            ? HistoryMapper.ToReleaseItemViewModel(result.LatestRelease)
            : null;

        var openTransactions = result.OpenTransactions
            .Select(tx => ToOpenTransactionItemViewModel(tx, nowUtc))
            .ToArray();

        var qualitySummary = ToQualityViewModel(result.QualitySummary);

        var recentChanges = result.RecentNodeChanges
            .Select(ToRecentNodeChangeViewModel)
            .ToArray();

        return new DashboardViewModel(
            currentSnapshot,
            latestRelease,
            openTransactions,
            qualitySummary,
            recentChanges);
    }

    public static OpenTransactionItemViewModel ToOpenTransactionItemViewModel(
        OpenTransactionSummary summary,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var tx = summary.Transaction;
        var isOlderThan7Days = tx.CreatedAtUtc <= nowUtc.AddDays(-7);
        var errors = summary.ValidationErrors.Select(ToDiagnosticViewModel).ToArray();

        return new OpenTransactionItemViewModel(
            tx.TransactionId.Value,
            tx.BaseSnapshotId.Value,
            tx.WorkingSnapshotId.Value,
            tx.Purpose,
            tx.Actor,
            tx.Client,
            tx.CreatedAtUtc,
            tx.ChangeVersion,
            isOlderThan7Days,
            errors);
    }

    public static DashboardQualityViewModel ToQualityViewModel(CurrentQualitySummary quality)
    {
        ArgumentNullException.ThrowIfNull(quality);

        var staleContents = quality.StaleContents
            .Select(s => new StaleContentItemViewModel(s.NodeId.Value, s.RoleId.Value, s.ContentRevisionId.Value))
            .ToArray();

        var warnings = quality.Warnings
            .Select(ToDiagnosticViewModel)
            .ToArray();

        return new DashboardQualityViewModel(
            staleContents.Length,
            staleContents,
            warnings.Length,
            warnings,
            quality.RefactoringCandidates.Count);
    }

    public static RecentNodeChangeViewModel ToRecentNodeChangeViewModel(RecentNodeChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        return new RecentNodeChangeViewModel(
            change.NodeId.Value,
            change.Title,
            change.Kind.ToString());
    }

    private static DashboardDiagnosticViewModel ToDiagnosticViewModel(DomainIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        return new DashboardDiagnosticViewModel(issue.Code, issue.Message, issue.Details);
    }

    public static Result<DashboardViewModel> ToDashboardResult(
        Result<DashboardResult> result,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
        {
            return Result<DashboardViewModel>.Failure(result.Error!, result.Warnings);
        }

        return Result<DashboardViewModel>.Success(
            result.Value is null ? null : ToDashboardViewModel(result.Value, nowUtc),
            result.Warnings);
    }
}
