namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine Quellrevision eines Derived Contents.
/// Entkoppelt Razor-Komponenten von Domain-Typen.
/// </summary>
/// <param name="SourceNodeId">ID des Quell-Node.</param>
/// <param name="SourceAudienceId">Zielgruppe des Quell-Contents.</param>
/// <param name="SourceContentRevisionId">Revisions-ID des Quell-Contents zum Zeitpunkt der Abhängigkeit.</param>
/// <param name="Freshness">Aktueller Freshness-Vergleich für die gespeicherte Quellrevision.</param>
/// <param name="SourceNodeTitle">Lesbarer Quellknotentitel oder ein Hinweis, wenn der Knoten nicht verfügbar ist.</param>
public sealed record SourceRevisionViewModel(
    Guid SourceNodeId,
    string SourceAudienceId,
    Guid SourceContentRevisionId,
    string Freshness,
    string SourceNodeTitle = "Quellknoten nicht verfügbar");
