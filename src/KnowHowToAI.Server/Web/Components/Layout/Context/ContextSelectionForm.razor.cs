using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Orchestriert den unpersistierten Auswahlvorgang des Kontextselektors.
/// Der Dialoghost bleibt dadurch auf Lifecycle und Zugänglichkeit begrenzt.
/// </summary>
public sealed partial class ContextSelectionForm : ComponentBase, IDisposable
{
    private readonly ContextSelectionDraft _draft = new();
    private ContextSelectionOptionsViewModel _options = ContextSelectionOptionsViewModel.Empty;
    private string? _selectedAudienceId;
    private string? _errorMessage;
    private bool _isLoadingAudiences;
    private CancellationTokenSource? _audienceLoadCancellation;
    private long _audienceLoadGeneration;

    [Inject]
    private ContextSelectorState State { get; set; } = default!;

    [Inject]
    private IContextSelectionCatalog Catalog { get; set; } = default!;

    [Inject]
    private IContextSelectionAudienceCatalog AudienceCatalog { get; set; } = default!;

    [Inject]
    private IAudienceStorageService AudienceStorage { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    private bool _isConfirmingDirtySwitch;

    protected override async Task OnInitializedAsync()
    {
        _draft.ApplyInitialContext(State.InitialReadContext);
        _selectedAudienceId = State.InitialAudienceId;

        if (State.Mode == ContextSelectorMode.Full)
        {
            _options = await Catalog.LoadAsync(CancellationToken.None);
        }

        await RefreshAudiencesAsync();
    }

    private async Task SelectKindAsync(KnowledgeReadContextKind kind)
    {
        if (_draft.SelectedKind == kind)
            return;

        _draft.SelectedKind = kind;
        _errorMessage = null;
        await RefreshAudiencesAsync();
    }

    private async Task OnContextParameterChangedAsync()
    {
        _errorMessage = null;
        await RefreshAudiencesAsync();
    }

    private async Task RefreshAudiencesAsync()
    {
        var generation = ++_audienceLoadGeneration;
        _audienceLoadCancellation?.Cancel();
        _audienceLoadCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _audienceLoadCancellation = cancellation;
        _isLoadingAudiences = true;
        _options = _options.WithAudiences([]);

        var context = _draft.BuildReadContext(_options);
        if (!context.IsSuccess)
        {
            if (generation == _audienceLoadGeneration)
            {
                _isLoadingAudiences = false;
                _audienceLoadCancellation = null;
            }

            cancellation.Dispose();

            return;
        }

        try
        {
            var readContext = context.Value!;
            var result = await AudienceCatalog.LoadAsync(readContext, cancellation.Token);
            if (!IsCurrentAudienceLoad(generation, readContext))
                return;

            _options = _options.WithAudiences(result.Audiences);
            _errorMessage = result.ErrorMessage;

            if (_selectedAudienceId is not null && !_options.Audiences.Any(audience => audience.Id == _selectedAudienceId))
            {
                _selectedAudienceId = null;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            if (generation == _audienceLoadGeneration)
            {
                _isLoadingAudiences = false;
                _audienceLoadCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private bool IsCurrentAudienceLoad(long generation, ReadContext loadedContext)
    {
        if (generation != _audienceLoadGeneration)
            return false;

        var currentContext = _draft.BuildReadContext(_options);
        return currentContext.IsSuccess && Equals(currentContext.Value, loadedContext);
    }

    private async Task ApplyAsync()
    {
        _errorMessage = ValidateSelection();
        if (_errorMessage is not null)
            return;

        if (WorkspaceEditState.IsDirty && !_isConfirmingDirtySwitch)
        {
            _isConfirmingDirtySwitch = true;
            _errorMessage = "Sie haben ungespeicherte Änderungen. Wenn Sie den Kontext wechseln, gehen diese verloren. Klicken Sie erneut auf 'Übernehmen', um trotzdem zu wechseln.";
            return;
        }

        if (WorkspaceEditState.IsDirty)
        {
            WorkspaceEditState.SetDirty(false, null);
        }

        if (!string.IsNullOrWhiteSpace(_selectedAudienceId))
        {
            await AudienceStorage.SetLastAudienceIdAsync(_selectedAudienceId);
        }

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var targetUrl = _draft.BuildTargetUrl(uri, State.Mode, _selectedAudienceId);

        _isConfirmingDirtySwitch = false;
        State.Close();
        NavigationManager.NavigateTo(targetUrl);
    }

    private string? ValidateSelection()
    {
        var context = _draft.BuildReadContext(_options);
        if (!context.IsSuccess)
            return context.Error!.Message;

        return _options.Audiences.Count > 0 && string.IsNullOrWhiteSpace(_selectedAudienceId)
            ? "Bitte wählen Sie eine Zielgruppe aus."
            : null;
    }

    private void Cancel()
    {
        _isConfirmingDirtySwitch = false;
        if (State.Mode == ContextSelectorMode.Full)
        {
            State.Close();
        }
    }

    public void Dispose()
    {
        _audienceLoadGeneration++;
        _audienceLoadCancellation?.Cancel();
        _audienceLoadCancellation?.Dispose();
        _audienceLoadCancellation = null;
    }
}
