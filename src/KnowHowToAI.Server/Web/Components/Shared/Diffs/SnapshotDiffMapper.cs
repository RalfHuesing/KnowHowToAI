using KnowHowToAI.Core.Application.History;

namespace KnowHowToAI.Server.Web.Components.Shared.Diffs;

/// <summary>Mappt transportneutrale Diffs für die gemeinsame Webdarstellung.</summary>
public static class SnapshotDiffMapper
{
    public static SnapshotDiffViewModel ToViewModel(SnapshotDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var entries = new List<SnapshotDiffEntryViewModel>(
            diff.Nodes.Count + diff.Roles.Count + diff.RoleResolutions.Count +
            diff.Contents.Count + diff.Dependencies.Count);

        AddRoleEntries(entries, diff.Roles);
        AddRoleResolutionEntries(entries, diff.RoleResolutions);
        AddNodeEntries(entries, diff.Nodes);
        AddContentEntries(entries, diff.Contents);
        AddDependencyEntries(entries, diff.Dependencies);

        return new SnapshotDiffViewModel(
            diff.BaseSnapshotId.Value,
            diff.TargetSnapshotId.Value,
            diff.TotalCount,
            entries,
            diff.NextCursor);
    }

    private static void AddRoleEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<RoleDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Role", side!.RoleId.Value,
                Detail: side.RoleId.Value,
                Before: change.Before is null ? null : DescribeRole(change.Before),
                After: change.After is null ? null : DescribeRole(change.After)));
        }
    }

    private static void AddRoleResolutionEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<RoleResolutionDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "RoleResolution", side!.RequestedRoleId.Value, side.CandidateRoleId.Value,
                Detail: $"{side.RequestedRoleId} → {side.CandidateRoleId}",
                Before: change.Before is null ? null : $"Priorität: {change.Before.Priority}",
                After: change.After is null ? null : $"Priorität: {change.After.Priority}"));
        }
    }

    private static void AddNodeEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<NodeDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Node", side!.NodeId.Value.ToString(), Detail: side.Title,
                Before: change.Before is null ? null : DescribeNode(change.Before),
                After: change.After is null ? null : DescribeNode(change.After)));
        }
    }

    private static void AddContentEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<ContentDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Content", side!.NodeId.Value.ToString(), side.RoleId.Value,
                Detail: $"Knoten {side.NodeId} · Rolle {side.RoleId}",
                Before: change.Before is null ? null : DescribeContent(change.Before),
                After: change.After is null ? null : DescribeContent(change.After)));
        }
    }

    private static void AddDependencyEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<DependencyDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Dependency", side!.TargetNodeId.Value.ToString(), side.SourceNodeId.Value.ToString(),
                $"Zielrolle {side.TargetRoleId} → Quellrolle {side.SourceRoleId}",
                change.Before is null ? null : DescribeDependency(change.Before),
                change.After is null ? null : DescribeDependency(change.After)));
        }
    }

    private static string DescribeNode(KnowHowToAI.Core.Domain.Hierarchy.Node node) =>
        $"Titel: {node.Title}; Beschreibung: {node.Description ?? "Keine"}; Position: {node.SortOrder}; Parent: {node.ParentNodeId?.Value.ToString() ?? "Root"}";

    private static string DescribeRole(KnowHowToAI.Core.Domain.Roles.Role role) =>
        $"Name: {role.Name}; Beschreibung: {role.Description ?? "Keine"}";

    private static string DescribeContent(KnowHowToAI.Core.Domain.Content.NodeContent content) =>
        $"Modus: {content.ContentMode}; Revision: {content.ContentRevisionId}; Inhalt: {content.ContentMd}";

    private static string DescribeDependency(KnowHowToAI.Core.Domain.Dependencies.ContentDependency dependency) =>
        $"Quelle: {dependency.SourceNodeId}/{dependency.SourceRoleId}; Revision: {dependency.SourceContentRevisionId}";
}
