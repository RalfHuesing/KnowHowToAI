using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Dashboard;

/// <summary>
/// Transportneutraler Application Service für das Wissensdashboard.
/// Aggregiert Current Snapshot, letzten Release, offene Transactions inklusive harter Validierungsfehler,
/// Qualitätsstatus über alle Rollen und zuletzt geänderte Nodes.
/// </summary>
public sealed class DashboardService
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IWorkingSnapshotValidationDataRepository _validationDataRepository;
    private readonly SnapshotReadRepositories _snapshotReadRepositories;
    private readonly ValidationPolicy _validationPolicy;

    public DashboardService(
        ISnapshotRepository snapshotRepository,
        IDashboardRepository dashboardRepository,
        IWorkingSnapshotValidationDataRepository validationDataRepository,
        SnapshotReadRepositories snapshotReadRepositories,
        ValidationPolicy validationPolicy)
    {
        ArgumentNullException.ThrowIfNull(snapshotRepository);
        ArgumentNullException.ThrowIfNull(dashboardRepository);
        ArgumentNullException.ThrowIfNull(validationDataRepository);
        ArgumentNullException.ThrowIfNull(snapshotReadRepositories);
        ArgumentNullException.ThrowIfNull(validationPolicy);

        _snapshotRepository = snapshotRepository;
        _dashboardRepository = dashboardRepository;
        _validationDataRepository = validationDataRepository;
        _snapshotReadRepositories = snapshotReadRepositories;
        _validationPolicy = validationPolicy;
    }

    /// <summary>
    /// Ermittelt den aggregierten Dashboard-Zustand.
    /// </summary>
    public async Task<Result<DashboardResult>> GetDashboardAsync(
        DashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currentSnapshot = await _snapshotRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var latestRelease = await _dashboardRepository.GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        var openTransactions = await _dashboardRepository.ListOpenTransactionsAsync(cancellationToken).ConfigureAwait(false);

        var openTransactionSummaries = new List<OpenTransactionSummary>(openTransactions.Count);
        foreach (var tx in openTransactions)
        {
            var errors = await ValidateOpenTransactionErrorsAsync(tx.TransactionId, cancellationToken).ConfigureAwait(false);
            openTransactionSummaries.Add(new OpenTransactionSummary(tx, errors));
        }

        var currentQuality = await EvaluateCurrentQualityAsync(currentSnapshot.SnapshotId, cancellationToken).ConfigureAwait(false);
        var recentNodeChanges = await ComputeRecentNodeChangesAsync(currentSnapshot, cancellationToken).ConfigureAwait(false);

        var result = new DashboardResult(
            currentSnapshot,
            latestRelease,
            openTransactionSummaries,
            currentQuality,
            recentNodeChanges);

        return Result<DashboardResult>.Success(result);
    }

    private async Task<IReadOnlyList<DomainError>> ValidateOpenTransactionErrorsAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken)
    {
        var validationDataResult = await _validationDataRepository.ReadOpenWorkingAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!validationDataResult.IsSuccess)
        {
            return validationDataResult.Error is not null ? [validationDataResult.Error] : [];
        }

        var data = validationDataResult.Value!;
        var report = TransactionValidator.Validate(new TransactionValidationRequest(
            data.Nodes,
            data.Roles,
            data.RoleResolutions,
            data.Contents,
            data.Dependencies,
            _validationPolicy.ToQualityWarningThresholds(),
            _validationPolicy.PossibleEmbeddedHeadingWarning));

        return report.Errors;
    }

    private async Task<CurrentQualitySummary> EvaluateCurrentQualityAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken)
    {
        var nodes = await _snapshotReadRepositories.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var roles = await _snapshotReadRepositories.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await _snapshotReadRepositories.Roles.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = await _snapshotReadRepositories.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _snapshotReadRepositories.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var report = TransactionValidator.Validate(new TransactionValidationRequest(
            nodes,
            roles,
            resolutions,
            contents,
            dependencies,
            _validationPolicy.ToQualityWarningThresholds(),
            _validationPolicy.PossibleEmbeddedHeadingWarning));

        return new CurrentQualitySummary(report.StaleContents, report.Warnings, report.RefactoringCandidates);
    }

    private async Task<IReadOnlyList<RecentNodeChange>> ComputeRecentNodeChangesAsync(
        Domain.Versioning.Snapshot currentSnapshot,
        CancellationToken cancellationToken)
    {
        if (currentSnapshot.BaseSnapshotId is null)
        {
            return [];
        }

        var baseSnapshotId = currentSnapshot.BaseSnapshotId.Value;

        var baseNodes = await _snapshotReadRepositories.Hierarchy.ListBySnapshotAsync(baseSnapshotId, cancellationToken).ConfigureAwait(false);
        var baseRoles = await _snapshotReadRepositories.Roles.ListBySnapshotAsync(baseSnapshotId, cancellationToken).ConfigureAwait(false);
        var baseResolutions = await _snapshotReadRepositories.Roles.ListResolutionsBySnapshotAsync(baseSnapshotId, cancellationToken).ConfigureAwait(false);
        var baseContents = await _snapshotReadRepositories.Contents.ListBySnapshotAsync(baseSnapshotId, cancellationToken).ConfigureAwait(false);
        var baseDependencies = await _snapshotReadRepositories.Dependencies.ListBySnapshotAsync(baseSnapshotId, cancellationToken).ConfigureAwait(false);

        var currentNodes = await _snapshotReadRepositories.Hierarchy.ListBySnapshotAsync(currentSnapshot.SnapshotId, cancellationToken).ConfigureAwait(false);
        var currentRoles = await _snapshotReadRepositories.Roles.ListBySnapshotAsync(currentSnapshot.SnapshotId, cancellationToken).ConfigureAwait(false);
        var currentResolutions = await _snapshotReadRepositories.Roles.ListResolutionsBySnapshotAsync(currentSnapshot.SnapshotId, cancellationToken).ConfigureAwait(false);
        var currentContents = await _snapshotReadRepositories.Contents.ListBySnapshotAsync(currentSnapshot.SnapshotId, cancellationToken).ConfigureAwait(false);
        var currentDependencies = await _snapshotReadRepositories.Dependencies.ListBySnapshotAsync(currentSnapshot.SnapshotId, cancellationToken).ConfigureAwait(false);

        var baseData = new SnapshotData(baseNodes, baseRoles, baseResolutions, baseContents, baseDependencies);
        var currentData = new SnapshotData(currentNodes, currentRoles, currentResolutions, currentContents, currentDependencies);

        var diff = SnapshotDiffCalculator.Compute(new SnapshotDiffCalculationRequest(
            baseSnapshotId,
            currentSnapshot.SnapshotId,
            baseData,
            currentData,
            Limit: int.MaxValue,
            Offset: 0,
            ChangeVersion: null));

        var changes = new Dictionary<NodeId, RecentNodeChange>();

        foreach (var nodeEntry in diff.Nodes)
        {
            var side = nodeEntry.After ?? nodeEntry.Before!;
            changes[side.NodeId] = new RecentNodeChange(side.NodeId, side.Title, nodeEntry.Kind);
        }

        foreach (var contentEntry in diff.Contents)
        {
            var side = contentEntry.After ?? contentEntry.Before!;
            if (!changes.ContainsKey(side.NodeId))
            {
                var title = currentData.Nodes.FirstOrDefault(n => n.NodeId == side.NodeId)?.Title
                    ?? baseData.Nodes.FirstOrDefault(n => n.NodeId == side.NodeId)?.Title
                    ?? side.NodeId.ToString();

                changes[side.NodeId] = new RecentNodeChange(side.NodeId, title, DiffChangeKind.Modified);
            }
        }

        return changes.Values.OrderBy(c => c.Title, StringComparer.Ordinal).ToArray();
    }
}
