using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>
/// Read-only Detailansicht eines ausgewählten Knotens.
/// Zeigt den Inhalt einer einzelnen Node-Ansicht und hält deren Titel stabil.
/// </summary>
public sealed partial class NodeDetails
{
    [Parameter]
    public NodeDetailsViewModel? ViewModel { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public bool NodeNotFound { get; set; }

    [Parameter]
    public bool ShowTitle { get; set; } = true;

    [Parameter]
    public bool IsWorking { get; set; }

    [Parameter]
    public string ActiveView { get; set; } = "Read";

    private bool _isLoading;
    private string? _errorMessage;
    private bool _nodeNotFound;

    protected override void OnParametersSet()
    {
        _isLoading = IsLoading;
        _errorMessage = ErrorMessage;
        _nodeNotFound = NodeNotFound && !IsLoading && ErrorMessage is null && ViewModel is null;
    }

    private static string MapAvailabilityLabel(string availability) => availability switch
    {
        "Explicit" => "Eigener Inhalt",
        "Fallback" => "Fallback",
        "None" => "Kein Inhalt",
        _ => availability
    };

    private static string MapFreshnessLabel(string freshness) => freshness switch
    {
        "Current" => "Aktuell",
        "Stale" => "Veraltet",
        "Unknown" => "Unbekannt",
        _ => freshness
    };

    private static string MapContentModeLabel(string? contentMode) => contentMode switch
    {
        "Independent" => "Eigenständig",
        "Derived" => "Abgeleitet",
        _ => contentMode ?? string.Empty
    };
}
