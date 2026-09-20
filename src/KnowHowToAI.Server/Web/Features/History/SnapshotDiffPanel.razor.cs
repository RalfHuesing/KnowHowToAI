using Microsoft.AspNetCore.Components;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Shared.Diffs;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Lädt und zeigt einen cursor-paginierten, schreibgeschützten Snapshot-Diff.</summary>
public sealed partial class SnapshotDiffPanel
{
    [Inject]
    private HistoryService HistoryService { get; set; } = default!;

    [Parameter]
    public long? BaseSnapshotId { get; set; }

    [Parameter]
    public long? TargetSnapshotId { get; set; }

    [Parameter]
    public Guid? NodeFilterId { get; set; }

    [Parameter]
    public string? InitialErrorMessage { get; set; }

    private SnapshotDiffViewModel? _viewModel;
    private string? _errorMessage;
    private bool _isLoading;
    private (long? BaseSnapshotId, long? TargetSnapshotId, Guid? NodeFilterId)? _loadedSelection;

    protected override async Task OnParametersSetAsync()
    {
        var selection = (BaseSnapshotId, TargetSnapshotId, NodeFilterId);
        if (_loadedSelection == selection)
            return;

        _loadedSelection = selection;
        _viewModel = null;
        _errorMessage = InitialErrorMessage;
        if (!string.IsNullOrWhiteSpace(_errorMessage) || BaseSnapshotId is null || TargetSnapshotId is null)
            return;

        if (BaseSnapshotId == TargetSnapshotId)
        {
            _errorMessage = "Ausgangs- und Ziel-Snapshot müssen unterschiedlich sein.";
            return;
        }

        await LoadPageAsync(cursor: null);
    }

    private Task LoadNextPageAsync() => LoadPageAsync(_viewModel?.NextCursor);

    private async Task LoadPageAsync(string? cursor)
    {
        _isLoading = true;
        _errorMessage = null;
        var result = await HistoryService.CompareSnapshotsAsync(
            new SnapshotComparisonQuery(
                new SnapshotId(BaseSnapshotId!.Value),
                new SnapshotId(TargetSnapshotId!.Value),
                Limit: null,
                Cursor: cursor,
                FilterNodeId: NodeFilterId is { } nodeId ? new NodeId(nodeId) : null),
            CancellationToken.None);
        _isLoading = false;
        var mapped = HistoryMapper.ToSnapshotDiffResult(result);
        if (mapped.IsSuccess)
            _viewModel = mapped.Value;
        else
            _errorMessage = mapped.Error!.Message;
    }

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
        "Role" => "Rolle",
        "RoleResolution" => "Rollenauflösung",
        "Dependency" => "Abhängigkeit",
        _ => entityType
    };
}
