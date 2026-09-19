using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.States;

/// <summary>
/// Technischer Fehler: neutraler deutscher Text, optional opaque
/// Correlation-ID und optionaler Retry; niemals Exception, Pfade, SQL- oder
/// Toolausgabe. Die Fehlerregion ist fokussierbar und über ihre Überschrift
/// beschriftet. Rein darstellend; keine Application-Aufrufe und keine
/// Zustandsmaschine.
/// </summary>
public sealed partial class TechnicalErrorState : ComponentBase
{
    [Parameter]
    public string? CorrelationId { get; set; }

    [Parameter]
    public EventCallback OnRetry { get; set; }
}
