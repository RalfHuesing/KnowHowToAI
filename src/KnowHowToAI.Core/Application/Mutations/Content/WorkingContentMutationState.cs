using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>Vollständige, konsistente Sicht für eine Content-Mutation.</summary>
public sealed record WorkingContentMutationState(
    SnapshotId SnapshotId,
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Audience> Audiences,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);

public sealed record WorkingContentMutationDecision<T>(T Value, WorkingContentMutationState State);

public sealed record WorkingContentMutationExecution<T>(
    T Value,
    SnapshotId SnapshotId,
    long ChangeVersion,
    WorkingContentMutationState PreviousState,
    WorkingContentMutationState CurrentState);
