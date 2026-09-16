using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Netto-Diff zwischen zwei committed Snapshots fuer compare_snapshots.
/// Enthalt ausschliesslich die geaenderten Eintraege, keine Rekonstruktion eines Operation Logs.
/// </summary>
public sealed record SnapshotDiff(
    SnapshotId BaseSnapshotId,
    SnapshotId TargetSnapshotId,
    IReadOnlyList<NodeDiffEntry> Nodes,
    IReadOnlyList<RoleDiffEntry> Roles,
    IReadOnlyList<RoleResolutionDiffEntry> RoleResolutions,
    IReadOnlyList<ContentDiffEntry> Contents,
    IReadOnlyList<DependencyDiffEntry> Dependencies,
    string? NextCursor = null,
    int TotalCount = 0);

public sealed record NodeDiffEntry(DiffChangeKind Kind, Node? Before, Node? After);
public sealed record RoleDiffEntry(DiffChangeKind Kind, Role? Before, Role? After);
public sealed record RoleResolutionDiffEntry(DiffChangeKind Kind, RoleResolution? Before, RoleResolution? After);
public sealed record ContentDiffEntry(DiffChangeKind Kind, NodeContent? Before, NodeContent? After);
public sealed record DependencyDiffEntry(DiffChangeKind Kind, ContentDependency? Before, ContentDependency? After);

/// <summary>Art der Aenderung in einem Diff-Eintrag.</summary>
public enum DiffChangeKind
{
    Added = 1,
    Modified = 2,
    Deleted = 3
}
