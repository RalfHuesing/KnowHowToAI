using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Domain.Dependencies;

/// <summary>
/// Prüft die Konsistenz expliziter Content-Provenienz innerhalb eines Snapshots.
/// </summary>
public static class DependencyValidator
{
    public static ValidationReport ValidateSnapshot(
        IEnumerable<NodeContent> contents,
        IEnumerable<ContentDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(dependencies);

        var activeContents = contents.Where(content => !content.IsDeleted).ToArray();
        var allDependencies = dependencies.ToArray();
        var errors = new List<DomainError>();
        var contentsByKey = IndexActiveContents(activeContents, errors);

        ValidateContentModes(activeContents, allDependencies, errors);
        ValidateDependencyTargets(allDependencies, contentsByKey, errors);
        ValidateCycles(allDependencies, contentsByKey, errors);

        return new ValidationReport(errors, []);
    }

    /// <summary>
    /// Prüft neue oder geänderte Provenienz vor dem Speichern. Ihre Quellen müssen aktiv und explizit sein.
    /// </summary>
    public static ValidationReport ValidateNewOrChangedDependencies(
        IEnumerable<NodeContent> contents,
        IEnumerable<ContentDependency> snapshotDependencies,
        IEnumerable<ContentDependency> newOrChangedDependencies)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(snapshotDependencies);
        ArgumentNullException.ThrowIfNull(newOrChangedDependencies);

        var allContents = contents.ToArray();
        var allDependencies = snapshotDependencies.ToArray();
        var errors = ValidateSnapshot(allContents, allDependencies).Errors.ToList();
        var activeContentsByKey = allContents
            .Where(content => !content.IsDeleted)
            .GroupBy(content => (content.SnapshotId, content.NodeId, content.RoleId))
            .ToDictionary(group => group.Key, group => group.First());

        ValidateDependencySources(newOrChangedDependencies, activeContentsByKey, errors);
        return new ValidationReport(errors, []);
    }

    private static Dictionary<(SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId), NodeContent> IndexActiveContents(
        IEnumerable<NodeContent> activeContents,
        ICollection<DomainError> errors)
    {
        var contentsByKey = new Dictionary<(SnapshotId, NodeId, RoleId), NodeContent>();
        foreach (var content in activeContents)
        {
            var key = (content.SnapshotId, content.NodeId, content.RoleId);
            if (!contentsByKey.TryAdd(key, content))
                errors.Add(CreateInvalidDependencyError(
                    "Aktiver Content darf pro Snapshot, Node und Rolle nur einmal vorhanden sein.",
                    content.NodeId,
                    content.RoleId,
                    content.NodeId,
                    content.RoleId));
        }

        return contentsByKey;
    }

    private static void ValidateContentModes(
        IEnumerable<NodeContent> activeContents,
        IReadOnlyCollection<ContentDependency> dependencies,
        ICollection<DomainError> errors)
    {
        foreach (var content in activeContents)
        {
            var hasDependencies = dependencies.Any(dependency =>
                dependency.SnapshotId == content.SnapshotId
                && dependency.TargetNodeId == content.NodeId
                && dependency.TargetRoleId == content.RoleId);

            if (content.ContentMode is not (ContentMode.Independent or ContentMode.Derived))
            {
                errors.Add(CreateInvalidDependencyError(
                    "Content benötigt einen gültigen Modus Independent oder Derived.",
                    content.NodeId,
                    content.RoleId,
                    content.NodeId,
                    content.RoleId));
                continue;
            }

            if (content.ContentMode == ContentMode.Independent && hasDependencies)
            {
                errors.Add(CreateInvalidDependencyError(
                    "Unabhängiger Content darf keine Abhängigkeiten speichern.",
                    content.NodeId,
                    content.RoleId,
                    content.NodeId,
                    content.RoleId));
            }

            if (content.ContentMode == ContentMode.Derived && !hasDependencies)
            {
                errors.Add(CreateInvalidDependencyError(
                    "Abgeleiteter Content benötigt mindestens eine Abhängigkeit.",
                    content.NodeId,
                    content.RoleId,
                    content.NodeId,
                    content.RoleId));
            }
        }
    }

    private static void ValidateDependencyTargets(
        IEnumerable<ContentDependency> dependencies,
        IReadOnlyDictionary<(SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId), NodeContent> contentsByKey,
        ICollection<DomainError> errors)
    {
        foreach (var dependency in dependencies)
        {
            var targetKey = (dependency.SnapshotId, dependency.TargetNodeId, dependency.TargetRoleId);
            if (!contentsByKey.TryGetValue(targetKey, out var target)
                || target.ContentMode != ContentMode.Derived)
            {
                errors.Add(CreateInvalidDependencyError(
                    "Eine Abhängigkeit benötigt aktiven expliziten Derived-Content als Ziel.",
                    dependency.TargetNodeId,
                    dependency.TargetRoleId,
                    dependency.SourceNodeId,
                    dependency.SourceRoleId));
            }
        }
    }

    private static void ValidateDependencySources(
        IEnumerable<ContentDependency> dependencies,
        IReadOnlyDictionary<(SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId), NodeContent> contentsByKey,
        ICollection<DomainError> errors)
    {
        foreach (var dependency in dependencies)
        {
            var sourceKey = (dependency.SnapshotId, dependency.SourceNodeId, dependency.SourceRoleId);
            if (!contentsByKey.ContainsKey(sourceKey))
            {
                errors.Add(CreateInvalidDependencyError(
                    "Eine neue oder geänderte Abhängigkeit benötigt aktiven expliziten Content als Quelle.",
                    dependency.TargetNodeId,
                    dependency.TargetRoleId,
                    dependency.SourceNodeId,
                    dependency.SourceRoleId));
            }
        }
    }

    private static void ValidateCycles(
        IEnumerable<ContentDependency> dependencies,
        IReadOnlyDictionary<(SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId), NodeContent> contentsByKey,
        ICollection<DomainError> errors)
    {
        var adjacency = dependencies
            .Where(dependency => contentsByKey.ContainsKey((dependency.SnapshotId, dependency.TargetNodeId, dependency.TargetRoleId))
                && contentsByKey.ContainsKey((dependency.SnapshotId, dependency.SourceNodeId, dependency.SourceRoleId)))
            .GroupBy(dependency => (dependency.SnapshotId, dependency.TargetNodeId, dependency.TargetRoleId))
            .ToDictionary(
                group => group.Key,
                group => group.Select(dependency => (dependency.SnapshotId, dependency.SourceNodeId, dependency.SourceRoleId)).ToArray());
        var inspected = new HashSet<(SnapshotId, NodeId, RoleId)>();
        var path = new HashSet<(SnapshotId, NodeId, RoleId)>();

        foreach (var target in adjacency.Keys)
        {
            if (ContainsCycle(target, adjacency, inspected, path))
            {
                errors.Add(CreateCycleError(target.TargetNodeId, target.TargetRoleId));
                return;
            }
        }
    }

    private static bool ContainsCycle(
        (SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId) current,
        IReadOnlyDictionary<
            (SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId),
            (SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId)[]> adjacency,
        ISet<(SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId)> inspected,
        ISet<(SnapshotId SnapshotId, NodeId NodeId, RoleId RoleId)> path)
    {
        if (!path.Add(current))
            return true;

        if (adjacency.TryGetValue(current, out var sources))
        {
            foreach (var source in sources)
            {
                if (!inspected.Contains(source) && ContainsCycle(source, adjacency, inspected, path))
                    return true;
            }
        }

        path.Remove(current);
        inspected.Add(current);
        return false;
    }

    private static DomainError CreateInvalidDependencyError(
        string message,
        NodeId targetNodeId,
        RoleId targetRoleId,
        NodeId sourceNodeId,
        RoleId sourceRoleId) =>
        new(DependencyErrorCodes.InvalidDependency, message, CreateDetails(targetNodeId, targetRoleId, sourceNodeId, sourceRoleId));

    private static DomainError CreateCycleError(NodeId nodeId, RoleId roleId) =>
        new(
            DependencyErrorCodes.DependencyCycle,
            "Content-Abhängigkeiten dürfen keine direkten oder transitiven Zyklen bilden.",
            CreateDetails(nodeId, roleId, nodeId, roleId));

    private static IReadOnlyDictionary<string, string> CreateDetails(
        NodeId targetNodeId,
        RoleId targetRoleId,
        NodeId sourceNodeId,
        RoleId sourceRoleId) =>
        new Dictionary<string, string>
        {
            [DependencyErrorCodes.TargetNodeIdDetail] = targetNodeId.ToString(),
            [DependencyErrorCodes.TargetRoleIdDetail] = targetRoleId.ToString(),
            [DependencyErrorCodes.SourceNodeIdDetail] = sourceNodeId.ToString(),
            [DependencyErrorCodes.SourceRoleIdDetail] = sourceRoleId.ToString()
        };
}
