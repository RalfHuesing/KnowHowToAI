namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Unveränderliches UI-Datenmodell für einen Kind-Knoten im Wissensbaum oder in Listen.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="NodeId">Eindeutige ID des Knotens.</param>
/// <param name="Title">Titel des Knotens.</param>
/// <param name="Description">Optionale Beschreibung des Knotens.</param>
/// <param name="SortOrder">Sortierreihenfolge unter Geschwisterknoten.</param>
/// <param name="ChildCount">Anzahl der direkten Kindknoten.</param>
/// <param name="ContentSizeBytes">Größe des Inhalts in Bytes.</param>
/// <param name="Availability">Verfügbarkeitsstatus als UI-Text.</param>
/// <param name="ResolvedRoleId">Aufgelöste Rolle (falls vorhanden).</param>
/// <param name="Freshness">Aktualitätsstatus als UI-Text.</param>
public sealed record ChildNodeViewModel(
    Guid NodeId,
    string Title,
    string? Description,
    int SortOrder,
    int ChildCount,
    int ContentSizeBytes,
    string Availability,
    string? ResolvedRoleId,
    string Freshness);
