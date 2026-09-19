namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine paginierte Kind-Knoten-Seite.
/// </summary>
/// <param name="ParentNodeId">Optionale ID des Elternknotens.</param>
/// <param name="Items">Liste der Kind-Knoten-ViewModels der aktuellen Seite.</param>
/// <param name="NextCursor">Opaker Cursor für die nächste Seite (falls vorhanden).</param>
public sealed record ChildrenPageViewModel(
    Guid? ParentNodeId,
    IReadOnlyList<ChildNodeViewModel> Items,
    string? NextCursor);
