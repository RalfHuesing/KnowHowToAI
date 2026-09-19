using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.Feedback;

/// <summary>
/// Seitenweiter Warnzustand: gilt für die gesamte Seite und nicht für ein
/// einzelnes Feld oder einen einzelnen Inhalt. Fehler werden sofort
/// (<c>role="alert"</c>), alle übrigen Stufen höflich
/// (<c>role="status"</c>) angekündigt. Rein darstellend; keine
/// Application-Aufrufe und keine Zustandsmaschine.
/// </summary>
public sealed partial class StatusBanner : ComponentBase
{
    [Parameter]
    public AlertKind Kind { get; set; } = AlertKind.Info;

    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    private AppStatusKind StatusKind => Kind.ToStatusKind();
}
