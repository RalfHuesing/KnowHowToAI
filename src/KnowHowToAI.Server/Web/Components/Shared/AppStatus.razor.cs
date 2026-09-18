using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared;

/// <summary>
/// Zentrale Statusdarstellung mit Icon und Text; die Farbe ist nie der
/// alleinige Informationsträger. Alle Farben stammen aus den globalen
/// Tokens in <c>wwwroot/css/app.css</c>.
/// </summary>
public sealed partial class AppStatus : ComponentBase
{
    [Parameter]
    public AppStatusKind Kind { get; set; } = AppStatusKind.Neutral;

    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;
}
