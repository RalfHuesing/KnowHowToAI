using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>Erfasst den unpersistierten Suchtext und übergibt ihn auf Submit an die Seite.</summary>
public sealed partial class SearchForm
{
    private string _text = string.Empty;

    [Parameter]
    public bool IsSearching { get; set; }

    [Parameter]
    public EventCallback<string> OnSearch { get; set; }

    private Task SubmitAsync() => OnSearch.InvokeAsync(_text);
}
