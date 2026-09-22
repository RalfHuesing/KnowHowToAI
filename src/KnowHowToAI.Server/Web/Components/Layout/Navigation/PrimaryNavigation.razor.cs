using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace KnowHowToAI.Server.Web.Components.Layout.Navigation;

/// <summary>
/// Globale Navigation für Wissen und Entwürfe. In kompakter Breite erscheint
/// sie als überlagerndes Panel; Öffnen und Schließen steuert das Hauptlayout,
/// das den Fokus auf den Bereich setzt und ihn beim Schließen an den Auslöser
/// zurückgibt. Escape wird an das Hauptlayout weitergegeben.
/// </summary>
public sealed partial class PrimaryNavigation : ComponentBase
{
    private ElementReference _navigationElement;

    [Parameter]
    public EventCallback OnEscape { get; set; }

    /// <summary>Setzt den Fokus auf den Navigationsbereich; vom Hauptlayout nach dem Öffnen aufgerufen.</summary>
    public Task FocusAsync() => _navigationElement.FocusAsync().AsTask();

    private Task HandleKeydown(KeyboardEventArgs args) =>
        args.Key == "Escape" ? OnEscape.InvokeAsync() : Task.CompletedTask;
}
