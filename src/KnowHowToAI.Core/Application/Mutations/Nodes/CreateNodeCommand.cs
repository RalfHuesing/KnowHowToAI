using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Beschreibt das Anlegen einer Node in einem Working Snapshot.
/// </summary>
public sealed record CreateNodeCommand(
    SnapshotId SnapshotId,
    NodeId? ParentNodeId,
    string Title,
    string? Description,
    int SortOrder);
