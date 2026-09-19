namespace KnowHowToAI.Server.Web.Features.Roles;

/// <summary>
/// Unveränderliches UI-Datenmodell für einen Rolleneintrag.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="RoleId">Eindeutige ID der Rolle.</param>
/// <param name="Name">Name der Rolle.</param>
/// <param name="Description">Optionale Beschreibung der Rolle.</param>
public sealed record RoleItemViewModel(
    string RoleId,
    string Name,
    string? Description);
