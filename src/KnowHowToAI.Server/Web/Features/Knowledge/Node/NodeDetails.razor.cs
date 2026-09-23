using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>
/// Read-only Detailansicht eines ausgewählten Knotens.
/// Zeigt Titel, Description, Position, Zielgruppe, aufgelösten Content,
/// Fallback/Provenienz, Revision und Freshness ohne Bearbeitungscontrols.
/// </summary>
public sealed partial class NodeDetails
{
    [Parameter]
    public NodeDetailsViewModel? ViewModel { get; set; }

    [Parameter]
    public string? MarkdownDownloadUrl { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public bool NodeNotFound { get; set; }

    [Parameter]
    public bool ShowTitle { get; set; } = true;

    [Parameter]
    public bool ShowContent { get; set; } = true;

    [Parameter]
    public bool IsWorking { get; set; }

    [Parameter]
    public bool ShowEditAction { get; set; }

    [Parameter]
    public string EditActionLabel { get; set; } = "Bearbeiten";

    [Parameter]
    public EventCallback OnEditRequested { get; set; }

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
