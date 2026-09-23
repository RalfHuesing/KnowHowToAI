using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Shared.Feedback;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.Components.Layout.Shell;

/// <summary>Gemeinsame Anwendungsshell mit globaler Navigation, Hauptinhalt und Feedback.</summary>
public sealed partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./Web/Components/Layout/Shell/MainLayout.razor.js";

    private bool _isCompactMode;
    private bool _isInteractive;
    private bool _isNavigationOpen = true;
    private RenderFragment? _renderedBody;
    private DotNetObjectReference<MainLayout>? _selfReference;
    private Task<IJSObjectReference>? _moduleTask;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private ToastState ToastState { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    protected override void OnInitialized()
    {
        PageRegions.Changed += HandlePageRegionsChanged;
        WorkspaceState.Changed += HandlePageRegionsChanged;
        WorkspaceEditState.Changed += HandlePageRegionsChanged;
    }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_renderedBody, Body))
        {
            return;
        }

        _renderedBody = Body;
        PageRegions.Clear();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfReference = DotNetObjectReference.Create(this);
            var module = await EnsureModuleAsync();
            await module.InvokeVoidAsync("observeBreakpoint", _selfReference);
            _isInteractive = true;
            await InvokeAsync(StateHasChanged);
        }

    }

    [JSInvokable]
    public async Task NotifyCompactModeChangedAsync(bool isCompact)
    {
        if (_isCompactMode == isCompact)
        {
            return;
        }

        _isCompactMode = isCompact;
        _isNavigationOpen = !isCompact;
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        PageRegions.Changed -= HandlePageRegionsChanged;
        WorkspaceState.Changed -= HandlePageRegionsChanged;
        WorkspaceEditState.Changed -= HandlePageRegionsChanged;
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
            // Der Circuit ist beendet, bevor das Modul freigegeben werden konnte.
        }
    }

    private void HandlePageRegionsChanged() => _ = InvokeAsync(StateHasChanged);

    private bool IsDirty => WorkspaceEditState.IsDirty;

    private Task ToggleNavigationAsync() => _isNavigationOpen
        ? CloseNavigationAsync()
        : OpenNavigationAsync();

    private Task OpenNavigationAsync()
    {
        _isNavigationOpen = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task CloseNavigationAsync()
    {
        _isNavigationOpen = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task<IJSObjectReference> EnsureModuleAsync() =>
        _moduleTask ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
