using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class QualitySummary
{
    [Parameter, EditorRequired]
    public DashboardQualityViewModel Quality { get; set; } = default!;
}
