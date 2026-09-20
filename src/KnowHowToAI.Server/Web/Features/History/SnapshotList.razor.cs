using System.Globalization;
using Microsoft.AspNetCore.Components;
using KnowHowToAI.Core.Application.History;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Zeigt und paginiert committed Snapshots und meldet die Vergleichsauswahl an die Seite.</summary>
public sealed partial class SnapshotList
{
    [Inject]
    private HistoryService HistoryService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public string? RoleId { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<SnapshotViewModel>> OnSnapshotsChanged { get; set; }

    [Parameter]
    public EventCallback<long> OnBaseSnapshotSelected { get; set; }

    [Parameter]
    public EventCallback<long> OnTargetSnapshotSelected { get; set; }

    private SnapshotPageViewModel? _page;
    private string? _errorMessage;
    private bool _isLoading;

    protected override async Task OnInitializedAsync() => await LoadPageAsync(cursor: null);

    private Task LoadNextPageAsync() => LoadPageAsync(_page?.NextCursor);

    private async Task LoadPageAsync(string? cursor)
    {
        _isLoading = true;
        _errorMessage = null;
        var result = await HistoryService.ListCommittedSnapshotsAsync(limit: null, cursor, CancellationToken.None);
        _isLoading = false;
        var mapped = HistoryMapper.ToSnapshotPageResult(result);
        if (!mapped.IsSuccess)
        {
            _errorMessage = mapped.Error!.Message;
            return;
        }

        var page = mapped.Value!;
        _page = page;
        await OnSnapshotsChanged.InvokeAsync(page.Items);
    }

    private void NavigateToSnapshot(long snapshotId)
    {
        var roleQuery = string.IsNullOrWhiteSpace(RoleId) ? string.Empty : $"&roleId={Uri.EscapeDataString(RoleId)}";
        NavigationManager.NavigateTo($"/knowledge?snapshotId={snapshotId.ToString(CultureInfo.InvariantCulture)}{roleQuery}");
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private static string FormatUtc(DateTimeOffset timestamp) => timestamp.ToString("O", CultureInfo.InvariantCulture);
}
