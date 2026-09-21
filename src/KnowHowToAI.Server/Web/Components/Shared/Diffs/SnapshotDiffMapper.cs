using KnowHowToAI.Core.Application.History;

namespace KnowHowToAI.Server.Web.Components.Shared.Diffs;

/// <summary>Mappt transportneutrale Diffs für die gemeinsame Webdarstellung.</summary>
public static class SnapshotDiffMapper
{
    public static SnapshotDiffViewModel ToViewModel(SnapshotDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var entries = new List<SnapshotDiffEntryViewModel>(
            diff.Nodes.Count + diff.Audiences.Count + diff.AudienceResolutions.Count +
            diff.Contents.Count + diff.Dependencies.Count);

        AddAudienceEntries(entries, diff.Audiences);
        AddAudienceResolutionEntries(entries, diff.AudienceResolutions);
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

    private static void AddAudienceEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<AudienceDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Audience", side!.AudienceId.Value,
                Detail: side.AudienceId.Value,
                Before: change.Before is null ? null : DescribeAudience(change.Before),
                After: change.After is null ? null : DescribeAudience(change.After)));
        }
    }

    private static void AddAudienceResolutionEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<AudienceResolutionDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "AudienceResolution", side!.RequestedAudienceId.Value, side.CandidateAudienceId.Value,
                Detail: $"{side.RequestedAudienceId} → {side.CandidateAudienceId}",
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
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Content", side!.NodeId.Value.ToString(), side.AudienceId.Value,
                Detail: $"Knoten {side.NodeId} · Zielgruppe {side.AudienceId}",
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
                $"Zielgruppe {side.TargetAudienceId} → Quell-Zielgruppe {side.SourceAudienceId}",
                change.Before is null ? null : DescribeDependency(change.Before),
                change.After is null ? null : DescribeDependency(change.After)));
        }
    }

    private static string DescribeNode(KnowHowToAI.Core.Domain.Hierarchy.Node node) =>
        $"Titel: {node.Title}; Beschreibung: {node.Description ?? "Keine"}; Position: {node.SortOrder}; Parent: {node.ParentNodeId?.Value.ToString() ?? "Root"}";

    private static string DescribeAudience(KnowHowToAI.Core.Domain.Audiences.Audience audience) =>
        $"Name: {audience.Name}; Beschreibung: {audience.Description ?? "Keine"}";

    private static string DescribeContent(KnowHowToAI.Core.Domain.Content.NodeContent content) =>
        $"Modus: {content.ContentMode}; Revision: {content.ContentRevisionId}; Inhalt: {content.ContentMd}";

    private static string DescribeDependency(KnowHowToAI.Core.Domain.Dependencies.ContentDependency dependency) =>
        $"Quelle: {dependency.SourceNodeId}/{dependency.SourceAudienceId}; Revision: {dependency.SourceContentRevisionId}";
}
