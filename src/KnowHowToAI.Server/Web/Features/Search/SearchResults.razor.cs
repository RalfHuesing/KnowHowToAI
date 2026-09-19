using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>Rendert eine einzelne, cursorbasierte Trefferseite ohne Content nachzuladen.</summary>
public sealed partial class SearchResults
{
    [Parameter]
    public SearchPageViewModel? Page { get; set; }

    [Parameter]
    public bool IsSearching { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public EventCallback OnNext { get; set; }

    [Parameter]
    public EventCallback<Guid> OnNavigate { get; set; }

    private static string MapHitField(string hitField) => hitField switch
    {
        "Title" => "Titel",
        "Description" => "Beschreibung",
        "Content" => "Inhalt",
        _ => hitField
    };
}
