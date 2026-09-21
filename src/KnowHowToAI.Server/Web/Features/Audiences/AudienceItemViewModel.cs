namespace KnowHowToAI.Server.Web.Features.Audiences;

/// <summary>
/// Unveränderliches UI-Datenmodell für einen Zielgruppeneintrag.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="AudienceId">Eindeutige ID der Zielgruppe.</param>
/// <param name="Name">Name der Zielgruppe.</param>
/// <param name="Description">Optionale Beschreibung der Zielgruppe.</param>
public sealed record AudienceItemViewModel(
    string AudienceId,
    string Name,
    string? Description);
