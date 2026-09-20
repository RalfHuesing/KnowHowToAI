using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Domain.Dependencies;

/// <summary>
/// Wertet den aktuellen, historischen oder Working-Snapshot anhand derselben Provenienzregeln aus.
/// </summary>
public static class FreshnessEvaluator
{
    public static Freshness Evaluate(
        NodeContent content,
        IEnumerable<NodeContent> contents,
        IEnumerable<ContentDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(dependencies);

        return Evaluate(
            content,
            contents.ToArray(),
            dependencies.ToArray(),
            new HashSet<(SnapshotId SnapshotId, NodeId NodeId, AudienceId AudienceId)>());
    }

    private static Freshness Evaluate(
        NodeContent content,
        IReadOnlyCollection<NodeContent> contents,
        IReadOnlyCollection<ContentDependency> dependencies,
        ISet<(SnapshotId SnapshotId, NodeId NodeId, AudienceId AudienceId)> path)
    {
        if (content.IsDeleted || content.ContentMode is not (ContentMode.Independent or ContentMode.Derived))
            return Freshness.Stale;

        if (content.ContentMode == ContentMode.Independent)
            return Freshness.Current;

        var contentKey = (content.SnapshotId, content.NodeId, content.AudienceId);
        if (!path.Add(contentKey))
            return Freshness.Stale;

        var freshness = AreSourcesCurrent(content, contents, dependencies, path)
            ? Freshness.Current
            : Freshness.Stale;
        path.Remove(contentKey);
        return freshness;
    }

    private static bool AreSourcesCurrent(
        NodeContent content,
        IReadOnlyCollection<NodeContent> contents,
        IReadOnlyCollection<ContentDependency> dependencies,
        ISet<(SnapshotId SnapshotId, NodeId NodeId, AudienceId AudienceId)> path)
    {
        var sourceDependencies = dependencies
            .Where(dependency => dependency.SnapshotId == content.SnapshotId
                && dependency.TargetNodeId == content.NodeId
                && dependency.TargetAudienceId == content.AudienceId)
            .ToArray();
        if (sourceDependencies.Length == 0)
            return false;

        return sourceDependencies.All(dependency => IsSourceCurrent(content, dependency, contents, dependencies, path));
    }

    private static bool IsSourceCurrent(
        NodeContent target,
        ContentDependency dependency,
        IReadOnlyCollection<NodeContent> contents,
        IReadOnlyCollection<ContentDependency> dependencies,
        ISet<(SnapshotId SnapshotId, NodeId NodeId, AudienceId AudienceId)> path)
    {
        var sources = contents
            .Where(source => source.SnapshotId == target.SnapshotId
                && source.NodeId == dependency.SourceNodeId
                && source.AudienceId == dependency.SourceAudienceId
                && !source.IsDeleted)
            .ToArray();

        return sources.Length == 1
            && sources[0].ContentRevisionId == dependency.SourceContentRevisionId
            && Evaluate(sources[0], contents, dependencies, path) == Freshness.Current;
    }
}
