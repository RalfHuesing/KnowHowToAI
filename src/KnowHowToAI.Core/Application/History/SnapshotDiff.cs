using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Netto-Diff zwischen zwei committed Snapshots fuer compare_snapshots.
/// Enthalt ausschliesslich die geaenderten Eintraege, keine Rekonstruktion eines Operation Logs.
/// </summary>
public sealed record SnapshotDiff(
    SnapshotId BaseSnapshotId,
    SnapshotId TargetSnapshotId,
    IReadOnlyList<NodeDiffEntry> Nodes,
    IReadOnlyList<AudienceDiffEntry> Audiences,
    IReadOnlyList<AudienceResolutionDiffEntry> AudienceResolutions,
    IReadOnlyList<ContentDiffEntry> Contents,
    IReadOnlyList<DependencyDiffEntry> Dependencies,
    string? NextCursor = null,
    int TotalCount = 0);

public sealed record NodeDiffEntry(DiffChangeKind Kind, Node? Before, Node? After);
public sealed record AudienceDiffEntry(DiffChangeKind Kind, Audience? Before, Audience? After);
public sealed record AudienceResolutionDiffEntry(DiffChangeKind Kind, AudienceResolution? Before, AudienceResolution? After);
public sealed record ContentDiffEntry(DiffChangeKind Kind, NodeContent? Before, NodeContent? After);
public sealed record DependencyDiffEntry(DiffChangeKind Kind, ContentDependency? Before, ContentDependency? After);

/// <summary>Art der Aenderung in einem Diff-Eintrag.</summary>
public enum DiffChangeKind
{
    Added = 1,
    Modified = 2,
    Deleted = 3
}
