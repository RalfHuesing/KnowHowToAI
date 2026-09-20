using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Mutations.Roles;

/// <summary>Ergebnis einer Rollenmutation mit dem danach gültigen Working-Stand.</summary>
public sealed record RoleMutationResult(Role Role, SnapshotId SnapshotId, long ChangeVersion)
{
    public RoleId RoleId => Role.RoleId;

    public string Name => Role.Name;

    public string? Description => Role.Description;

    public bool IsDeleted => Role.IsDeleted;
}

/// <summary>Transportneutrale Eingabe für eine Rollenänderung.</summary>
public sealed record UpdateRoleMutationRequest(
    RoleId RoleId,
    string Name,
    string? Description,
    long? ExpectedChangeVersion = null);

/// <summary>Ergebnis einer Resolution-Order-Mutation mit dem danach gültigen Working-Stand.</summary>
public sealed record RoleResolutionMutationResult(
    RoleId RequestedRoleId,
    IReadOnlyList<RoleResolution> Resolutions,
    SnapshotId SnapshotId,
    long ChangeVersion);
