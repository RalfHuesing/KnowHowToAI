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

    public static SnapshotDiffViewModel ToSnapshotDiffViewModel(SnapshotDiff diff)
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

    public static Result<SnapshotDiffViewModel> ToSnapshotDiffResult(Result<SnapshotDiff> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<SnapshotDiffViewModel>.Failure(result.Error!, result.Warnings);

        return Result<SnapshotDiffViewModel>.Success(
            result.Value is null ? null : ToSnapshotDiffViewModel(result.Value),
            result.Warnings);
    }

    private static IReadOnlyList<SnapshotDiffEntryViewModel> ToFlatEntries(SnapshotDiff diff)
    {
        var entries = new List<SnapshotDiffEntryViewModel>(
            diff.Nodes.Count + diff.Roles.Count + diff.RoleResolutions.Count +
            diff.Contents.Count + diff.Dependencies.Count);

        foreach (var e in diff.Roles)
        {
            var side = e.After ?? e.Before;
            entries.Add(new SnapshotDiffEntryViewModel(
                e.Kind.ToString(),
                "Role",
                side!.RoleId.Value));
        }

        foreach (var e in diff.RoleResolutions)
        {
            var side = e.After ?? e.Before;
            entries.Add(new SnapshotDiffEntryViewModel(
                e.Kind.ToString(),
                "RoleResolution",
                side!.RequestedRoleId.Value,
                side.CandidateRoleId.Value));
        }

        foreach (var e in diff.Nodes)
        {
            var side = e.After ?? e.Before;
            entries.Add(new SnapshotDiffEntryViewModel(
                e.Kind.ToString(),
                "Node",
                side!.NodeId.Value.ToString(),
                Detail: side.Title));
        }

        foreach (var e in diff.Contents)
        {
            var side = e.After ?? e.Before;
            entries.Add(new SnapshotDiffEntryViewModel(
                e.Kind.ToString(),
                "Content",
                side!.NodeId.Value.ToString(),
                side.RoleId.Value));
        }

        foreach (var e in diff.Dependencies)
        {
            var side = e.After ?? e.Before;
            entries.Add(new SnapshotDiffEntryViewModel(
                e.Kind.ToString(),
                "Dependency",
                side!.TargetNodeId.Value.ToString(),
                side.SourceNodeId.Value.ToString(),
                $"{side.TargetRoleId} -> {side.SourceRoleId}"));
        }

        return entries;
    }
}
