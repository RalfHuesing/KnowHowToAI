using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>Paginiertes Ergebnis für list_children mit Metadaten-first-Inhalt.</summary>
public sealed record ChildrenPage(
    NodeId? ParentNodeId,
    IReadOnlyList<ChildNodeSummary> Items,
    string? NextCursor);

/// <summary>
/// Metadaten-Zusammenfassung einer Kind-Node für list_children.
/// Enthält bewusst keinen vollständigen Content (Metadata-First-Prinzip).
/// </summary>
public sealed record ChildNodeSummary(
    NodeId NodeId,
    string Title,
    string? Description,
    int SortOrder,
    int ChildCount,
    int ContentSizeBytes,
    Availability Availability,
    RoleId? ResolvedRoleId,
    Freshness Freshness);
