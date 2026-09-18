using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.Components.Layout;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class DashboardPage
{
    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    private string _shellStatus = "Shell wird initialisiert.";
    private string _interactionStatus = "Interaktivität wurde noch nicht geprüft.";

    protected override async Task OnInitializedAsync()
    {
        // Die Dashboard-Seite zeigt den Current Snapshot; daraus entsteht der
        // initiale Wissenskontext ohne Rolle und ohne ungespeicherte Änderungen.
        PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));

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
