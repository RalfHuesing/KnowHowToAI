namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>
/// Unveränderliches UI-Datenmodell für einen einzelnen Eintrag in einem Snapshot-Diff.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="Kind">Art der Änderung (Added, Modified, Deleted).</param>
/// <param name="EntityType">Art der Entität (Node, Role, RoleResolution, Content, Dependency).</param>
/// <param name="PrimaryId">Primäre ID der betroffenen Entität.</param>
/// <param name="SecondaryId">Optionale sekundäre ID (z. B. RoleId bei Content oder CandidateRoleId).</param>
/// <param name="Detail">Optionales Detail zur Änderung.</param>
public sealed record SnapshotDiffEntryViewModel(
    string Kind,
    string EntityType,
    string PrimaryId,
    string? SecondaryId = null,
    string? Detail = null);
