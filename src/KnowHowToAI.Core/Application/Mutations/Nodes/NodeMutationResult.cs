using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Transportneutrales Ergebnis einer globalen Strukturänderung.</summary>
public sealed record NodeMutationResult(
    Node Node,
    SnapshotId SnapshotId,
    long ChangeVersion,
    IReadOnlyList<NodeId> AffectedNodeIds,
    bool AppliesToAllRoles);
