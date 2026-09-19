using Microsoft.AspNetCore.Components;
using KnowHowToAI.Server.Web.Components.Shared.Diffs;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Read-only Darstellung einer einzelnen, cursor-paginierten Snapshot-Diffseite.</summary>
public sealed partial class SnapshotDiffPanel
{
    [Parameter]
    public SnapshotDiffViewModel? ViewModel { get; set; }

    [Parameter]
    public long? BaseSnapshotId { get; set; }

    [Parameter]
    public long? TargetSnapshotId { get; set; }

    [Parameter]
    public Guid? NodeFilterId { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public EventCallback LoadNext { get; set; }

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
