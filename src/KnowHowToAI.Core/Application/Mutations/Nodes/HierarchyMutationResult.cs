using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Enthält die geänderte Node und den vollständig normalisierten Snapshot-Zustand.
/// </summary>
public sealed record HierarchyMutationResult(Node ChangedNode, IReadOnlyList<Node> Nodes);
