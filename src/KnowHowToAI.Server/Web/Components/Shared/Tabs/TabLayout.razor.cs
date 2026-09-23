using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.Tabs;

public sealed partial class TabLayout : ComponentBase
{
    [Parameter, EditorRequired]
    public IReadOnlyList<TabDefinition> Tabs { get; set; } = Array.Empty<TabDefinition>();

    [Parameter, EditorRequired]
    public string ActiveKey { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public EventCallback<string> OnTabSelected { get; set; }

    [Parameter]
    public RenderFragment? Context { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? Panels { get; set; }

    private Task SelectTabAsync(string key) => OnTabSelected.InvokeAsync(key);
}
