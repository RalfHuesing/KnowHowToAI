using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.Components.Shared.Dialogs;

/// <summary>
/// Dünner Wrapper um den nativen HTML-Dialog. Die isolierte Moduldatei öffnet
/// und schließt das Dialogelement und leitet dessen natives Close-Ereignis an
/// <see cref="OnClosed"/> weiter.
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
    public string? CssClass { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback OnClosed { get; set; }

    [Parameter]
    public EventCallback OnReady { get; set; }

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
        await OnReady.InvokeAsync();
    }

    [JSInvokable]
    public async Task NotifyDialogClosedAsync()
    {
        await OnClosed.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is not null)
        {
            try
            {
                var module = await _moduleTask;
                await module.InvokeVoidAsync("dispose", _dialogElement);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Der Circuit ist beendet, bevor das Modul freigegeben werden konnte.
            }
        }

        _selfReference?.Dispose();
    }

    private Task<IJSObjectReference> EnsureModuleAsync() =>
        _moduleTask ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
