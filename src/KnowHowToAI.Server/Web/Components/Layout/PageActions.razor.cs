using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout;

/// <summary>
/// Slot für die Seitenaktionen der Fachseite; das Hauptlayout rendert die
/// Komponente nur, wenn die Seite Aktionen eingehängt hat.
/// </summary>
public sealed partial class PageActions : ComponentBase
{
    [Parameter, EditorRequired]
    public RenderFragment Content { get; set; } = default!;
}
