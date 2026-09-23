using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Audiences;

/// <summary>Wählt die Zielgruppenperspektive der Wissensseite.</summary>
public sealed partial class ContextSelectionForm : ComponentBase, IDisposable
{
    private IReadOnlyList<ContextSelectionAudienceOptionViewModel> _audiences = [];
    private string? _selectedAudienceId;
    private string? _errorMessage;
    private bool _isLoadingAudiences;
    private bool _isConfirmingDirtySwitch;
    private CancellationTokenSource? _audienceLoadCancellation;
    private long _audienceLoadGeneration;

    [Inject]
    private ContextSelectorState State { get; set; } = default!;

    [Inject]
    private IContextSelectionAudienceCatalog AudienceCatalog { get; set; } = default!;

    [Inject]
    private IAudienceStorageService AudienceStorage { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _selectedAudienceId = State.InitialAudienceId;
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

        try
        {
            var result = await AudienceCatalog.LoadAsync(
                State.InitialReadContext!, cancellation.Token);
            if (generation != _audienceLoadGeneration)
                return;

            _audiences = result.Audiences;
            _errorMessage = result.ErrorMessage;
            if (_selectedAudienceId is not null && !_audiences.Any(audience => audience.Id == _selectedAudienceId))
                _selectedAudienceId = null;
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

    private async Task ApplyAsync()
    {
        _errorMessage = _audiences.Count > 0 && string.IsNullOrWhiteSpace(_selectedAudienceId)
            ? "Bitte wählen Sie eine Zielgruppe aus."
            : null;
        if (_errorMessage is not null)
            return;

        if (WorkspaceEditState.IsDirty && !_isConfirmingDirtySwitch)
        {
            _isConfirmingDirtySwitch = true;
            _errorMessage = "Sie haben ungespeicherte Änderungen. Wenn Sie die Zielgruppe wechseln, gehen diese verloren. Klicken Sie erneut auf 'Auswählen', um trotzdem zu wechseln.";
            return;
        }

        if (WorkspaceEditState.IsDirty)
            WorkspaceEditState.SetDirty(false, null);

        if (!string.IsNullOrWhiteSpace(_selectedAudienceId))
            await AudienceStorage.SetLastAudienceIdAsync(_selectedAudienceId);

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var targetUrl = BuildKnowledgeUrl(uri, State.InitialReadContext!, _selectedAudienceId);
        _isConfirmingDirtySwitch = false;
        State.Close();
        NavigationManager.NavigateTo(targetUrl);
    }

    private void SelectAudience(string audienceId) => _selectedAudienceId = audienceId;


    private static string BuildKnowledgeUrl(Uri currentUri, ReadContext readContext, string? audienceId)
    {
        var query = new Dictionary<string, string?>();
        if (readContext.TransactionId is { } transactionId)
            query["transactionId"] = transactionId.Value.ToString("D");
        if (!string.IsNullOrWhiteSpace(audienceId))
            query["audienceId"] = audienceId;

        return QueryHelpers.AddQueryString(currentUri.AbsolutePath, query);
    }

    public void Dispose()
    {
        _audienceLoadGeneration++;
        _audienceLoadCancellation?.Cancel();
        _audienceLoadCancellation?.Dispose();
        _audienceLoadCancellation = null;
    }
}
