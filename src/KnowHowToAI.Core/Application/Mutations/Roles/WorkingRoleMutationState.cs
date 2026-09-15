using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Mutations.Roles;

/// <summary>Vollständige, konsistente Sicht für eine Rollenmutation.</summary>
public sealed record WorkingRoleMutationState(
    SnapshotId SnapshotId,
    IReadOnlyList<Role> Roles,
    IReadOnlyList<RoleResolution> Resolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);

public sealed record WorkingRoleMutationDecision<T>(T Value, WorkingRoleMutationState State);

public sealed record WorkingRoleMutationExecution<T>(
    T Value,
    SnapshotId SnapshotId,
    long ChangeVersion,
    WorkingRoleMutationState PreviousState,
    WorkingRoleMutationState CurrentState);
