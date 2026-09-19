using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.States;

/// <summary>
/// Nicht gefundener Kontext (z. B. Node, Transaction oder Snapshot existiert
/// nicht oder nicht mehr): klar getrennt vom fachlich leeren Bestand, weil
/// hier ein konkret gesuchter Kontext fehlt. Rein darstellend; keine
/// Application-Aufrufe und keine Zustandsmaschine.
/// </summary>
public sealed partial class NotFoundState : ComponentBase
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public EventCallback OnRetry { get; set; }
}
