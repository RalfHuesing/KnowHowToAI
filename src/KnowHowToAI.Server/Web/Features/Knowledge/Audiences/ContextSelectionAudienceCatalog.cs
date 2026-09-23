using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Audiences;

/// <summary>
/// Übersetzt die Zielgruppen eines Lesekontexts in die Auswahlwerte des Dialogs.
/// </summary>
public sealed class ContextSelectionAudienceCatalog : IContextSelectionAudienceCatalog
{
    private readonly NavigationService _navigationService;
    private readonly ILogger<ContextSelectionAudienceCatalog> _logger;

    public ContextSelectionAudienceCatalog(NavigationService navigationService)
        : this(navigationService, NullLogger<ContextSelectionAudienceCatalog>.Instance)
    {
    }

    public ContextSelectionAudienceCatalog(
        NavigationService navigationService,
        ILogger<ContextSelectionAudienceCatalog> logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ContextSelectionAudienceLoadResult> LoadAsync(
        ReadContext readContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var audiences = new List<ContextSelectionAudienceOptionViewModel>();
            string? cursor = null;
            do
            {
                var result = await _navigationService.ListAudiencesAsync(
                    new ListAudiencesQuery(readContext, Limit: 100, Cursor: cursor),
                    cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    return new ContextSelectionAudienceLoadResult(
                        [],
                        ToDiagnostic(result.Error));
                }

                var page = result.Value!;
                audiences.AddRange(page.Items.Select(audience => new ContextSelectionAudienceOptionViewModel(
                    audience.AudienceId.Value,
                    audience.Name,
                    audience.Description)));
                cursor = page.NextCursor;
            }
            while (cursor is not null);

            return new ContextSelectionAudienceLoadResult(audiences, null);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Zielgruppen konnten für den gewählten Kontext nicht geladen werden.");
            return new ContextSelectionAudienceLoadResult(
                [],
                "Zielgruppen konnten für den gewählten Kontext nicht geladen werden.");
        }
    }

    private static string ToDiagnostic(DomainError? error) => error is null
        ? "Zielgruppen konnten für den gewählten Kontext nicht geladen werden."
        : $"[{error.Code}] {error.Message}";
}
