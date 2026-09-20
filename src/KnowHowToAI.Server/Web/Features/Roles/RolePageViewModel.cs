namespace KnowHowToAI.Server.Web.Features.Roles;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine paginierte Rollenliste.
/// </summary>
/// <param name="Items">Liste der Rollen-ViewModels der aktuellen Seite.</param>
/// <param name="NextCursor">Opaker Cursor für die nächste Seite (falls vorhanden).</param>
public sealed record RolePageViewModel(
    IReadOnlyList<RoleItemViewModel> Items,
    string? NextCursor,
    long? ChangeVersion = null);
