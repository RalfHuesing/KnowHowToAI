using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Shared.Diffs;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Drafts;

/// <summary>Zeigt den paginierten Netto-Diff einer Transaction gegenüber ihrem Base Snapshot.</summary>
public sealed partial class TransactionDiff : ComponentBase, IDisposable
{
    [Inject]
    private HistoryService HistoryService { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Parameter, EditorRequired]
    public KnowledgeTransaction Transaction { get; set; } = default!;

    private SnapshotDiffViewModel? _viewModel;
    private string? _errorMessage;
    private long? _loadedChangeVersion;
    private bool _isLoading;
    private Guid _loadedTransactionId;

    private bool IsStale => _viewModel is not null
        && _loadedChangeVersion != (WorkspaceState.CurrentChangeVersion ?? Transaction.ChangeVersion);

    protected override void OnInitialized() => WorkspaceState.Changed += OnWorkspaceChanged;

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedTransactionId == Transaction.TransactionId.Value)
            return;

        _loadedTransactionId = Transaction.TransactionId.Value;
        await LoadPageAsync(cursor: null, append: false);
    }

    private Task RefreshAsync() => LoadPageAsync(cursor: null, append: false);

    private Task LoadNextAsync() => LoadPageAsync(_viewModel?.NextCursor, append: true);

    private async Task LoadPageAsync(string? cursor, bool append)
    {
        _isLoading = true;
        _errorMessage = null;

        var result = await HistoryService.GetTransactionChangesAsync(
            Transaction.TransactionId,
            limit: null,
            cursor,
            CancellationToken.None);

        _isLoading = false;
        if (!result.IsSuccess)
        {
            _errorMessage = result.Error!.Message;
            return;
        }

        var page = SnapshotDiffMapper.ToViewModel(result.Value!.Changes);
        _viewModel = append && _viewModel is not null
            ? page with { Entries = _viewModel.Entries.Concat(page.Entries).ToArray() }
            : page;
        _loadedChangeVersion = result.Value.Transaction.ChangeVersion;
    }

    private void OnWorkspaceChanged() => _ = InvokeAsync(StateHasChanged);

    private static string MapKind(string kind) => kind switch
    {
        "Added" => "Hinzugefügt",
        "Modified" => "Geändert",
        "Deleted" => "Gelöscht",
        _ => kind
    };

    private static string MapEntityType(string entityType) => entityType switch
    {
        "Node" => "Knoten",
        "Content" => "Inhalt",
        "Audience" => "Zielgruppe",
        "AudienceResolution" => "Zielgruppenauflösung",
        "Dependency" => "Abhängigkeit",
        _ => entityType
    };

    public void Dispose() => WorkspaceState.Changed -= OnWorkspaceChanged;
}
