using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Übersetzt die Rollen eines Lesekontexts in die Auswahlwerte des Dialogs.
/// </summary>
public sealed class ContextSelectionRoleCatalog : IContextSelectionRoleCatalog
{
    private readonly NavigationService _navigationService;
    private readonly ILogger<ContextSelectionRoleCatalog> _logger;

    public ContextSelectionRoleCatalog(NavigationService navigationService)
        : this(navigationService, NullLogger<ContextSelectionRoleCatalog>.Instance)
    {
    }

    public ContextSelectionRoleCatalog(
        NavigationService navigationService,
        ILogger<ContextSelectionRoleCatalog> logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ContextSelectionRoleLoadResult> LoadAsync(
        ReadContext readContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = new List<ContextSelectionRoleOptionViewModel>();
            string? cursor = null;
            do
            {
                var result = await _navigationService.ListAudiencesAsync(
                    new ListAudiencesQuery(readContext, Limit: 100, Cursor: cursor),
                    cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    return new ContextSelectionRoleLoadResult(
                        [],
                        ToDiagnostic(result.Error));
                }

                var page = result.Value!;
                roles.AddRange(page.Items.Select(role => new ContextSelectionRoleOptionViewModel(
                    role.AudienceId.Value,
                    role.Name,
                    role.Description)));
                cursor = page.NextCursor;
            }
            while (cursor is not null);

            return new ContextSelectionRoleLoadResult(roles, null);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Rollen konnten für den gewählten Kontext nicht geladen werden.");
            return new ContextSelectionRoleLoadResult(
                [],
                "Rollen konnten für den gewählten Kontext nicht geladen werden.");
        }
    }

    private static string ToDiagnostic(DomainError? error) => error is null
        ? "Rollen konnten für den gewählten Kontext nicht geladen werden."
        : $"[{error.Code}] {error.Message}";
}
