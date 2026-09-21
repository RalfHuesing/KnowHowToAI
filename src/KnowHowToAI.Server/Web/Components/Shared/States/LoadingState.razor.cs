using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.States;

/// <summary>
/// Wahrnehmbarer Initial-Load-Zustand: ersetzt die endlose leere Fläche
/// durch Statusregion, höfliche einmalige Statusmeldung und ruhende bzw.
/// animierte Ladedarstellung gemäß <c>prefers-reduced-motion</c>.
/// Rein darstellend; keine Application-Aufrufe und keine Zustandsmaschine.
/// </summary>
public sealed partial class LoadingState : ComponentBase
{
    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;
}
