using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Navigationsergebnis: eine Node mit aufgelöstem Rollen-Content, Availability und Freshness.
/// Wird von NavigationService für GetRootAsync und GetNodeAsync zurückgegeben.
/// </summary>
public sealed record NodeWithContent(
    Node? Node,
    RoleId RequestedRoleId,
    RoleId? ResolvedRoleId,
    Availability Availability,
    bool FallbackUsed,
    NodeContent? Content,
    Freshness Freshness);
