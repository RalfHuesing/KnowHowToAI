using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.States;

/// <summary>
/// Fachlich leerer Bestand (z. B. noch keine Einträge): klar getrennt vom
/// Nichtgefunden-Zustand, weil hier kein gesuchter Kontext fehlt, sondern
/// der Bestand ohne Ergebnis berechtigt leer ist. Rein darstellend; keine
/// Application-Aufrufe und keine Zustandsmaschine.
/// </summary>
public sealed partial class EmptyState : ComponentBase
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public EventCallback OnRetry { get; set; }
}
