using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Dashboard;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Features.History;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class DashboardPage
{
    [Inject]
    private DashboardService DashboardService { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    [Inject]
    private ILogger<DashboardPage> Logger { get; set; } = default!;

    private bool _isLoading = true;
    private SnapshotViewModel? _currentSnapshot;
    private ReleaseItemViewModel? _latestRelease;
    private IReadOnlyList<OpenTransactionItemViewModel>? _openTransactions;
    private DashboardQualityViewModel? _qualitySummary;
    private IReadOnlyList<RecentNodeChangeViewModel>? _recentChanges;
    private DashboardAreaError? _snapshotError;
    private DashboardAreaError? _transactionsError;
    private DashboardAreaError? _qualityError;
    private DashboardAreaError? _changesError;

    protected override async Task OnInitializedAsync()
    {
        PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));
        _isLoading = true;
        await Task.WhenAll(LoadSnapshotAsync(), LoadTransactionsAsync(), LoadQualityAsync(), LoadChangesAsync());
        _isLoading = false;
    }

    private async Task LoadSnapshotAsync()
    {
        _snapshotError = null;
        try
        {
            var summary = await DashboardService.GetSnapshotSummaryAsync();
            _currentSnapshot = HistoryMapper.ToSnapshotViewModel(summary.CurrentSnapshot);
            _latestRelease = summary.LatestRelease is null ? null : HistoryMapper.ToReleaseItemViewModel(summary.LatestRelease);
        }
        catch (Exception exception)
        {
            _snapshotError = CreateTechnicalError("Snapshot und Release", exception);
        }
    }

    private async Task LoadTransactionsAsync()
    {
        _transactionsError = null;
        try
        {
            var transactions = await DashboardService.GetOpenTransactionsAsync();
            _openTransactions = transactions.Select(transaction =>
                DashboardMapper.ToOpenTransactionItemViewModel(transaction, Clock.UtcNow)).ToArray();
        }
        catch (Exception exception)
        {
            _transactionsError = CreateTechnicalError("Offene Transactions", exception);
        }
    }

    private async Task LoadQualityAsync()
    {
        _qualityError = null;
        try
        {
            var currentSnapshot = await DashboardService.GetCurrentSnapshotAsync();
            var quality = await DashboardService.GetCurrentQualityAsync(currentSnapshot.SnapshotId);
            _qualitySummary = DashboardMapper.ToQualityViewModel(quality);
        }
        catch (Exception exception)
        {
            _qualityError = CreateTechnicalError("Qualitätsübersicht", exception);
        }
    }

    private async Task LoadChangesAsync()
    {
        _changesError = null;
        try
        {
            var currentSnapshot = await DashboardService.GetCurrentSnapshotAsync();
            var changes = await DashboardService.GetRecentNodeChangesAsync(currentSnapshot);
            _recentChanges = changes.Select(DashboardMapper.ToRecentNodeChangeViewModel).ToArray();
        }
        catch (Exception exception)
        {
            _changesError = CreateTechnicalError("Zuletzt geänderte Nodes", exception);
        }
    }

    private DashboardAreaError CreateTechnicalError(string area, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        Logger.LogError(exception, "Dashboardbereich {DashboardArea} konnte nicht geladen werden. CorrelationId: {CorrelationId}", area, correlationId);
        return new DashboardAreaError("DashboardTechnicalError", correlationId);
    }

    private string _interactionStatus = "Interaktivität wurde noch nicht geprüft.";

    private void CheckInteractivity() =>
        _interactionStatus = "Interaktivität ist verfügbar.";

}
