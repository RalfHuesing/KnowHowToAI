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
    private const int RecentChangesPageSize = 100;
    private const int OpenTransactionsPageSize = 100;

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

        var snapshotSummary = await GetSnapshotSummaryAsync(cancellationToken).ConfigureAwait(false);
        var openTransactionSummaries = await GetOpenTransactionsAsync(cancellationToken).ConfigureAwait(false);
        var currentQuality = await GetCurrentQualityAsync(snapshotSummary.CurrentSnapshot.SnapshotId, cancellationToken).ConfigureAwait(false);
        var recentNodeChanges = await GetRecentNodeChangesAsync(snapshotSummary.CurrentSnapshot, cancellationToken).ConfigureAwait(false);

        var result = new DashboardResult(
            snapshotSummary.CurrentSnapshot,
            snapshotSummary.LatestRelease,
            openTransactionSummaries,
            currentQuality,
            recentNodeChanges);

        return Result<DashboardResult>.Success(result);
    }

    /// <summary>Lädt ausschließlich die Snapshot- und Releasekarte des Dashboards.</summary>
    public async Task<DashboardSnapshotSummary> GetSnapshotSummaryAsync(CancellationToken cancellationToken = default)
    {
        var currentSnapshot = await GetCurrentSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var latestRelease = await _dashboardRepository.GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        return new DashboardSnapshotSummary(currentSnapshot, latestRelease);
    }

    /// <summary>Lädt ausschließlich den Current Snapshot für unabhängige Dashboardbereiche.</summary>
    public Task<Domain.Versioning.Snapshot> GetCurrentSnapshotAsync(CancellationToken cancellationToken = default) =>
        _snapshotRepository.GetCurrentAsync(cancellationToken);

    /// <summary>Lädt ausschließlich die offenen Transactions inklusive ihrer fachlichen Fehler.</summary>
    public async Task<IReadOnlyList<OpenTransactionSummary>> GetOpenTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var openTransactions = await _dashboardRepository.ListOpenTransactionsAsync(OpenTransactionsPageSize, cancellationToken).ConfigureAwait(false);
        var openTransactionSummaries = new List<OpenTransactionSummary>(openTransactions.Count);
        foreach (var tx in openTransactions)
        {
            var errors = await ValidateOpenTransactionErrorsAsync(tx.TransactionId, cancellationToken).ConfigureAwait(false);
            openTransactionSummaries.Add(new OpenTransactionSummary(tx, errors));
        }

        return openTransactionSummaries;
    }

    /// <summary>Lädt ausschließlich den Qualitätsbereich des aktuellen Snapshots.</summary>
    public Task<CurrentQualitySummary> GetCurrentQualityAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        EvaluateCurrentQualityAsync(snapshotId, cancellationToken);

    /// <summary>Lädt ausschließlich die jüngsten nodebezogenen Änderungen.</summary>
    public Task<IReadOnlyList<RecentNodeChange>> GetRecentNodeChangesAsync(
        Domain.Versioning.Snapshot currentSnapshot,
        CancellationToken cancellationToken = default) =>
        ComputeRecentNodeChangesAsync(currentSnapshot, cancellationToken);

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
            Limit: RecentChangesPageSize,
            Offset: 0,
            ChangeVersion: null));

        var changes = new Dictionary<NodeId, RecentNodeChange>();

        AddNodeChanges(changes, diff);
        AddContentChanges(changes, diff, baseData, currentData);
        AddDependencyChanges(changes, diff, baseData, currentData);

        return changes.Values.OrderBy(c => c.Title, StringComparer.Ordinal).ToArray();
    }

    private static void AddNodeChanges(Dictionary<NodeId, RecentNodeChange> changes, SnapshotDiff diff)
    {
        foreach (var nodeEntry in diff.Nodes)
        {
            var side = nodeEntry.After ?? nodeEntry.Before!;
            changes[side.NodeId] = new RecentNodeChange(side.NodeId, side.Title, nodeEntry.Kind);
        }
    }

    private static void AddContentChanges(
        Dictionary<NodeId, RecentNodeChange> changes,
        SnapshotDiff diff,
        SnapshotData baseData,
        SnapshotData currentData)
    {
        foreach (var contentEntry in diff.Contents)
        {
            var side = contentEntry.After ?? contentEntry.Before!;
            AddChangedNode(changes, side.NodeId, baseData, currentData);
        }
    }

    private static void AddDependencyChanges(
        Dictionary<NodeId, RecentNodeChange> changes,
        SnapshotDiff diff,
        SnapshotData baseData,
        SnapshotData currentData)
    {
        foreach (var dependencyEntry in diff.Dependencies)
        {
            var dependency = dependencyEntry.After ?? dependencyEntry.Before!;
            AddChangedNode(changes, dependency.SourceNodeId, baseData, currentData);
            AddChangedNode(changes, dependency.TargetNodeId, baseData, currentData);
        }
    }

    private static void AddChangedNode(
        Dictionary<NodeId, RecentNodeChange> changes,
        NodeId nodeId,
        SnapshotData baseData,
        SnapshotData currentData)
    {
        if (changes.ContainsKey(nodeId))
        {
            return;
        }

        var title = currentData.Nodes.FirstOrDefault(node => node.NodeId == nodeId)?.Title
            ?? baseData.Nodes.FirstOrDefault(node => node.NodeId == nodeId)?.Title
            ?? nodeId.ToString();
        changes[nodeId] = new RecentNodeChange(nodeId, title, DiffChangeKind.Modified);
    }
}
