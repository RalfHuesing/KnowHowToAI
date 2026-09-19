using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>
/// Statische Mapper-Methoden zur Überführung von Suchergebnissen in UI-ViewModels.
/// Stellt sicher, dass Domain-Typen nicht im Rendering verwendet werden und Fehler,
/// Warnungen und Cursor vollständig erhalten bleiben.
/// </summary>
public static class SearchMapper
{
    public static SearchHitViewModel ToSearchHitViewModel(SearchHit hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return new SearchHitViewModel(
            hit.NodeId.Value,
            hit.Title,
            hit.Description,
            hit.Snippet,
            hit.HitField,
            hit.Availability.ToString(),
            hit.ResolvedRoleId?.Value,
            hit.Freshness.ToString(),
            hit.SortOrder,
            [hit.Title]);
    }

    public static SearchPageViewModel ToSearchPageViewModel(SearchResultPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var items = page.Items.Select(ToSearchHitViewModel).ToArray();
        return new SearchPageViewModel(page.Query, items, page.NextCursor);
    }

    public static Result<SearchPageViewModel> ToSearchPageResult(Result<SearchResultPage> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<SearchPageViewModel>.Failure(result.Error!, result.Warnings);

        return Result<SearchPageViewModel>.Success(
            result.Value is null ? null : ToSearchPageViewModel(result.Value),
            result.Warnings);
    }
}
