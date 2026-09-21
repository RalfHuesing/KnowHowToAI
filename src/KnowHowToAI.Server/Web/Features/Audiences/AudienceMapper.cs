using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Server.Web.Features.Audiences;

/// <summary>
/// Statische Mapper-Methoden zur Überführung von Zielgruppenergebnissen in UI-ViewModels.
/// Stellt sicher, dass Domain-Typen nicht im Rendering verwendet werden und Fehler,
/// Warnungen und Cursor vollständig erhalten bleiben.
/// </summary>
public static class AudienceMapper
{
    public static AudienceItemViewModel ToAudienceItemViewModel(Audience audience)
    {
        ArgumentNullException.ThrowIfNull(audience);
        return new AudienceItemViewModel(
            audience.AudienceId.Value,
            audience.Name,
            audience.Description);
    }

    public static AudiencePageViewModel ToAudiencePageViewModel(AudiencePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var items = page.Items.Select(ToAudienceItemViewModel).ToArray();
        return new AudiencePageViewModel(items, page.NextCursor, page.ChangeVersion);
    }

    public static Result<AudiencePageViewModel> ToAudiencePageResult(Result<AudiencePage> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<AudiencePageViewModel>.Failure(result.Error!, result.Warnings);

        return Result<AudiencePageViewModel>.Success(
            result.Value is null ? null : ToAudiencePageViewModel(result.Value),
            result.Warnings);
    }

    public static string ToErrorMessage(DomainError error)
    {
        var details = error.Details.Count == 0
            ? string.Empty
            : $" ({string.Join(", ", error.Details.Select(pair => $"{pair.Key}={pair.Value}"))})";
        return $"[{error.Code}] {error.Message}{details}";
    }
}
