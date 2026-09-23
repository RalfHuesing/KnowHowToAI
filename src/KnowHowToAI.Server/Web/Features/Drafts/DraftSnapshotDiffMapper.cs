using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Shared.Diffs;

namespace KnowHowToAI.Server.Web.Features.Drafts;

internal static class DraftSnapshotDiffMapper
{
    public static Result<SnapshotDiffViewModel> ToResult(Result<SnapshotDiff> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<SnapshotDiffViewModel>.Failure(result.Error!, result.Warnings);

        return Result<SnapshotDiffViewModel>.Success(
            result.Value is null ? null : SnapshotDiffMapper.ToViewModel(result.Value),
            result.Warnings);
    }
}
