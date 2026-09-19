using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Dashboard;
using KnowHowToAI.Server.Web.Components.Layout;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class DashboardPage
{
    [Inject]
    private DashboardService DashboardService { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    private bool _isLoading = true;
    private string? _errorMessage;
    private DashboardViewModel? _model;

    protected override async Task OnInitializedAsync()
    {
        PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var result = await DashboardService.GetDashboardAsync(new DashboardQuery(), CancellationToken.None);
            if (result.IsSuccess && result.Value is not null)
            {
                _model = DashboardMapper.ToDashboardViewModel(result.Value, Clock.UtcNow);
            }
            else
            {
                _errorMessage = result.Error?.Message ?? "Das Wissensdashboard konnte nicht geladen werden.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = "Fehler beim Laden des Wissensdashboards: " + ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string _interactionStatus = "Interaktivität wurde noch nicht geprüft.";

    private void CheckInteractivity() =>
        _interactionStatus = "Interaktivität ist verfügbar.";
}
