using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Roles;

/// <summary>
/// Ein expliziter Kandidat einer nicht rekursiven Rollenauflösungsreihenfolge.
/// </summary>
public sealed record RoleResolution(
    SnapshotId SnapshotId,
    RoleId RequestedRoleId,
    RoleId CandidateRoleId,
    int Priority);
