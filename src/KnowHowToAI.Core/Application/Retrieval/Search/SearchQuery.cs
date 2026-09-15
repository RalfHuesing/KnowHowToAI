using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Suchparameter für search-Use-Case. Text ist der einzige Pflichtwert.
/// Limit und Cursor steuern das Paging gemäß RetrievalPolicy.
/// </summary>
public sealed record SearchQuery(
    string Text,
    int? Limit = null,
    string? Cursor = null,
    RoleId? RoleId = null);

/// <summary>Paginiertes Suchergebnis mit Snippets.</summary>
public sealed record SearchResultPage(
    string Query,
    IReadOnlyList<SearchHit> Items,
    string? NextCursor);

/// <summary>Einzelner Suchtreffer mit Node-Metadaten und Snippet.</summary>
public sealed record SearchHit(
    NodeId NodeId,
    string Title,
    string? Description,
    string? Snippet,
    string HitField,
    Availability Availability,
    RoleId? ResolvedRoleId,
    Freshness Freshness);
