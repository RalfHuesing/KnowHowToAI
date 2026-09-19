namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine Quellrevision eines Derived Contents.
/// Entkoppelt Razor-Komponenten von Domain-Typen.
/// </summary>
/// <param name="SourceNodeId">ID des Quell-Node.</param>
/// <param name="SourceRoleId">Rolle des Quell-Contents.</param>
/// <param name="SourceContentRevisionId">Revisions-ID des Quell-Contents zum Zeitpunkt der Abhängigkeit.</param>
/// <param name="Freshness">Aktueller Freshness-Vergleich für die gespeicherte Quellrevision.</param>
public sealed record SourceRevisionViewModel(
    Guid SourceNodeId,
    string SourceRoleId,
    Guid SourceContentRevisionId,
    string Freshness);
