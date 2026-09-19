using System.Globalization;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
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

    private SnapshotPageViewModel? _snapshots;
    private ReleasePageViewModel? _releases;
    private string? _snapshotErrorMessage;
    private string? _releaseErrorMessage;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));
        await Task.WhenAll(LoadSnapshotPageAsync(cursor: null), LoadReleasePageAsync(cursor: null));
        _isLoading = false;
    }

    private Task LoadNextSnapshotPageAsync() => LoadSnapshotPageAsync(_snapshots?.NextCursor);

    private Task LoadNextReleasePageAsync() => LoadReleasePageAsync(_releases?.NextCursor);

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
