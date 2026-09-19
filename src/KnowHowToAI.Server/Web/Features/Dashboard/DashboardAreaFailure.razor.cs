using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class DashboardAreaFailure
{
    [Parameter, EditorRequired]
    public string Area { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public DashboardAreaError Error { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback Retry { get; set; }
}
