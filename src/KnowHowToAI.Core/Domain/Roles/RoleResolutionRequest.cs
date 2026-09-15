using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Domain.Roles;

/// <summary>
/// Der vollständige, auf einen Snapshot und eine Node begrenzte Eingabekontext der Rollenauflösung.
/// </summary>
public sealed record RoleResolutionRequest(
    SnapshotId SnapshotId,
    NodeId NodeId,
    RoleId RequestedRole,
    IEnumerable<Role> Roles,
    IEnumerable<RoleResolution> ResolutionOrder,
    IEnumerable<NodeContent> Contents);
