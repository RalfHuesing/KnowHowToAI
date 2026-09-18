using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared;

/// <summary>
/// Teilaktualisierung im betroffenen Bereich: während <see cref="IsBusy"/>
/// bleibt der Inhalt sichtbar (halbdurchsichtige Abdeckung), Interaktion und
/// Fokus im Kindinhalt sind gesperrt (<c>inert</c>), sodass nur die konkret
/// laufende Aktion geschützt ist und keine Doppelaktion entsteht. Rein
/// darstellend; keine Application-Aufrufe und keine Zustandsmaschine.
/// </summary>
public sealed partial class BusyOverlay : ComponentBase
{
    [Parameter]
    public bool IsBusy { get; set; }

    [Parameter, EditorRequired]
    public string BusyText { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
