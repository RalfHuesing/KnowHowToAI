using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared;

/// <summary>
/// Einzige globale Region für abschlussbestätigende Meldungen; wird vom
/// Layout gehostet und liest den Circuit-Zustand <see cref="ToastState"/>.
/// Neue Meldungen werden über die dauerhaft vorhandene höfliche Live-Region
/// angekündigt, ohne den Fokus zu verschieben; Fehler- und Warninformationen
/// gehören in den Seitenzustand, nie nur in diese Region.
/// </summary>
public sealed partial class ToastRegion : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public ToastState State { get; set; } = default!;

    protected override void OnInitialized() => State.Changed += HandleStateChanged;

    private void HandleStateChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => State.Changed -= HandleStateChanged;
}
