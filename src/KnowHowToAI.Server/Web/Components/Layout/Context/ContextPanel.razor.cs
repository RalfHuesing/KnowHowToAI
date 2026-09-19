using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Optionaler Kontextbereich der Shell als <c>aside</c>-Landmark; das
/// Hauptlayout rendert die Komponente nur, wenn die Seite Kontext eingehängt
/// hat. In kompakter Breite erscheint der Bereich als überlagerndes Panel;
/// Öffnen und Schließen steuert das Hauptlayout, das dafür den Fokus auf die
/// Bereichsüberschrift setzt und ihn beim Schließen an den Auslöser
/// zurückgibt. Escape reicht das Hauptlayout über <see cref="OnEscape"/>
/// weiter, das nur den zuletzt geöffneten Bereich schließt.
/// </summary>
public sealed partial class ContextPanel : ComponentBase
{
    private ElementReference _titleElement;

    [Parameter, EditorRequired]
    public RenderFragment Content { get; set; } = default!;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnEscape { get; set; }

    /// <summary>Setzt den Fokus auf die Bereichsüberschrift; vom Hauptlayout nach dem Öffnen aufgerufen.</summary>
    public Task FocusTitleAsync() => _titleElement.FocusAsync().AsTask();

    private Task HandleKeydown(KeyboardEventArgs args) =>
        args.Key == "Escape" ? OnEscape.InvokeAsync() : Task.CompletedTask;
}
