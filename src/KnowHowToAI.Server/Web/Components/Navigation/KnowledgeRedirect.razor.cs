using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Navigation;

public sealed partial class KnowledgeRedirect : ComponentBase
{
    protected override void OnInitialized() => NavigationManager.NavigateTo("/knowledge", replace: true);
}
