using System.Globalization;
using Microsoft.AspNetCore.Components;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Koordiniert Query-Parameter und die Auswahl der beiden Vergleichssnapshots.</summary>
public sealed partial class HistoryPage
{
    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "roleId")]
    private string? QueryRoleId { get; set; }

    [SupplyParameterFromQuery(Name = "baseSnapshotId")]
    private string? QueryBaseSnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "targetSnapshotId")]
    private string? QueryTargetSnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "nodeId")]
    private string? QueryNodeId { get; set; }

    private IReadOnlyList<SnapshotViewModel> _snapshots = [];
    private long? _baseSnapshotId;
    private long? _targetSnapshotId;
    private Guid? _nodeFilterId;
    private string? _comparisonErrorMessage;

    protected override void OnInitialized()
    {
        PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));
        InitializeComparisonFromQuery();
    }

    private Task SnapshotsChangedAsync(IReadOnlyList<SnapshotViewModel> snapshots)
    {
        _snapshots = snapshots;
        return Task.CompletedTask;
    }

    private Task SelectBaseSnapshotAsync(long snapshotId)
    {
        _baseSnapshotId = snapshotId;
        return Task.CompletedTask;
    }

    private Task SelectTargetSnapshotAsync(long snapshotId)
    {
        _targetSnapshotId = snapshotId;
        return Task.CompletedTask;
    }

    private void InitializeComparisonFromQuery()
    {
        _baseSnapshotId = TryParseSnapshotId(QueryBaseSnapshotId, "Ausgangs-Snapshot");
        _targetSnapshotId = TryParseSnapshotId(QueryTargetSnapshotId, "Ziel-Snapshot");
        if (string.IsNullOrWhiteSpace(QueryNodeId))
            return;

        if (Guid.TryParse(QueryNodeId, out var nodeId))
            _nodeFilterId = nodeId;
        else
            _comparisonErrorMessage = "Der Knotenfilter ist ungültig.";
    }

    private static string FormatSelection(long? snapshotId) =>
        snapshotId is { } value
            ? $"Snapshot {value.ToString(CultureInfo.InvariantCulture)}"
            : "Noch nicht gewählt";

    private long? TryParseSnapshotId(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var snapshotId) && snapshotId > 0)
            return snapshotId;

        _comparisonErrorMessage = $"Der {label} ist ungültig.";
        return null;
    }
}
