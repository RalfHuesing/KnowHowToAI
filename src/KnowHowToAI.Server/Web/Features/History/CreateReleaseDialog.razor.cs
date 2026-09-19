using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Erfasst die Metadaten eines Releases und übergibt ihn an den bestehenden Release-Use-Case.</summary>
public sealed partial class CreateReleaseDialog : ComponentBase
{
    private static int _instanceCounter;

    private readonly string _snapshotId = $"create-release-snapshot-{Interlocked.Increment(ref _instanceCounter)}";
    private readonly string _nameId = $"create-release-name-{Interlocked.Increment(ref _instanceCounter)}";
    private readonly string _descriptionId = $"create-release-description-{Interlocked.Increment(ref _instanceCounter)}";
    private AppDialog? _dialog;
    private string? _selectedSnapshotId;
    private string? _name;
    private string? _description;
    private string? _errorMessage;
    private bool _isSubmitting;

    [Inject]
    private ReleaseService ReleaseService { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<SnapshotViewModel> Snapshots { get; set; } = [];

    [Parameter]
    public EventCallback<CreateReleaseViewModel> OnCreated { get; set; }

    private IEnumerable<SnapshotViewModel> CommittedSnapshots =>
        Snapshots.Where(static snapshot => string.Equals(snapshot.State, SnapshotState.Committed.ToString(), StringComparison.Ordinal));

    public async Task OpenAsync()
    {
        _selectedSnapshotId = null;
        _name = null;
        _description = null;
        _errorMessage = null;
        _isSubmitting = false;

        if (_dialog is not null)
            await _dialog.OpenAsync();
    }

    private async Task CreateAsync()
    {
        if (!TryGetSelectedSnapshot(out var snapshotId))
            return;

        _isSubmitting = true;
        _errorMessage = null;
        var result = await ReleaseService.CreateReleaseAsync(
            _name ?? string.Empty,
            new SnapshotId(snapshotId),
            _description,
            CancellationToken.None);
        _isSubmitting = false;

        if (!result.IsSuccess)
        {
            _errorMessage = result.Error!.Message;
            return;
        }

        await OnCreated.InvokeAsync(HistoryMapper.ToCreateReleaseViewModel(result.Value!));
        if (_dialog is not null)
            await _dialog.CloseAsync();
    }

    private bool TryGetSelectedSnapshot(out long snapshotId)
    {
        snapshotId = default;
        if (!long.TryParse(_selectedSnapshotId, out var selectedSnapshotId)
            || !CommittedSnapshots.Any(snapshot => snapshot.SnapshotId == selectedSnapshotId))
        {
            _errorMessage = "Bitte wählen Sie einen committed Snapshot aus.";
            return false;
        }

        snapshotId = selectedSnapshotId;
        return true;
    }

    private void UpdateSelectedSnapshot(ChangeEventArgs eventArgs) =>
        _selectedSnapshotId = eventArgs.Value?.ToString();

    private void UpdateName(ChangeEventArgs eventArgs) =>
        _name = eventArgs.Value?.ToString();

    private void UpdateDescription(ChangeEventArgs eventArgs) =>
        _description = eventArgs.Value?.ToString();

    private async Task CancelAsync()
    {
        if (_dialog is not null)
            await _dialog.CloseAsync();
    }

    private static Task DismissAsync() => Task.CompletedTask;
}
