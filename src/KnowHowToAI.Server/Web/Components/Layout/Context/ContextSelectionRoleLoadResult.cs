namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// UI-sicheres Ergebnis eines Rollen-Ladevorgangs im Kontextselektor.
/// </summary>
public sealed record ContextSelectionRoleLoadResult(
    IReadOnlyList<ContextSelectionRoleOptionViewModel> Roles,
    string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
}
