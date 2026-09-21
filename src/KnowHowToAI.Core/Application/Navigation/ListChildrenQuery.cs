using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Abfrage-Parameter für <see cref="NavigationService.ListChildrenAsync"/>.
/// Kapselt Node-ID, Read-Kontext, Zielgruppenfilter und Paging-Optionen.
/// </summary>
public sealed record ListChildrenQuery(
    NodeId? ParentNodeId,
    ReadContext Context,
    AudienceId AudienceId,
    int? Limit = null,
    string? Cursor = null);
