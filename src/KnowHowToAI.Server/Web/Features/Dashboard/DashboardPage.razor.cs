using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class DashboardPage
{
    private string _shellStatus = "Shell wird initialisiert.";
    private string _interactionStatus = "Interaktivität wurde noch nicht geprüft.";

    protected override async Task OnInitializedAsync()
    {
        var result = await NavigationService.ListRolesAsync(
            new ListRolesQuery(new ReadContext(), Limit: 1),
            CancellationToken.None);

        _shellStatus = result.IsSuccess
            ? result.Value!.Items.Count == 0
                ? "Shell bereit; keine Rollen vorhanden."
                : "Shell bereit."
            : "Shell bereit; der Read-only-Status konnte nicht ermittelt werden.";
    }

    private void CheckInteractivity() =>
        _interactionStatus = "Interaktivität ist verfügbar.";
}
