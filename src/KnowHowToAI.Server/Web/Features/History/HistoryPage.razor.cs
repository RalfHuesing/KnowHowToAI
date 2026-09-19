using System.Globalization;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Shared.Diffs;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Orchestriert die getrennten, paginierten Übersichten für Historie und Releases.</summary>
public sealed partial class HistoryPage
{
    [Inject]
    private HistoryService HistoryService { get; set; } = default!;

    [Inject]
    private ReleaseService ReleaseService { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "roleId")]
    private string? QueryRoleId { get; set; }

    [SupplyParameterFromQuery(Name = "baseSnapshotId")]
    private string? QueryBaseSnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "targetSnapshotId")]
    private string? QueryTargetSnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "nodeId")]
    private string? QueryNodeId { get; set; }

    private SnapshotPageViewModel? _snapshots;
    private ReleasePageViewModel? _releases;
    private string? _snapshotErrorMessage;
    private string? _releaseErrorMessage;
    private SnapshotDiffViewModel? _diff;
    private long? _baseSnapshotId;
    private long? _targetSnapshotId;
    private Guid? _nodeFilterId;
    private string? _diffErrorMessage;
    private bool _isLoadingDiff;
    private bool _isLoading = true;
    private CreateReleaseDialog? _createReleaseDialog;
    private CreateReleaseViewModel? _createdRelease;

    protected override async Task OnInitializedAsync()
    {
        PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));
        InitializeComparisonFromQuery();
        await Task.WhenAll(LoadSnapshotPageAsync(cursor: null), LoadReleasePageAsync(cursor: null));
        if (HasSnapshotComparison)
            await LoadDiffPageAsync(cursor: null);
        _isLoading = false;
    }

    private Task LoadNextSnapshotPageAsync() => LoadSnapshotPageAsync(_snapshots?.NextCursor);

    private Task LoadNextReleasePageAsync() => LoadReleasePageAsync(_releases?.NextCursor);

    private Task LoadNextDiffPageAsync() => LoadDiffPageAsync(_diff?.NextCursor);

    private async Task OpenCreateReleaseDialogAsync()
    {
        _createdRelease = null;
        if (_createReleaseDialog is not null)
            await _createReleaseDialog.OpenAsync();
    }

    private async Task ReleaseCreatedAsync(CreateReleaseViewModel release)
    {
        _createdRelease = release;
        await LoadReleasePageAsync(cursor: null);
    }

    private async Task SelectBaseSnapshotAsync(long snapshotId)
    {
        _baseSnapshotId = snapshotId;
        await RefreshDiffAfterSelectionAsync();
    }

    private async Task SelectTargetSnapshotAsync(long snapshotId)
    {
        _targetSnapshotId = snapshotId;
        await RefreshDiffAfterSelectionAsync();
    }

    private async Task LoadSnapshotPageAsync(string? cursor)
    {
        _snapshotErrorMessage = null;
        var result = await HistoryService.ListCommittedSnapshotsAsync(limit: null, cursor, CancellationToken.None);
        var mapped = HistoryMapper.ToSnapshotPageResult(result);
        if (mapped.IsSuccess)
        {
            _snapshots = mapped.Value;
        }
        else
        {
            _snapshotErrorMessage = mapped.Error!.Message;
        }
    }

    private async Task LoadReleasePageAsync(string? cursor)
    {
        _releaseErrorMessage = null;
        var result = await ReleaseService.ListReleasesAsync(limit: null, cursor, CancellationToken.None);
        var mapped = HistoryMapper.ToReleasePageResult(result);
        if (mapped.IsSuccess)
        {
            _releases = mapped.Value;
        }
        else
        {
            _releaseErrorMessage = mapped.Error!.Message;
        }
    }

    private async Task RefreshDiffAfterSelectionAsync()
    {
        _diff = null;
        _diffErrorMessage = null;
        if (!HasSnapshotComparison)
            return;

        if (_baseSnapshotId == _targetSnapshotId)
        {
            _diffErrorMessage = "Ausgangs- und Ziel-Snapshot müssen unterschiedlich sein.";
            return;
        }

        await LoadDiffPageAsync(cursor: null);
    }

    private async Task LoadDiffPageAsync(string? cursor)
    {
        if (!HasSnapshotComparison || _baseSnapshotId == _targetSnapshotId)
            return;

        _isLoadingDiff = true;
        _diffErrorMessage = null;
        var result = await HistoryService.CompareSnapshotsAsync(
            new SnapshotComparisonQuery(
                new SnapshotId(_baseSnapshotId!.Value),
                new SnapshotId(_targetSnapshotId!.Value),
                Limit: null,
                Cursor: cursor,
                FilterNodeId: _nodeFilterId is { } nodeId ? new NodeId(nodeId) : null),
            CancellationToken.None);
        var mapped = HistoryMapper.ToSnapshotDiffResult(result);
        _isLoadingDiff = false;
        if (mapped.IsSuccess)
        {
            _diff = mapped.Value;
        }
        else
        {
            _diff = null;
            _diffErrorMessage = mapped.Error!.Message;
        }
    }

    private bool HasSnapshotComparison => _baseSnapshotId.HasValue && _targetSnapshotId.HasValue;

    private void InitializeComparisonFromQuery()
    {
        _baseSnapshotId = TryParseSnapshotId(QueryBaseSnapshotId, "Ausgangs-Snapshot");
        _targetSnapshotId = TryParseSnapshotId(QueryTargetSnapshotId, "Ziel-Snapshot");
        if (!string.IsNullOrWhiteSpace(QueryNodeId))
        {
            if (Guid.TryParse(QueryNodeId, out var nodeId))
                _nodeFilterId = nodeId;
            else
                _diffErrorMessage = "Der Knotenfilter ist ungültig.";
        }
    }

    private long? TryParseSnapshotId(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var snapshotId) && snapshotId > 0)
            return snapshotId;

        _diffErrorMessage = $"Der {label} ist ungültig.";
        return null;
    }

    private void NavigateToSnapshot(long snapshotId) =>
        NavigationManager.NavigateTo(BuildKnowledgeUrl("snapshotId", snapshotId.ToString(CultureInfo.InvariantCulture)));

    private void NavigateToRelease(long releaseId) =>
        NavigationManager.NavigateTo(BuildKnowledgeUrl("releaseId", releaseId.ToString(CultureInfo.InvariantCulture)));

    private string BuildKnowledgeUrl(string contextName, string contextValue)
    {
        var roleQuery = string.IsNullOrWhiteSpace(QueryRoleId)
            ? string.Empty
            : $"&roleId={Uri.EscapeDataString(QueryRoleId)}";
        return $"/knowledge?{contextName}={Uri.EscapeDataString(contextValue)}{roleQuery}";
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private static string FormatUtc(DateTimeOffset timestamp) => timestamp.ToString("O", CultureInfo.InvariantCulture);
}
