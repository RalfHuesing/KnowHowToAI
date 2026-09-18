using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared;

/// <summary>
/// Änderungszustand am Ort des Geschehens: zeigt eine laufende Änderung als
/// Text plus Icon und nie nur über Farbe; im Ruhezustand wird nichts
/// gerendert. Für blockierende Läufe über einem ganzen Bereich bleibt der
/// <see cref="BusyOverlay"/>, für den ungespeicherten Zustand die zentrale
/// Statusdarstellung <see cref="AppStatus"/> zuständig. Rein darstellend;
/// keine Application-Aufrufe und keine Zustandsmaschine.
/// </summary>
public sealed partial class WorkingIndicator : ComponentBase
{
    [Parameter]
    public bool IsWorking { get; set; }

    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;
}
