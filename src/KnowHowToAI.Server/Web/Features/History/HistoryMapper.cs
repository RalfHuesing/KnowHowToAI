using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Shared.Diffs;

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
            snapshot.CommittedAtUtc,
            snapshot.CommitMetadata?.TransactionId.Value,
            snapshot.CommitMetadata?.Actor,
            snapshot.CommitMetadata?.Client,
            snapshot.CommitMetadata?.Purpose,
            snapshot.CommitMetadata?.CommitMessage);
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
        return SnapshotDiffMapper.ToViewModel(diff);
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

}
