using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout.Navigation;

/// <summary>Zeigt den im flüchtigen Arbeitsbereich ausgewählten Entwurf als direkten Link.</summary>
public sealed partial class ActiveDraftLink : ComponentBase, IDisposable
{
    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    protected override void OnInitialized() => WorkspaceState.Changed += HandleWorkspaceChanged;

    public void Dispose() => WorkspaceState.Changed -= HandleWorkspaceChanged;

    private void HandleWorkspaceChanged() => _ = InvokeAsync(StateHasChanged);
}
