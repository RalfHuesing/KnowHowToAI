using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout.PageRegions;

/// <summary>
/// Slot für den Breadcrumb-Bereich der Fachseite; das Hauptlayout rendert die
/// Komponente nur, wenn die Seite Breadcrumbs eingehängt hat. Leere Bereiche
/// belegen dadurch keinen Platz.
/// </summary>
public sealed partial class BreadcrumbRegion : ComponentBase
{
    [Parameter, EditorRequired]
    public RenderFragment Content { get; set; } = default!;
}
