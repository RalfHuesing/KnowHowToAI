using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.Tabs;

public sealed partial class TabPanelLayout : ComponentBase
{
    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public RenderFragment? PrimaryContent { get; set; }

    [Parameter]
    public RenderFragment? AdditionalContent { get; set; }

    [Parameter]
    public RenderFragment? Status { get; set; }

    [Parameter]
    public RenderFragment? Actions { get; set; }
}
