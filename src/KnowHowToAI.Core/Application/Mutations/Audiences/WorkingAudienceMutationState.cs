using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Mutations.Audiences;

/// <summary>Vollständige, konsistente Sicht für eine Zielgruppenmutation.</summary>
public sealed record WorkingAudienceMutationState(
    SnapshotId SnapshotId,
    IReadOnlyList<Audience> Audiences,
    IReadOnlyList<AudienceResolution> Resolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);

public sealed record WorkingAudienceMutationDecision<T>(T Value, WorkingAudienceMutationState State);

public sealed record WorkingAudienceMutationExecution<T>(
    T Value,
    SnapshotId SnapshotId,
    long ChangeVersion,
    WorkingAudienceMutationState PreviousState,
    WorkingAudienceMutationState CurrentState);
