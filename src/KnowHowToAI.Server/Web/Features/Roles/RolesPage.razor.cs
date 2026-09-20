using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Roles;

/// <summary>
/// Rollenadministration im explizit gewählten Read-Kontext. Nur ein offener
/// Working-Transaction-Kontext erlaubt Mutationen; Rollen bleiben Zielgruppen
/// und werden nicht als Authentifizierungs- oder ACL-Rollen behandelt.
/// </summary>
public sealed partial class RolesPage : ComponentBase
{
    [Inject]
    private NavigationService NavigationService { get; set; } = default!;

    [Inject]
    private RoleMutationService RoleMutationService { get; set; } = default!;

    [Inject]
    private IWebReadContextResolver ReadContextResolver { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "transactionId")]
    private string? QueryTransactionId { get; set; }

    [SupplyParameterFromQuery(Name = "snapshotId")]
    private string? QuerySnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "releaseId")]
    private string? QueryReleaseId { get; set; }

    private ReadContext _readContext = new();
    private IReadOnlyList<RoleItemViewModel> _roles = [];
    private string? _contextErrorMessage;
    private string? _rolesErrorMessage;
    private string? _mutationErrorMessage;
    private bool _isLoading;
    private bool _isSubmitting;
    private string? _editingRoleId;
    private RoleItemViewModel? _pendingDeleteRole;

    internal string NewRoleName { get; set; } = string.Empty;

    internal string NewRoleDescription { get; set; } = string.Empty;

    internal string EditRoleName { get; set; } = string.Empty;

    internal string EditRoleDescription { get; set; } = string.Empty;

    private bool IsWorkingTransaction => _readContext.TransactionId.HasValue;

    private long? CurrentChangeVersion => WorkspaceState.CurrentChangeVersion;

    protected override async Task OnParametersSetAsync()
    {
        var previousTransactionId = WorkspaceState.ActiveTransactionId;
        _contextErrorMessage = null;
        _rolesErrorMessage = null;
        _mutationErrorMessage = null;
        _editingRoleId = null;
        _pendingDeleteRole = null;
        _roles = [];
        _isLoading = true;

        var resolution = await ReadContextResolver.ResolveAsync(
            QueryTransactionId,
            QuerySnapshotId,
            QueryReleaseId,
            CancellationToken.None);

        if (!resolution.IsSuccess)
        {
            _contextErrorMessage = ToErrorMessage(resolution.Error!);
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Ungültiger Kontext"));
            _isLoading = false;
            return;
        }

        var resolved = resolution.Value!;
        _readContext = resolved.ReadContext;
        var context = resolved.ContextViewModel with { ChangeVersion = resolved.ChangeVersion };
        PageRegions.SetKnowledgeContext(context);
        WorkspaceState.SetContext(context, _readContext);
        WorkspaceState.SetChangeVersion(resolved.ChangeVersion);
        if (previousTransactionId != resolved.ReadContext.TransactionId)
            WorkspaceState.SetDirty(false);

        await LoadRolesAsync();
        _isLoading = false;
    }

    private async Task LoadRolesAsync()
    {
        var roles = new List<RoleItemViewModel>();
        string? cursor = null;

        do
        {
            var result = await NavigationService.ListRolesAsync(
                new ListRolesQuery(_readContext, Limit: 100, Cursor: cursor),
                CancellationToken.None);
            if (!result.IsSuccess)
            {
                _rolesErrorMessage = ToErrorMessage(result.Error!);
                return;
            }

            var page = RoleMapper.ToRolePageViewModel(result.Value!);
            roles.AddRange(page.Items);
            if (page.ChangeVersion is { } changeVersion)
            {
                WorkspaceState.SetChangeVersion(changeVersion);
                PageRegions.SetKnowledgeContext(
                    WorkspaceState.CurrentContext with { ChangeVersion = changeVersion });
            }

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
            var result = await RoleMutationService.CreateRoleMutationAsync(
                _readContext.TransactionId!.Value,
                NewRoleName,
                NullIfWhiteSpace(NewRoleDescription),
                ExpectedChangeVersion,
                CancellationToken.None);
            await HandleMutationResultAsync(result);
            if (result.IsSuccess)
            {
                NewRoleName = string.Empty;
                NewRoleDescription = string.Empty;
            }
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
            var result = await RoleMutationService.UpdateRoleMutationAsync(
                _readContext.TransactionId!.Value,
                new UpdateRoleMutationRequest(
                    new RoleId(role.RoleId),
                    EditRoleName,
                    NullIfWhiteSpace(EditRoleDescription),
                    ExpectedChangeVersion),
                CancellationToken.None);
            await HandleMutationResultAsync(result);
            if (result.IsSuccess)
                _editingRoleId = null;
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
            var result = await RoleMutationService.DeleteRoleMutationAsync(
                _readContext.TransactionId!.Value,
                new RoleId(_pendingDeleteRole.RoleId),
                ExpectedChangeVersion,
                CancellationToken.None);
            await HandleMutationResultAsync(result);
            if (result.IsSuccess)
            {
                _pendingDeleteRole = null;
            }
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task HandleMutationResultAsync(Result<RoleMutationResult> result)
    {
        if (!result.IsSuccess)
        {
            _mutationErrorMessage = ToErrorMessage(result.Error!);
            return;
        }

        var mutation = result.Value!;
        WorkspaceState.SetChangeVersion(mutation.ChangeVersion);
        WorkspaceState.SetDirty(true);
        var context = WorkspaceState.CurrentContext with
        {
            IsDirty = true,
            ChangeVersion = mutation.ChangeVersion
        };
        WorkspaceState.SetContext(context, _readContext);
        PageRegions.SetKnowledgeContext(context);
        await LoadRolesAsync();
    }

    private bool CanMutate => IsWorkingTransaction && !_isLoading && !_isSubmitting;

    private long ExpectedChangeVersion => CurrentChangeVersion ?? 0;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string ToErrorMessage(DomainError error)
    {
        var details = error.Details.Count == 0
            ? string.Empty
            : $" ({string.Join(", ", error.Details.Select(pair => $"{pair.Key}={pair.Value}"))})";
        return $"[{error.Code}] {error.Message}{details}";
    }
}
