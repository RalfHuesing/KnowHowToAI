namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine Quellrevision eines Derived Contents.
/// Entkoppelt Razor-Komponenten von Domain-Typen.
/// </summary>
/// <param name="TargetNodeId">ID des abhängigen Node (Derived).</param>
/// <param name="TargetRoleId">Rolle des abhängigen Contents.</param>
/// <param name="SourceNodeId">ID des Quell-Node.</param>
/// <param name="SourceRoleId">Rolle des Quell-Contents.</param>
/// <param name="SourceContentRevisionId">Revisions-ID des Quell-Contents zum Zeitpunkt der Abhängigkeit.</param>
public sealed record SourceRevisionViewModel(
    Guid TargetNodeId,
    string TargetRoleId,
    Guid SourceNodeId,
    string SourceRoleId,
    Guid SourceContentRevisionId);
