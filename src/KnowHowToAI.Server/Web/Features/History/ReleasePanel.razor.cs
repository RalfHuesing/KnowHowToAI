using System.Globalization;
using Microsoft.AspNetCore.Components;
using KnowHowToAI.Core.Application.History;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Verwaltet die Release-Liste, die Release-Erstellung und deren Erfolgsmeldung.</summary>
public sealed partial class ReleasePanel
{
    [Inject]
    private ReleaseService ReleaseService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<SnapshotViewModel> Snapshots { get; set; } = [];

    [Parameter]
    public string? AudienceId { get; set; }

    private ReleasePageViewModel? _page;
    private string? _errorMessage;
    private CreateReleaseDialog? _createReleaseDialog;
    private CreateReleaseViewModel? _createdRelease;

    protected override async Task OnInitializedAsync() => await LoadPageAsync(cursor: null);

    private Task LoadNextPageAsync() => LoadPageAsync(_page?.NextCursor);

    private async Task OpenCreateReleaseDialogAsync()
    {
        _createdRelease = null;
        if (_createReleaseDialog is not null)
            await _createReleaseDialog.OpenAsync();
    }

    private async Task ReleaseCreatedAsync(CreateReleaseViewModel release)
    {
        _createdRelease = release;
        await LoadPageAsync(cursor: null);
    }

    private async Task LoadPageAsync(string? cursor)
    {
        _errorMessage = null;
        var result = await ReleaseService.ListReleasesAsync(limit: null, cursor, CancellationToken.None);
        var mapped = HistoryMapper.ToReleasePageResult(result);
        if (mapped.IsSuccess)
            _page = mapped.Value;
        else
            _errorMessage = mapped.Error!.Message;
    }

    private void NavigateToRelease(long releaseId)
    {
        var audienceQuery = string.IsNullOrWhiteSpace(AudienceId) ? string.Empty : $"&audienceId={Uri.EscapeDataString(AudienceId)}";
        NavigationManager.NavigateTo($"/knowledge?releaseId={releaseId.ToString(CultureInfo.InvariantCulture)}{audienceQuery}");
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private static string FormatUtc(DateTimeOffset timestamp) => timestamp.ToString("O", CultureInfo.InvariantCulture);
}
