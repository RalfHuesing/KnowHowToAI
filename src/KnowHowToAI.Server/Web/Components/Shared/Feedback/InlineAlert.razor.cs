using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.Feedback;

/// <summary>
/// Warnmeldung beim auslösenden Inhalt: bleibt genau dort stehen, wo der
/// Fehler oder Hinweis entsteht, und wird nicht zusätzlich in Banner oder
/// Toast verschoben. Fehler werden sofort (<c>role="alert"</c>), alle
/// übrigen Stufen höflich (<c>role="status"</c>) angekündigt. Rein
/// darstellend; keine Application-Aufrufe und keine Zustandsmaschine.
/// </summary>
public sealed partial class InlineAlert : ComponentBase
{
    [Parameter]
    public AlertKind Kind { get; set; } = AlertKind.Info;

    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    private AppStatusKind StatusKind => Kind.ToStatusKind();
}
