using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Übersetzt die Rollen eines Lesekontexts in die Auswahlwerte des Dialogs.
/// </summary>
public sealed class ContextSelectionRoleCatalog : IContextSelectionRoleCatalog
{
    private readonly NavigationService _navigationService;

    public ContextSelectionRoleCatalog(NavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public async Task<ContextSelectionRoleLoadResult> LoadAsync(
        ReadContext readContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _navigationService.ListRolesAsync(
                new ListRolesQuery(readContext, Limit: 100),
                cancellationToken);

            if (!result.IsSuccess)
            {
                return new ContextSelectionRoleLoadResult(
                    [],
                    result.Error?.Message ?? "Rollen konnten für den gewählten Kontext nicht geladen werden.");
            }

            var roles = result.Value?.Items.Select(role => new ContextSelectionRoleOptionViewModel(
                role.RoleId.Value,
                role.Name,
                role.Description)).ToArray() ?? [];

            return new ContextSelectionRoleLoadResult(roles, null);
        }
        catch (Exception)
        {
            return new ContextSelectionRoleLoadResult(
                [],
                "Rollen konnten für den gewählten Kontext nicht geladen werden.");
        }
    }
}
