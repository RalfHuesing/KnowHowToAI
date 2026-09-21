namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// UI-sicheres Ergebnis eines Zielgruppen-Ladevorgangs im Kontextselektor.
/// </summary>
public sealed record ContextSelectionAudienceLoadResult(
    IReadOnlyList<ContextSelectionAudienceOptionViewModel> Audiences,
    string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
}
