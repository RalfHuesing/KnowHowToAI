using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class RecentChanges
{
    [Parameter, EditorRequired]
    public IReadOnlyList<RecentNodeChangeViewModel> Changes { get; set; } = [];

    [Parameter]
    public bool HasPredecessor { get; set; }
}
