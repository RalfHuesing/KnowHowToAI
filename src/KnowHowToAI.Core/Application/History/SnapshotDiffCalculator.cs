using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Berechnet rein funktionale, deterministische Netto-Diffs zwischen zwei Snapshots.
/// </summary>
public static class SnapshotDiffCalculator
{
    public static SnapshotDiff Compute(SnapshotDiffCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var audiencesDiff = ComputeAudiencesDiff(request.BaseData.Audiences, request.TargetData.Audiences);
        var resolutionsDiff = ComputeAudienceResolutionsDiff(request.BaseData.Resolutions, request.TargetData.Resolutions);
        var nodesDiff = ComputeNodesDiff(request.BaseData.Nodes, request.TargetData.Nodes);
        var contentsDiff = ComputeContentsDiff(request.BaseData.Contents, request.TargetData.Contents);
        var dependenciesDiff = ComputeDependenciesDiff(request.BaseData.Dependencies, request.TargetData.Dependencies);

        if (request.FilterNodeId is { } filterNodeId)
        {
            nodesDiff = nodesDiff.Where(entry => (entry.After ?? entry.Before)!.NodeId == filterNodeId).ToArray();
            contentsDiff = contentsDiff.Where(entry => (entry.After ?? entry.Before)!.NodeId == filterNodeId).ToArray();
            dependenciesDiff = dependenciesDiff.Where(entry =>
            {
                var dependency = entry.After ?? entry.Before;
                return dependency!.TargetNodeId == filterNodeId || dependency.SourceNodeId == filterNodeId;
            }).ToArray();
            audiencesDiff = Array.Empty<AudienceDiffEntry>();
            resolutionsDiff = Array.Empty<AudienceResolutionDiffEntry>();
        }

        var totalCount = audiencesDiff.Count + resolutionsDiff.Count + nodesDiff.Count + contentsDiff.Count + dependenciesDiff.Count;

        var (pagedAudiences, off1, lim1) = SliceCategory(audiencesDiff, request.Offset, request.Limit);
        var (pagedResolutions, off2, lim2) = SliceCategory(resolutionsDiff, off1, lim1);
        var (pagedNodes, off3, lim3) = SliceCategory(nodesDiff, off2, lim2);
        var (pagedContents, off4, lim4) = SliceCategory(contentsDiff, off3, lim3);
        var (pagedDependencies, _, _) = SliceCategory(dependenciesDiff, off4, lim4);

        var hasNext = request.Offset + request.Limit < totalCount;
        var nextCursor = hasNext
            ? new DiffCursor(
                request.BaseSnapshotId,
                request.TargetSnapshotId,
                request.ChangeVersion,
                request.Offset + request.Limit,
                request.FilterNodeId).Encode()
            : null;

        return new SnapshotDiff(
            request.BaseSnapshotId,
            request.TargetSnapshotId,
            pagedNodes,
            pagedAudiences,
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

    private static IReadOnlyList<AudienceDiffEntry> ComputeAudiencesDiff(
        IReadOnlyList<Audience> baseAudiences,
        IReadOnlyList<Audience> targetAudiences)
    {
        var baseDict = baseAudiences.Where(r => !r.IsDeleted).ToDictionary(r => r.AudienceId);
        var targetDict = targetAudiences.Where(r => !r.IsDeleted).ToDictionary(r => r.AudienceId);
        var result = new List<AudienceDiffEntry>();

        foreach (var (audienceId, targetAudience) in targetDict)
        {
            if (baseDict.TryGetValue(audienceId, out var baseAudience))
            {
                if (targetAudience.Name != baseAudience.Name || targetAudience.Description != baseAudience.Description)
                {
                    result.Add(new AudienceDiffEntry(DiffChangeKind.Modified, baseAudience, targetAudience));
                }
            }
            else
            {
                result.Add(new AudienceDiffEntry(DiffChangeKind.Added, null, targetAudience));
            }
        }

        foreach (var (audienceId, baseAudience) in baseDict)
        {
            if (!targetDict.ContainsKey(audienceId))
            {
                result.Add(new AudienceDiffEntry(DiffChangeKind.Deleted, baseAudience, null));
            }
        }

        return result
            .OrderBy(e => (e.After ?? e.Before)!.AudienceId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<AudienceResolutionDiffEntry> ComputeAudienceResolutionsDiff(
        IReadOnlyList<AudienceResolution> baseResolutions,
        IReadOnlyList<AudienceResolution> targetResolutions)
    {
        var baseDict = baseResolutions.ToDictionary(r => (r.RequestedAudienceId, r.CandidateAudienceId));
        var targetDict = targetResolutions.ToDictionary(r => (r.RequestedAudienceId, r.CandidateAudienceId));
        var result = new List<AudienceResolutionDiffEntry>();

        foreach (var (key, targetRes) in targetDict)
        {
            if (baseDict.TryGetValue(key, out var baseRes))
            {
                if (targetRes.Priority != baseRes.Priority)
                {
                    result.Add(new AudienceResolutionDiffEntry(DiffChangeKind.Modified, baseRes, targetRes));
                }
            }
            else
            {
                result.Add(new AudienceResolutionDiffEntry(DiffChangeKind.Added, null, targetRes));
            }
        }

        foreach (var (key, baseRes) in baseDict)
        {
            if (!targetDict.ContainsKey(key))
            {
                result.Add(new AudienceResolutionDiffEntry(DiffChangeKind.Deleted, baseRes, null));
            }
        }

        return result
            .OrderBy(e => (e.After ?? e.Before)!.RequestedAudienceId.Value, StringComparer.Ordinal)
            .ThenBy(e => (e.After ?? e.Before)!.CandidateAudienceId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<ContentDiffEntry> ComputeContentsDiff(
        IReadOnlyList<NodeContent> baseContents,
        IReadOnlyList<NodeContent> targetContents)
    {
        var baseDict = baseContents.Where(c => !c.IsDeleted).ToDictionary(c => (c.NodeId, c.AudienceId));
        var targetDict = targetContents.Where(c => !c.IsDeleted).ToDictionary(c => (c.NodeId, c.AudienceId));
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
            .ThenBy(e => (e.After ?? e.Before)!.AudienceId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<DependencyDiffEntry> ComputeDependenciesDiff(
        IReadOnlyList<ContentDependency> baseDependencies,
        IReadOnlyList<ContentDependency> targetDependencies)
    {
        var baseDict = baseDependencies.ToDictionary(d => (d.TargetNodeId, d.TargetAudienceId, d.SourceNodeId, d.SourceAudienceId));
        var targetDict = targetDependencies.ToDictionary(d => (d.TargetNodeId, d.TargetAudienceId, d.SourceNodeId, d.SourceAudienceId));
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
            .ThenBy(e => (e.After ?? e.Before)!.TargetAudienceId.Value, StringComparer.Ordinal)
            .ThenBy(e => (e.After ?? e.Before)!.SourceNodeId.Value)
            .ThenBy(e => (e.After ?? e.Before)!.SourceAudienceId.Value, StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>Kapselt alle Daten eines Snapshots für die Diff-Berechnung.</summary>
public sealed record SnapshotData(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Audience> Audiences,
    IReadOnlyList<AudienceResolution> Resolutions,
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
    long? ChangeVersion = null,
    NodeId? FilterNodeId = null);
