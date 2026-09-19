using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.Components.Shared.Dialogs;

/// <summary>
/// Dünner Wrapper um den nativen HTML-Dialog. <c>showModal</c>, Fokusfalle,
/// Escape über das native Cancel/Close-Ereignis und die Fokusrückgabe liegen in
/// der isolierten Moduldatei <c>AppDialog.razor.js</c>; diese Komponente hält
/// dafür keine eigene Zustandsmaschine. Der verwendende Component steuert das
/// Öffnen über <see cref="OpenAsync"/> und erhält das Schließen über
/// <see cref="OnClosed"/>.
/// </summary>
public sealed partial class AppDialog : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./Web/Components/Shared/Dialogs/AppDialog.razor.js";

    private static int _instanceCounter;

    private readonly string _titleElementId = $"app-dialog-title-{Interlocked.Increment(ref _instanceCounter)}";
    private ElementReference _dialogElement;
    private Task<IJSObjectReference>? _moduleTask;
    private DotNetObjectReference<AppDialog>? _selfReference;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback OnClosed { get; set; }

    public async Task OpenAsync()
    {
        var module = await EnsureModuleAsync();

        await module.InvokeVoidAsync("show", _dialogElement);
    }

    public async Task CloseAsync()
    {
        var module = await EnsureModuleAsync();

        await module.InvokeVoidAsync("close", _dialogElement);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _selfReference = DotNetObjectReference.Create(this);
        var module = await EnsureModuleAsync();

        await module.InvokeVoidAsync("initialize", _dialogElement, _selfReference);
    }

    [JSInvokable]
    public async Task NotifyDialogClosedAsync()
    {
        await OnClosed.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _selfReference?.Dispose();

        if (_moduleTask is null)
        {
            return;
        }

        try
        {
            var module = await _moduleTask;

            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Der Circuit ist beendet, bevor das Modul freigegeben werden konnte;
            // im Browserprozess werden die Listener mit dem Dialog abgebaut.
        }
    }

    private Task<IJSObjectReference> EnsureModuleAsync() =>
        _moduleTask ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
