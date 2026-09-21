using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Audiences;

/// <summary>
/// Lädt und projiziert Zielgruppen sowie deren Working-Transaction-Mutationen.
/// </summary>
public sealed partial class AudienceEditor : ComponentBase
{
    [Inject]
    private NavigationService NavigationService { get; set; } = default!;

    [Inject]
    private AudienceMutationService AudienceMutationService { get; set; } = default!;

    [Parameter, EditorRequired]
    public ReadContext ReadContext { get; set; } = new();

    [Parameter]
    public long? ChangeVersion { get; set; }

    [Parameter]
    public EventCallback<long> MutationSucceeded { get; set; }

    private ReadContext? _loadedReadContext;
    private long? _loadedChangeVersion;
    private long? _changeVersion;
    private IReadOnlyList<AudienceItemViewModel> _audiences = [];
    private string? _audiencesErrorMessage;
    private string? _mutationErrorMessage;
    private bool _isLoading;
    private bool _isSubmitting;
    private string? _editingAudienceId;
    private AudienceItemViewModel? _pendingDeleteAudience;
    private bool _hasLoaded;

    internal string NewAudienceName { get; set; } = string.Empty;

    internal string NewAudienceDescription { get; set; } = string.Empty;

    internal string EditAudienceName { get; set; } = string.Empty;

    internal string EditAudienceDescription { get; set; } = string.Empty;

    private bool IsWorkingTransaction => ReadContext.TransactionId.HasValue;

    private bool CanMutate => IsWorkingTransaction && !_isLoading && !_isSubmitting;

    private long ExpectedChangeVersion => _changeVersion ?? 0;

    private string DeleteDialogTitle => _pendingDeleteAudience is null
        ? "Zielgruppe löschen"
        : $"Zielgruppe „{_pendingDeleteAudience.Name}“ löschen?";

    protected override async Task OnParametersSetAsync()
    {
        if (_hasLoaded && !ContextChanged())
            return;

        _loadedReadContext = ReadContext;
        _loadedChangeVersion = ChangeVersion;
        _changeVersion = ChangeVersion;
        _audiencesErrorMessage = null;
        _mutationErrorMessage = null;
        _editingAudienceId = null;
        _pendingDeleteAudience = null;
        _audiences = [];
        _isLoading = true;
        await LoadAudiencesAsync();
        _isLoading = false;
        _hasLoaded = true;
    }

    private async Task LoadAudiencesAsync()
    {
        var audiences = new List<AudienceItemViewModel>();
        string? cursor = null;

        do
        {
            var result = await NavigationService.ListAudiencesAsync(
                new ListAudiencesQuery(ReadContext, Limit: 100, Cursor: cursor),
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _audiencesErrorMessage = AudienceMapper.ToErrorMessage(result.Error!);
                return;
            }

            var page = AudienceMapper.ToAudiencePageViewModel(result.Value!);
            audiences.AddRange(page.Items);
            if (page.ChangeVersion is { } changeVersion)
                _changeVersion = changeVersion;
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        _audiences = audiences;
    }

    private async Task CreateAudienceAsync()
    {
        if (!CanMutate)
            return;

        _isSubmitting = true;
        _mutationErrorMessage = null;
        try
        {
            var result = await AudienceMutationService.CreateAudienceMutationAsync(
                ReadContext.TransactionId!.Value,
                NewAudienceName,
                NullIfWhiteSpace(NewAudienceDescription),
                ExpectedChangeVersion,
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _mutationErrorMessage = AudienceMapper.ToErrorMessage(result.Error!);
                return;
            }

            var mutation = result.Value!;
            ProjectCreated(mutation);
            NewAudienceName = string.Empty;
            NewAudienceDescription = string.Empty;
            await NotifyMutationSucceededAsync(mutation.ChangeVersion);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void BeginEdit(AudienceItemViewModel audience)
    {
        if (!CanMutate)
            return;

        _editingAudienceId = audience.AudienceId;
        EditAudienceName = audience.Name;
        EditAudienceDescription = audience.Description ?? string.Empty;
        _mutationErrorMessage = null;
    }

    private void CancelEdit()
    {
        _editingAudienceId = null;
        _mutationErrorMessage = null;
    }

    private async Task UpdateAudienceAsync(AudienceItemViewModel audience)
    {
        if (!CanMutate)
            return;

        _isSubmitting = true;
        _mutationErrorMessage = null;
        try
        {
            var result = await AudienceMutationService.UpdateAudienceMutationAsync(
                ReadContext.TransactionId!.Value,
                new UpdateAudienceMutationRequest(
                    new AudienceId(audience.AudienceId),
                    EditAudienceName,
                    NullIfWhiteSpace(EditAudienceDescription),
                    ExpectedChangeVersion),
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _mutationErrorMessage = AudienceMapper.ToErrorMessage(result.Error!);
                return;
            }

            var mutation = result.Value!;
            ProjectUpdated(mutation);
            _editingAudienceId = null;
            await NotifyMutationSucceededAsync(mutation.ChangeVersion);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task OpenDeleteConfirmationAsync(AudienceItemViewModel audience)
    {
        if (!CanMutate)
            return;

        _pendingDeleteAudience = audience;
        _mutationErrorMessage = null;
        await InvokeAsync(StateHasChanged);
    }

    private void CancelDelete() => _pendingDeleteAudience = null;

    private async Task DeleteAudienceAsync()
    {
        if (!CanMutate || _pendingDeleteAudience is null)
            return;

        _isSubmitting = true;
        _mutationErrorMessage = null;
        try
        {
            var result = await AudienceMutationService.DeleteAudienceMutationAsync(
                ReadContext.TransactionId!.Value,
                new AudienceId(_pendingDeleteAudience.AudienceId),
                ExpectedChangeVersion,
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _mutationErrorMessage = AudienceMapper.ToErrorMessage(result.Error!);
                return;
            }

            var mutation = result.Value!;
            ProjectDeleted(mutation);
            _pendingDeleteAudience = null;
            await NotifyMutationSucceededAsync(mutation.ChangeVersion);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task NotifyMutationSucceededAsync(long changeVersion)
    {
        _changeVersion = changeVersion;
        _loadedChangeVersion = changeVersion;
        await MutationSucceeded.InvokeAsync(changeVersion);
    }

    private void ProjectCreated(AudienceMutationResult mutation)
    {
        var projected = AudienceMapper.ToAudienceItemViewModel(mutation.Audience);
        _audiences = _audiences.Append(projected)
            .OrderBy(audience => audience.AudienceId, StringComparer.Ordinal)
            .ToArray();
    }

    private void ProjectUpdated(AudienceMutationResult mutation)
    {
        var projected = AudienceMapper.ToAudienceItemViewModel(mutation.Audience);
        _audiences = _audiences.Select(audience => audience.AudienceId == projected.AudienceId ? projected : audience).ToArray();
    }

    private void ProjectDeleted(AudienceMutationResult mutation) =>
        _audiences = _audiences.Where(audience => audience.AudienceId != mutation.AudienceId.Value).ToArray();

    private bool ContextChanged() =>
        !Equals(_loadedReadContext?.TransactionId, ReadContext.TransactionId)
        || !Equals(_loadedReadContext?.SnapshotId, ReadContext.SnapshotId)
        || _loadedReadContext?.IncludeDeleted != ReadContext.IncludeDeleted
        || _loadedChangeVersion != ChangeVersion;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

}
