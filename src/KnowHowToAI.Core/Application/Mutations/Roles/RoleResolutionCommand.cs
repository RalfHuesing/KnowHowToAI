using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Roles;

/// <summary>
/// Parameter für set_role_resolution: vollständiger Ersatz der Kandidatenliste
/// für eine angefragte Rolle. Reihenfolge der Kandidaten bestimmt die Priority.
/// </summary>
public sealed record RoleResolutionCommand(
    TransactionId TransactionId,
    RoleId RequestedRoleId,
    IReadOnlyList<RoleId> CandidateRoleIds);

