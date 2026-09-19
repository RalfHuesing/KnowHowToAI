using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>
/// Statische Mapper-Methoden zur Überführung von Historien- und Release-Ergebnissen in UI-ViewModels.
/// Stellt sicher, dass Domain-Typen nicht im Rendering verwendet werden und Fehler,
/// Warnungen und Cursor vollständig erhalten bleiben.
/// </summary>
public static class HistoryMapper
{
    public static SnapshotViewModel ToSnapshotViewModel(Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new SnapshotViewModel(
            snapshot.SnapshotId.Value,
            snapshot.State.ToString(),
            snapshot.CreatedAtUtc,
            snapshot.BaseSnapshotId?.Value,
            snapshot.CommittedAtUtc);
    }

    public static ReleaseItemViewModel ToReleaseItemViewModel(Release release)
    {
        ArgumentNullException.ThrowIfNull(release);
        return new ReleaseItemViewModel(
            release.ReleaseId.Value,
            release.SnapshotId.Value,
            release.Name,
            release.Description,
            release.ReleasedAtUtc);
    }

    public static ReleasePageViewModel ToReleasePageViewModel(ReleasePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var items = page.Items.Select(ToReleaseItemViewModel).ToArray();
        return new ReleasePageViewModel(items, page.NextCursor);
    }

    public static SnapshotPageViewModel ToSnapshotPageViewModel(SnapshotPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return new SnapshotPageViewModel(page.Items.Select(ToSnapshotViewModel).ToArray(), page.NextCursor);
    }

    public static SnapshotDiffViewModel ToSnapshotDiffViewModel(global::KnowHowToAI.Core.Application.History.SnapshotDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var entries = ToFlatEntries(diff);
        return new SnapshotDiffViewModel(
            diff.BaseSnapshotId.Value,
            diff.TargetSnapshotId.Value,
            diff.TotalCount,
            entries,
            diff.NextCursor);
    }

    public static Result<SnapshotViewModel> ToSnapshotResult(Result<Snapshot> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<SnapshotViewModel>.Failure(result.Error!, result.Warnings);

        return Result<SnapshotViewModel>.Success(
            result.Value is null ? null : ToSnapshotViewModel(result.Value),
            result.Warnings);
    }

    public static Result<ReleasePageViewModel> ToReleasePageResult(Result<ReleasePage> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<ReleasePageViewModel>.Failure(result.Error!, result.Warnings);

        return Result<ReleasePageViewModel>.Success(
            result.Value is null ? null : ToReleasePageViewModel(result.Value),
            result.Warnings);
    }

    public static Result<SnapshotPageViewModel> ToSnapshotPageResult(Result<SnapshotPage> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<SnapshotPageViewModel>.Failure(result.Error!, result.Warnings);

        return Result<SnapshotPageViewModel>.Success(
            result.Value is null ? null : ToSnapshotPageViewModel(result.Value),
            result.Warnings);
    }

    public static Result<SnapshotDiffViewModel> ToSnapshotDiffResult(Result<global::KnowHowToAI.Core.Application.History.SnapshotDiff> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<SnapshotDiffViewModel>.Failure(result.Error!, result.Warnings);

        return Result<SnapshotDiffViewModel>.Success(
            result.Value is null ? null : ToSnapshotDiffViewModel(result.Value),
            result.Warnings);
    }

    private static IReadOnlyList<SnapshotDiffEntryViewModel> ToFlatEntries(global::KnowHowToAI.Core.Application.History.SnapshotDiff diff)
    {
        var entries = new List<SnapshotDiffEntryViewModel>(
            diff.Nodes.Count + diff.Roles.Count + diff.RoleResolutions.Count +
            diff.Contents.Count + diff.Dependencies.Count);

        AddRoleEntries(entries, diff.Roles);
        AddRoleResolutionEntries(entries, diff.RoleResolutions);
        AddNodeEntries(entries, diff.Nodes);
        AddContentEntries(entries, diff.Contents);
        AddDependencyEntries(entries, diff.Dependencies);

        return entries;
    }

    private static void AddRoleEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<RoleDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Role", side!.RoleId.Value,
                Detail: side.RoleId.Value, Before: change.Before is null ? null : $"Name: {change.Before.Name}", After: change.After is null ? null : $"Name: {change.After.Name}"));
        }
    }

    private static void AddRoleResolutionEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<RoleResolutionDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "RoleResolution", side!.RequestedRoleId.Value, side.CandidateRoleId.Value,
                Detail: $"{side.RequestedRoleId} → {side.CandidateRoleId}", Before: change.Before is null ? null : $"Priorität: {change.Before.Priority}", After: change.After is null ? null : $"Priorität: {change.After.Priority}"));
        }
    }

    private static void AddNodeEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<NodeDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Node", side!.NodeId.Value.ToString(), Detail: side.Title,
                Before: change.Before is null ? null : DescribeNode(change.Before), After: change.After is null ? null : DescribeNode(change.After)));
        }
    }

    private static void AddContentEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<ContentDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Content", side!.NodeId.Value.ToString(), side.RoleId.Value,
                Detail: $"Knoten {side.NodeId} · Rolle {side.RoleId}", Before: change.Before is null ? null : DescribeContent(change.Before), After: change.After is null ? null : DescribeContent(change.After)));
        }
    }

    private static void AddDependencyEntries(List<SnapshotDiffEntryViewModel> entries, IReadOnlyList<DependencyDiffEntry> changes)
    {
        foreach (var change in changes)
        {
            var side = change.After ?? change.Before;
            entries.Add(new SnapshotDiffEntryViewModel(change.Kind.ToString(), "Dependency", side!.TargetNodeId.Value.ToString(), side.SourceNodeId.Value.ToString(),
                $"Zielrolle {side.TargetRoleId} → Quellrolle {side.SourceRoleId}", change.Before is null ? null : DescribeDependency(change.Before), change.After is null ? null : DescribeDependency(change.After)));
        }
    }

    private static string DescribeNode(KnowHowToAI.Core.Domain.Hierarchy.Node node) =>
        $"Titel: {node.Title}; Position: {node.SortOrder}; Parent: {node.ParentNodeId?.Value.ToString() ?? "Root"}";

    private static string DescribeContent(KnowHowToAI.Core.Domain.Content.NodeContent content) =>
        $"Modus: {content.ContentMode}; Revision: {content.ContentRevisionId}";

    private static string DescribeDependency(KnowHowToAI.Core.Domain.Dependencies.ContentDependency dependency) =>
        $"Quelle: {dependency.SourceNodeId}/{dependency.SourceRoleId}; Revision: {dependency.SourceContentRevisionId}";
}
