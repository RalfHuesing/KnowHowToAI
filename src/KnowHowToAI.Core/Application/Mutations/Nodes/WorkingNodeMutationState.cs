using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Vollständige, unter einer Working-Snapshot-Sperre gelesene Node-Mutationsansicht.</summary>
public sealed record WorkingNodeMutationState(
    SnapshotId SnapshotId,
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies,
    IReadOnlyList<NodeId> KnownNodeIds);

/// <summary>Fachliche Entscheidung und der daraus resultierende Snapshot-Zustand.</summary>
public sealed record WorkingNodeMutationDecision<T>(T Value, WorkingNodeMutationState State);

/// <summary>Atomar persistiertes Ergebnis einer Node-Mutation.</summary>
public sealed record WorkingNodeMutationExecution<T>(
    T Value,
    SnapshotId SnapshotId,
    long ChangeVersion,
    WorkingNodeMutationState PreviousState,
    WorkingNodeMutationState CurrentState);
