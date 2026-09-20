using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Roles;

/// <summary>
/// Lädt und projiziert Rollen sowie deren Working-Transaction-Mutationen.
/// </summary>
public sealed partial class RoleEditor : ComponentBase
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
    private IReadOnlyList<RoleItemViewModel> _roles = [];
    private string? _rolesErrorMessage;
    private string? _mutationErrorMessage;
    private bool _isLoading;
    private bool _isSubmitting;
    private string? _editingRoleId;
    private RoleItemViewModel? _pendingDeleteRole;
    private bool _hasLoaded;

    internal string NewRoleName { get; set; } = string.Empty;

    internal string NewRoleDescription { get; set; } = string.Empty;

    internal string EditRoleName { get; set; } = string.Empty;

    internal string EditRoleDescription { get; set; } = string.Empty;

    private bool IsWorkingTransaction => ReadContext.TransactionId.HasValue;

    private bool CanMutate => IsWorkingTransaction && !_isLoading && !_isSubmitting;

    private long ExpectedChangeVersion => _changeVersion ?? 0;

    protected override async Task OnParametersSetAsync()
    {
        if (_hasLoaded && !ContextChanged())
            return;

        _loadedReadContext = ReadContext;
        _loadedChangeVersion = ChangeVersion;
        _changeVersion = ChangeVersion;
        _rolesErrorMessage = null;
        _mutationErrorMessage = null;
        _editingRoleId = null;
        _pendingDeleteRole = null;
        _roles = [];
        _isLoading = true;
        await LoadRolesAsync();
        _isLoading = false;
        _hasLoaded = true;
    }

    private async Task LoadRolesAsync()
    {
        var roles = new List<RoleItemViewModel>();
        string? cursor = null;

        do
        {
            var result = await NavigationService.ListAudiencesAsync(
                new ListAudiencesQuery(ReadContext, Limit: 100, Cursor: cursor),
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _rolesErrorMessage = RoleMapper.ToErrorMessage(result.Error!);
                return;
            }

            var page = RoleMapper.ToRolePageViewModel(result.Value!);
            roles.AddRange(page.Items);
            if (page.ChangeVersion is { } changeVersion)
                _changeVersion = changeVersion;
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        _roles = roles;
    }

    private async Task CreateRoleAsync()
    {
        if (!CanMutate)
            return;

        _isSubmitting = true;
        _mutationErrorMessage = null;
        try
        {
            var result = await AudienceMutationService.CreateAudienceMutationAsync(
                ReadContext.TransactionId!.Value,
                NewRoleName,
                NullIfWhiteSpace(NewRoleDescription),
                ExpectedChangeVersion,
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _mutationErrorMessage = RoleMapper.ToErrorMessage(result.Error!);
                return;
            }

            var mutation = result.Value!;
            ProjectCreated(mutation);
            NewRoleName = string.Empty;
            NewRoleDescription = string.Empty;
            await NotifyMutationSucceededAsync(mutation.ChangeVersion);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void BeginEdit(RoleItemViewModel role)
    {
        if (!CanMutate)
            return;

        _editingRoleId = role.RoleId;
        EditRoleName = role.Name;
        EditRoleDescription = role.Description ?? string.Empty;
        _mutationErrorMessage = null;
    }

    private void CancelEdit()
    {
        _editingRoleId = null;
        _mutationErrorMessage = null;
    }

    private async Task UpdateRoleAsync(RoleItemViewModel role)
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
                    new AudienceId(role.RoleId),
                    EditRoleName,
                    NullIfWhiteSpace(EditRoleDescription),
                    ExpectedChangeVersion),
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _mutationErrorMessage = RoleMapper.ToErrorMessage(result.Error!);
                return;
            }

            var mutation = result.Value!;
            ProjectUpdated(mutation);
            _editingRoleId = null;
            await NotifyMutationSucceededAsync(mutation.ChangeVersion);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private Task OpenDeleteConfirmationAsync(RoleItemViewModel role)
    {
        if (!CanMutate)
            return Task.CompletedTask;

        _pendingDeleteRole = role;
        _mutationErrorMessage = null;
        return Task.CompletedTask;
    }

    private void CancelDelete() => _pendingDeleteRole = null;

    private async Task DeleteRoleAsync()
    {
        if (!CanMutate || _pendingDeleteRole is null)
            return;

        _isSubmitting = true;
        _mutationErrorMessage = null;
        try
        {
            var result = await AudienceMutationService.DeleteAudienceMutationAsync(
                ReadContext.TransactionId!.Value,
                new AudienceId(_pendingDeleteRole.RoleId),
                ExpectedChangeVersion,
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _mutationErrorMessage = RoleMapper.ToErrorMessage(result.Error!);
                return;
            }

            var mutation = result.Value!;
            ProjectDeleted(mutation);
            _pendingDeleteRole = null;
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
        var projected = RoleMapper.ToRoleItemViewModel(mutation.Audience);
        _roles = _roles.Append(projected)
            .OrderBy(role => role.RoleId, StringComparer.Ordinal)
            .ToArray();
    }

    private void ProjectUpdated(AudienceMutationResult mutation)
    {
        var projected = RoleMapper.ToRoleItemViewModel(mutation.Audience);
        _roles = _roles.Select(role => role.RoleId == projected.RoleId ? projected : role).ToArray();
    }

    private void ProjectDeleted(AudienceMutationResult mutation) =>
        _roles = _roles.Where(role => role.RoleId != mutation.AudienceId.Value).ToArray();

    private bool ContextChanged() =>
        !Equals(_loadedReadContext?.TransactionId, ReadContext.TransactionId)
        || !Equals(_loadedReadContext?.SnapshotId, ReadContext.SnapshotId)
        || _loadedReadContext?.IncludeDeleted != ReadContext.IncludeDeleted
        || _loadedChangeVersion != ChangeVersion;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

}
