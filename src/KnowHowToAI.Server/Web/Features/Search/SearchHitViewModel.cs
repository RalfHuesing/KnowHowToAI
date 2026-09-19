namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>
/// Unveränderliches UI-Datenmodell für einen einzelnen Suchtreffer.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="NodeId">Eindeutige ID des gefundenen Knotens.</param>
/// <param name="Title">Titel des Knotens.</param>
/// <param name="Description">Optionale Beschreibung des Knotens.</param>
/// <param name="Snippet">Optionaler Treffer-Textausschnitt.</param>
/// <param name="HitField">Feld, in dem der Treffer gefunden wurde (Title, Description, Content).</param>
/// <param name="Availability">Verfügbarkeitsstatus als UI-Text.</param>
/// <param name="ResolvedRoleId">Aufgelöste Rolle des Inhalts (falls vorhanden).</param>
/// <param name="Freshness">Aktualitätsstatus als UI-Text.</param>
/// <param name="SortOrder">Sortierreihenfolge des Knotens.</param>
/// <param name="Breadcrumb">Hierarchischer Pfad vom Root bis zum Treffer.</param>
public sealed record SearchHitViewModel(
    Guid NodeId,
    string Title,
    string? Description,
    string? Snippet,
    string HitField,
    string Availability,
    string? ResolvedRoleId,
    string Freshness,
    int SortOrder,
    IReadOnlyList<string> Breadcrumb);
