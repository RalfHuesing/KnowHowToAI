namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Unveränderliches UI-Datenmodell für die Detailansicht eines Knotens.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="NodeId">Eindeutige ID des Knotens.</param>
/// <param name="ParentNodeId">Optionale ID des Elternknotens.</param>
/// <param name="Title">Titel des Knotens.</param>
/// <param name="Description">Optionale Beschreibung des Knotens.</param>
/// <param name="SortOrder">Sortierreihenfolge unter Geschwisterknoten.</param>
/// <param name="RequestedRoleId">Angefragte Rolle bei der Auflösung.</param>
/// <param name="ResolvedRoleId">Tatsächlich aufgelöste Rolle des Inhalts (falls vorhanden).</param>
/// <param name="FallbackUsed">Gibt an, ob der Inhalt über Fallback-Regeln aufgelöst wurde.</param>
/// <param name="Availability">Verfügbarkeitsstatus als UI-Text.</param>
/// <param name="Freshness">Aktualitätsstatus als UI-Text.</param>
/// <param name="ContentRevisionId">Optionale ID der Inhaltsrevision.</param>
/// <param name="ContentMode">Optionaler Inhaltsmodus (z. B. Independent, Derived).</param>
/// <param name="ContentMd">Optionaler Markdown-Inhalt des Knotens.</param>
/// <param name="ChangeVersion">Optionale Versionsnummer bei Working-Snapshot-Reads.</param>
public sealed record NodeDetailsViewModel(
    Guid NodeId,
    Guid? ParentNodeId,
    string Title,
    string? Description,
    int SortOrder,
    string RequestedRoleId,
    string? ResolvedRoleId,
    bool FallbackUsed,
    string Availability,
    string Freshness,
    Guid? ContentRevisionId,
    string? ContentMode,
    string? ContentMd,
    long? ChangeVersion = null);
