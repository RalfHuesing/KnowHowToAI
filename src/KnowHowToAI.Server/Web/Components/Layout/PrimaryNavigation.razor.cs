using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace KnowHowToAI.Server.Web.Components.Layout;

/// <summary>
/// Linke Navigationspalte der Shell mit dem vorhandenen Start-Link als
/// einzigem M2-Eintrag; noch nicht implementierte M3+-Routen erscheinen
/// bewusst nicht. In kompakter Breite erscheint die Navigation als
/// überlagerndes Panel; Öffnen und Schließen steuert das Hauptlayout, das
/// dafür den Fokus auf die Bereichsüberschrift setzt und ihn beim Schließen
/// an den Auslöser zurückgibt. Escape reicht das Hauptlayout über
/// <see cref="OnEscape"/> weiter, das nur den zuletzt geöffneten Bereich
/// schließt.
/// </summary>
public sealed partial class PrimaryNavigation : ComponentBase
{
    private ElementReference _titleElement;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnEscape { get; set; }

    /// <summary>Setzt den Fokus auf die Bereichsüberschrift; vom Hauptlayout nach dem Öffnen aufgerufen.</summary>
    public Task FocusTitleAsync() => _titleElement.FocusAsync().AsTask();

    private Task HandleKeydown(KeyboardEventArgs args) =>
        args.Key == "Escape" ? OnEscape.InvokeAsync() : Task.CompletedTask;
}
