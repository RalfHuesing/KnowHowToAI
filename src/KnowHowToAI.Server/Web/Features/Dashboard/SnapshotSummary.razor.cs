using KnowHowToAI.Server.Web.Features.History;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class SnapshotSummary
{
    [Parameter, EditorRequired]
    public SnapshotViewModel CurrentSnapshot { get; set; } = default!;

    [Parameter]
    public ReleaseItemViewModel? LatestRelease { get; set; }
}
