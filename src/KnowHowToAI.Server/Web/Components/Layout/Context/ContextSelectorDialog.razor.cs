using System.Globalization;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

public sealed partial class ContextSelectorDialog : ComponentBase, IAsyncDisposable
{
    private AppDialog? _dialog;
    private KnowledgeReadContextKind _selectedKind = KnowledgeReadContextKind.Current;
    private string? _snapshotIdInput;
    private string? _releaseIdInput;
    private string? _selectedReleaseId;
    private string? _transactionIdInput;
    private string? _selectedTransactionId;
    private string? _selectedRoleId;
    private string? _errorMessage;
    private bool _isLoadingRoles;
    private bool _isOpen;

    private readonly List<RoleOption> _availableRoles = [];
    private readonly List<ReleaseOption> _availableReleases = [];
    private readonly List<TransactionOption> _availableTransactions = [];

    [Inject]
    private ContextSelectorState State { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    private NavigationService? NavigationService => ServiceProvider.GetService<NavigationService>();

    private IRoleStorageService? RoleStorage => ServiceProvider.GetService<IRoleStorageService>();

    private ILogger<ContextSelectorDialog>? Logger => ServiceProvider.GetService<ILogger<ContextSelectorDialog>>();

    private ReleaseService? ReleaseService => ServiceProvider.GetService<ReleaseService>();

    private IDashboardRepository? DashboardRepository => ServiceProvider.GetService<IDashboardRepository>();

    internal string DialogTitle => State.Mode == ContextSelectorMode.MandatoryRole
        ? "Rolle auswählen"
        : "Wissenskontext und Rolle anpassen";

    protected override void OnInitialized()
    {
        State.Changed += HandleStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (State.IsOpen && !_isOpen)
        {
            _isOpen = true;
            await InitializeAndOpenAsync();
        }
        else if (!State.IsOpen && _isOpen)
        {
            _isOpen = false;
            if (_dialog is not null)
            {
                await _dialog.CloseAsync();
            }
        }
    }

    private void HandleStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task InitializeAndOpenAsync()
    {
        _errorMessage = null;
        _availableReleases.Clear();
        _availableTransactions.Clear();

        ApplyInitialContext(State.InitialReadContext);
        _selectedRoleId = State.InitialRoleId;

        if (State.Mode == ContextSelectorMode.Full)
        {
            await LoadReleasesAndTransactionsAsync();
        }

        await LoadRolesForCurrentSelectionAsync();

        if (_dialog is not null)
        {
            await _dialog.OpenAsync();
        }

        StateHasChanged();
    }

    private void ApplyInitialContext(ReadContext? initialContext)
    {
        if (initialContext is null)
        {
            _selectedKind = KnowledgeReadContextKind.Current;
            return;
        }

        if (initialContext.TransactionId is { } txId)
        {
            _selectedKind = KnowledgeReadContextKind.Transaction;
            _selectedTransactionId = txId.Value.ToString("D");
            _transactionIdInput = _selectedTransactionId;
        }
        else if (initialContext.SnapshotId is { } snapId)
        {
            _selectedKind = KnowledgeReadContextKind.Snapshot;
            _snapshotIdInput = snapId.Value.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            _selectedKind = KnowledgeReadContextKind.Current;
        }
    }

    private async Task LoadReleasesAndTransactionsAsync()
    {
        if (ReleaseService is not null)
        {
            try
            {
                var releasesResult = await ReleaseService.ListReleasesAsync(limit: 50, cursor: null, CancellationToken.None);
                if (releasesResult.IsSuccess && releasesResult.Value is { } page)
                {
                    _availableReleases.AddRange(page.Items.Select(r => new ReleaseOption(
                        r.ReleaseId.Value.ToString(CultureInfo.InvariantCulture),
                        r.Name,
                        r.SnapshotId.Value)));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Releases konnten im Selektor nicht geladen werden.");
            }
        }

        if (DashboardRepository is not null)
        {
            try
            {
                var openTxs = await DashboardRepository.ListOpenTransactionsAsync(CancellationToken.None);
                _availableTransactions.AddRange(openTxs.Select(t => new TransactionOption(
                    t.TransactionId.Value.ToString("D"),
                    $"{t.Purpose} ({t.Actor})")));
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Offene Transaktionen konnten im Selektor nicht geladen werden.");
            }
        }
    }

    internal async Task SelectKindAsync(KnowledgeReadContextKind kind)
    {
        if (_selectedKind == kind)
            return;

        _selectedKind = kind;
        _errorMessage = null;
        await LoadRolesForCurrentSelectionAsync();
    }

    internal async Task OnContextParameterChangedAsync()
    {
        _errorMessage = null;
        await LoadRolesForCurrentSelectionAsync();
    }

    private async Task LoadRolesForCurrentSelectionAsync()
    {
        _isLoadingRoles = true;
        _availableRoles.Clear();
        StateHasChanged();

        if (NavigationService is null)
        {
            _isLoadingRoles = false;
            return;
        }

        try
        {
            var readContextResult = BuildCurrentReadContext();
            if (!readContextResult.IsSuccess)
            {
                _isLoadingRoles = false;
                return;
            }

            var rolesResult = await NavigationService.ListRolesAsync(
                new ListRolesQuery(readContextResult.Value!, Limit: 100),
                CancellationToken.None);

            if (rolesResult.IsSuccess && rolesResult.Value is { } rolePage)
            {
                _availableRoles.AddRange(rolePage.Items.Select(r => new RoleOption(
                    r.RoleId.Value,
                    r.Name,
                    r.Description)));

                if (_selectedRoleId is not null && !_availableRoles.Any(r => r.Id == _selectedRoleId))
                {
                    _selectedRoleId = null;
                }
            }
            else
            {
                _errorMessage = rolesResult.Error?.Message ?? "Rollen konnten für den gewählten Kontext nicht geladen werden.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = "Fehler beim Laden der Rollen: " + ex.Message;
        }
        finally
        {
            _isLoadingRoles = false;
            StateHasChanged();
        }
    }

    private Result<ReadContext> BuildCurrentReadContext() => _selectedKind switch
    {
        KnowledgeReadContextKind.Snapshot => BuildSnapshotContext(),
        KnowledgeReadContextKind.Release => BuildReleaseContext(),
        KnowledgeReadContextKind.Transaction => BuildTransactionContext(),
        _ => Result<ReadContext>.Success(new ReadContext())
    };

    private Result<ReadContext> BuildSnapshotContext()
    {
        if (string.IsNullOrWhiteSpace(_snapshotIdInput) ||
            !long.TryParse(_snapshotIdInput, NumberStyles.None, CultureInfo.InvariantCulture, out var snapId) ||
            snapId <= 0)
        {
            return Result<ReadContext>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                "Bitte geben Sie eine gültige positive Snapshot-ID ein."));
        }
        return Result<ReadContext>.Success(new ReadContext(SnapshotId: new SnapshotId(snapId)));
    }

    private Result<ReadContext> BuildReleaseContext()
    {
        var releaseIdStr = !string.IsNullOrWhiteSpace(_selectedReleaseId) ? _selectedReleaseId : _releaseIdInput;
        if (string.IsNullOrWhiteSpace(releaseIdStr) ||
            !long.TryParse(releaseIdStr, NumberStyles.None, CultureInfo.InvariantCulture, out var relId) ||
            relId <= 0)
        {
            return Result<ReadContext>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                "Bitte wählen Sie einen gültigen Release aus."));
        }

        var matchingRel = _availableReleases.FirstOrDefault(r => r.Id == releaseIdStr);
        if (matchingRel is not null)
        {
            return Result<ReadContext>.Success(new ReadContext(SnapshotId: new SnapshotId(matchingRel.SnapshotId)));
        }

        return Result<ReadContext>.Success(new ReadContext());
    }

    private Result<ReadContext> BuildTransactionContext()
    {
        var txIdStr = !string.IsNullOrWhiteSpace(_selectedTransactionId) ? _selectedTransactionId : _transactionIdInput;
        if (string.IsNullOrWhiteSpace(txIdStr) ||
            !Guid.TryParseExact(txIdStr, "D", out var txGuid))
        {
            return Result<ReadContext>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                "Bitte wählen Sie eine gültige Transaktions-ID aus (GUID im Format D)."));
        }
        return Result<ReadContext>.Success(new ReadContext(TransactionId: new TransactionId(txGuid)));
    }

    internal async Task ApplyAsync()
    {
        _errorMessage = null;

        var validationError = ValidateSelection();
        if (validationError is not null)
        {
            _errorMessage = validationError;
            return;
        }

        if (!string.IsNullOrWhiteSpace(_selectedRoleId) && RoleStorage is not null)
        {
            await RoleStorage.SetLastRoleIdAsync(_selectedRoleId);
        }

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var targetUrl = BuildTargetUrl(uri);

        _isOpen = false;
        if (_dialog is not null)
        {
            await _dialog.CloseAsync();
        }
        State.Close();

        NavigationManager.NavigateTo(targetUrl);
    }

    private string? ValidateSelection()
    {
        var contextCheck = BuildCurrentReadContext();
        if (!contextCheck.IsSuccess)
        {
            return contextCheck.Error!.Message;
        }

        if (_availableRoles.Count > 0 && string.IsNullOrWhiteSpace(_selectedRoleId))
        {
            return "Bitte wählen Sie eine Rolle aus.";
        }

        return null;
    }

    private string BuildTargetUrl(Uri uri)
    {
        var path = uri.AbsolutePath;
        if (string.Equals(path, "/", StringComparison.Ordinal))
        {
            path = "/knowledge";
        }

        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_selectedRoleId))
        {
            queryParts.Add($"roleId={Uri.EscapeDataString(_selectedRoleId)}");
        }

        if (State.Mode == ContextSelectorMode.Full)
        {
            AppendFullModeContextQuery(queryParts);
        }
        else
        {
            AppendPreservedContextQuery(uri, queryParts);
        }

        return queryParts.Count > 0
            ? $"{path}?{string.Join("&", queryParts)}"
            : path;
    }

    private void AppendFullModeContextQuery(List<string> queryParts)
    {
        switch (_selectedKind)
        {
            case KnowledgeReadContextKind.Snapshot:
                queryParts.Add($"snapshotId={Uri.EscapeDataString(_snapshotIdInput!)}");
                break;

            case KnowledgeReadContextKind.Release:
                var relId = !string.IsNullOrWhiteSpace(_selectedReleaseId) ? _selectedReleaseId : _releaseIdInput;
                queryParts.Add($"releaseId={Uri.EscapeDataString(relId!)}");
                break;

            case KnowledgeReadContextKind.Transaction:
                var txId = !string.IsNullOrWhiteSpace(_selectedTransactionId) ? _selectedTransactionId : _transactionIdInput;
                queryParts.Add($"transactionId={Uri.EscapeDataString(txId!)}");
                break;
        }
    }

    private static void AppendPreservedContextQuery(Uri uri, List<string> queryParts)
    {
        var existingQuery = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        if (existingQuery.TryGetValue("snapshotId", out var snapVal))
            queryParts.Add($"snapshotId={Uri.EscapeDataString(snapVal[0]!)}");
        else if (existingQuery.TryGetValue("releaseId", out var relVal))
            queryParts.Add($"releaseId={Uri.EscapeDataString(relVal[0]!)}");
        else if (existingQuery.TryGetValue("transactionId", out var txVal))
            queryParts.Add($"transactionId={Uri.EscapeDataString(txVal[0]!)}");
    }

    internal async Task CancelAsync()
    {
        if (State.Mode == ContextSelectorMode.MandatoryRole)
        {
            return;
        }

        _isOpen = false;
        if (_dialog is not null)
        {
            await _dialog.CloseAsync();
        }
        State.Close();
    }

    internal async Task HandleDialogClosedAsync()
    {
        if (State.Mode == ContextSelectorMode.MandatoryRole &&
            _availableRoles.Count > 0 &&
            string.IsNullOrWhiteSpace(_selectedRoleId) &&
            State.IsOpen)
        {
            if (_dialog is not null)
            {
                await _dialog.OpenAsync();
            }
            return;
        }

        _isOpen = false;
        State.Close();
    }

    public ValueTask DisposeAsync()
    {
        State.Changed -= HandleStateChanged;
        return ValueTask.CompletedTask;
    }

    private sealed record RoleOption(string Id, string Name, string? Description);
    private sealed record ReleaseOption(string Id, string Name, long SnapshotId);
    private sealed record TransactionOption(string Id, string Title);
}
