namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>
/// Unveränderliches UI-Datenmodell für einen einzelnen Release-Eintrag.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="ReleaseId">Eindeutige ID des Releases.</param>
/// <param name="SnapshotId">ID des zugehörigen unveränderlichen Snapshots.</param>
/// <param name="Name">Name des Releases.</param>
/// <param name="Description">Optionale Beschreibung des Releases.</param>
/// <param name="ReleasedAtUtc">Veröffentlichungszeitpunkt in UTC.</param>
public sealed record ReleaseItemViewModel(
    long ReleaseId,
    long SnapshotId,
    string Name,
    string? Description,
    DateTimeOffset ReleasedAtUtc);
