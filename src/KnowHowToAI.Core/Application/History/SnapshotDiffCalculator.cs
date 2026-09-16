using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Berechnet rein funktionale, deterministische Netto-Diffs zwischen zwei Snapshots.
/// </summary>
public static class SnapshotDiffCalculator
{
    public static SnapshotDiff Compute(SnapshotDiffCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rolesDiff = ComputeRolesDiff(request.BaseData.Roles, request.TargetData.Roles);
        var resolutionsDiff = ComputeRoleResolutionsDiff(request.BaseData.Resolutions, request.TargetData.Resolutions);
        var nodesDiff = ComputeNodesDiff(request.BaseData.Nodes, request.TargetData.Nodes);
        var contentsDiff = ComputeContentsDiff(request.BaseData.Contents, request.TargetData.Contents);
        var dependenciesDiff = ComputeDependenciesDiff(request.BaseData.Dependencies, request.TargetData.Dependencies);

        var totalCount = rolesDiff.Count + resolutionsDiff.Count + nodesDiff.Count + contentsDiff.Count + dependenciesDiff.Count;

        var (pagedRoles, off1, lim1) = SliceCategory(rolesDiff, request.Offset, request.Limit);
        var (pagedResolutions, off2, lim2) = SliceCategory(resolutionsDiff, off1, lim1);
        var (pagedNodes, off3, lim3) = SliceCategory(nodesDiff, off2, lim2);
        var (pagedContents, off4, lim4) = SliceCategory(contentsDiff, off3, lim3);
        var (pagedDependencies, _, _) = SliceCategory(dependenciesDiff, off4, lim4);

        var hasNext = request.Offset + request.Limit < totalCount;
        var nextCursor = hasNext
            ? new DiffCursor(request.BaseSnapshotId, request.TargetSnapshotId, request.ChangeVersion, request.Offset + request.Limit).Encode()
            : null;

        return new SnapshotDiff(
            request.BaseSnapshotId,
            request.TargetSnapshotId,
            pagedNodes,
            pagedRoles,
            pagedResolutions,
            pagedContents,
            pagedDependencies,
            nextCursor,
            totalCount);
    }

    private static (IReadOnlyList<T> Items, int RemainingOffset, int RemainingLimit) SliceCategory<T>(
        IReadOnlyList<T> list,
        int offset,
        int limit)
    {
        if (limit <= 0)
            return (Array.Empty<T>(), offset, 0);

        if (offset >= list.Count)
            return (Array.Empty<T>(), offset - list.Count, limit);

        var skip = offset;
        var take = Math.Min(limit, list.Count - skip);
        var items = list.Skip(skip).Take(take).ToArray();

        return (items, 0, limit - take);
    }

    private static IReadOnlyList<NodeDiffEntry> ComputeNodesDiff(
        IReadOnlyList<Node> baseNodes,
        IReadOnlyList<Node> targetNodes)
    {
        var baseDict = baseNodes.Where(n => !n.IsDeleted).ToDictionary(n => n.NodeId);
        var targetDict = targetNodes.Where(n => !n.IsDeleted).ToDictionary(n => n.NodeId);
        var result = new List<NodeDiffEntry>();

        foreach (var (nodeId, targetNode) in targetDict)
        {
            if (baseDict.TryGetValue(nodeId, out var baseNode))
            {
                if (targetNode.Title != baseNode.Title
                    || targetNode.Description != baseNode.Description
                    || targetNode.ParentNodeId != baseNode.ParentNodeId
                    || targetNode.SortOrder != baseNode.SortOrder)
                {
                    result.Add(new NodeDiffEntry(DiffChangeKind.Modified, baseNode, targetNode));
                }
            }
            else
            {
                result.Add(new NodeDiffEntry(DiffChangeKind.Added, null, targetNode));
            }
        }

        foreach (var (nodeId, baseNode) in baseDict)
        {
            if (!targetDict.ContainsKey(nodeId))
            {
                result.Add(new NodeDiffEntry(DiffChangeKind.Deleted, baseNode, null));
            }
        }

        return result
            .OrderBy(e => (e.After ?? e.Before)!.SortOrder)
            .ThenBy(e => (e.After ?? e.Before)!.NodeId.Value)
            .ToArray();
    }

    private static IReadOnlyList<RoleDiffEntry> ComputeRolesDiff(
        IReadOnlyList<Role> baseRoles,
        IReadOnlyList<Role> targetRoles)
    {
        var baseDict = baseRoles.Where(r => !r.IsDeleted).ToDictionary(r => r.RoleId);
        var targetDict = targetRoles.Where(r => !r.IsDeleted).ToDictionary(r => r.RoleId);
        var result = new List<RoleDiffEntry>();

        foreach (var (roleId, targetRole) in targetDict)
        {
            if (baseDict.TryGetValue(roleId, out var baseRole))
            {
                if (targetRole.Name != baseRole.Name || targetRole.Description != baseRole.Description)
                {
                    result.Add(new RoleDiffEntry(DiffChangeKind.Modified, baseRole, targetRole));
                }
            }
            else
            {
                result.Add(new RoleDiffEntry(DiffChangeKind.Added, null, targetRole));
            }
        }

        foreach (var (roleId, baseRole) in baseDict)
        {
            if (!targetDict.ContainsKey(roleId))
            {
                result.Add(new RoleDiffEntry(DiffChangeKind.Deleted, baseRole, null));
            }
        }

        return result
            .OrderBy(e => (e.After ?? e.Before)!.RoleId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<RoleResolutionDiffEntry> ComputeRoleResolutionsDiff(
        IReadOnlyList<RoleResolution> baseResolutions,
        IReadOnlyList<RoleResolution> targetResolutions)
    {
        var baseDict = baseResolutions.ToDictionary(r => (r.RequestedRoleId, r.CandidateRoleId));
        var targetDict = targetResolutions.ToDictionary(r => (r.RequestedRoleId, r.CandidateRoleId));
        var result = new List<RoleResolutionDiffEntry>();

        foreach (var (key, targetRes) in targetDict)
        {
            if (baseDict.TryGetValue(key, out var baseRes))
            {
                if (targetRes.Priority != baseRes.Priority)
                {
                    result.Add(new RoleResolutionDiffEntry(DiffChangeKind.Modified, baseRes, targetRes));
                }
            }
            else
            {
                result.Add(new RoleResolutionDiffEntry(DiffChangeKind.Added, null, targetRes));
            }
        }

        foreach (var (key, baseRes) in baseDict)
        {
            if (!targetDict.ContainsKey(key))
            {
                result.Add(new RoleResolutionDiffEntry(DiffChangeKind.Deleted, baseRes, null));
            }
        }

        return result
            .OrderBy(e => (e.After ?? e.Before)!.RequestedRoleId.Value, StringComparer.Ordinal)
            .ThenBy(e => (e.After ?? e.Before)!.CandidateRoleId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<ContentDiffEntry> ComputeContentsDiff(
        IReadOnlyList<NodeContent> baseContents,
        IReadOnlyList<NodeContent> targetContents)
    {
        var baseDict = baseContents.Where(c => !c.IsDeleted).ToDictionary(c => (c.NodeId, c.RoleId));
        var targetDict = targetContents.Where(c => !c.IsDeleted).ToDictionary(c => (c.NodeId, c.RoleId));
        var result = new List<ContentDiffEntry>();

        foreach (var (key, targetContent) in targetDict)
        {
            if (baseDict.TryGetValue(key, out var baseContent))
            {
                if (targetContent.ContentRevisionId != baseContent.ContentRevisionId
                    || targetContent.ContentMode != baseContent.ContentMode
                    || targetContent.ContentMd != baseContent.ContentMd)
                {
                    result.Add(new ContentDiffEntry(DiffChangeKind.Modified, baseContent, targetContent));
                }
            }
            else
            {
                result.Add(new ContentDiffEntry(DiffChangeKind.Added, null, targetContent));
            }
        }

        foreach (var (key, baseContent) in baseDict)
        {
            if (!targetDict.ContainsKey(key))
            {
                result.Add(new ContentDiffEntry(DiffChangeKind.Deleted, baseContent, null));
            }
        }

        return result
            .OrderBy(e => (e.After ?? e.Before)!.NodeId.Value)
            .ThenBy(e => (e.After ?? e.Before)!.RoleId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<DependencyDiffEntry> ComputeDependenciesDiff(
        IReadOnlyList<ContentDependency> baseDependencies,
        IReadOnlyList<ContentDependency> targetDependencies)
    {
        var baseDict = baseDependencies.ToDictionary(d => (d.TargetNodeId, d.TargetRoleId, d.SourceNodeId, d.SourceRoleId));
        var targetDict = targetDependencies.ToDictionary(d => (d.TargetNodeId, d.TargetRoleId, d.SourceNodeId, d.SourceRoleId));
        var result = new List<DependencyDiffEntry>();

        foreach (var (key, targetDep) in targetDict)
        {
            if (baseDict.TryGetValue(key, out var baseDep))
            {
                if (targetDep.SourceContentRevisionId != baseDep.SourceContentRevisionId)
                {
                    result.Add(new DependencyDiffEntry(DiffChangeKind.Modified, baseDep, targetDep));
                }
            }
            else
            {
                result.Add(new DependencyDiffEntry(DiffChangeKind.Added, null, targetDep));
            }
        }

        foreach (var (key, baseDep) in baseDict)
        {
            if (!targetDict.ContainsKey(key))
            {
                result.Add(new DependencyDiffEntry(DiffChangeKind.Deleted, baseDep, null));
            }
        }

        return result
            .OrderBy(e => (e.After ?? e.Before)!.TargetNodeId.Value)
            .ThenBy(e => (e.After ?? e.Before)!.TargetRoleId.Value, StringComparer.Ordinal)
            .ThenBy(e => (e.After ?? e.Before)!.SourceNodeId.Value)
            .ThenBy(e => (e.After ?? e.Before)!.SourceRoleId.Value, StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>Kapselt alle Daten eines Snapshots für die Diff-Berechnung.</summary>
public sealed record SnapshotData(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Role> Roles,
    IReadOnlyList<RoleResolution> Resolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);

/// <summary>Parameter für die Diff-Berechnung mit Paginierungsgrenzen.</summary>
public sealed record SnapshotDiffCalculationRequest(
    SnapshotId BaseSnapshotId,
    SnapshotId TargetSnapshotId,
    SnapshotData BaseData,
    SnapshotData TargetData,
    int Limit,
    int Offset,
    long? ChangeVersion = null);
