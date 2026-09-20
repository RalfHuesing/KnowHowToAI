using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Navigationsergebnis: eine Node mit aufgelöstem Zielgruppen-Content, Availability und Freshness.
/// Wird von NavigationService für GetRootAsync und GetNodeAsync zurückgegeben.
/// </summary>
public sealed record NodeWithContent(
    Node? Node,
    AudienceId RequestedAudienceId,
    AudienceId? ResolvedAudienceId,
    Availability Availability,
    bool FallbackUsed,
    NodeContent? Content,
    Freshness Freshness,
    IReadOnlyList<DerivedSourceRevision>? SourceRevisions = null);
