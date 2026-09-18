using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.Components.Layout;

/// <summary>
/// Hauptlayout der Anwendungsshell: Kopf mit Textwortmarke, linke
/// Navigationspalte (<see cref="PrimaryNavigation"/>), Arbeitsfläche mit
/// seiteneingehängten Breadcrumbs und Aktionen über genau einem
/// <c>main</c>-Landmark sowie optionaler Kontextbereich
/// (<see cref="ContextPanel"/>). Ab 1280 CSS-Pixeln (vom zugehörigen Modul
/// gemeldet) stehen die Bereiche nebeneinander; in kompakten Breiten klappen
/// klar beschriftete Kopfbuttons sie ein und aus, Öffnen setzt den Fokus auf
/// die Bereichsüberschrift, Schließen gibt ihn an den Auslöser zurück und
/// Escape schließt nur den zuletzt geöffneten überlagernden Bereich. Eine
/// Fachseite hängt ihre Bereiche über <see cref="PageRegionState"/> ein und
/// kennt kein Seitenraster.
/// </summary>
public sealed partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./Web/Components/Layout/MainLayout.razor.js";

    /// <summary>Einklappbare Seitenbereiche des Layouts in der Reihenfolge ihrer Öffnung.</summary>
    private enum ShellPanel
    {
        Navigation,
        Context
    }

    private readonly List<ShellPanel> _openPanels = [];

    private bool _isCompactMode;
    private bool _isNavigationOpen = true;
    private bool _isContextOpen = true;
    private ShellPanel? _pendingFocusPanel;
    private ShellPanel? _pendingFocusReturnPanel;
    private RenderFragment? _renderedBody;
    private ElementReference _navigationToggleButton;
    private ElementReference _contextToggleButton;
    private PrimaryNavigation? _primaryNavigation;
    private ContextPanel? _contextPanel;
    private DotNetObjectReference<MainLayout>? _selfReference;
    private Task<IJSObjectReference>? _moduleTask;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    protected override void OnInitialized()
    {
        PageRegions.Changed += HandlePageRegionsChanged;
    }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_renderedBody, Body))
        {
            return;
        }

        // Beim Seitenwechsel hängen verbleibende Bereiche der Vorgängerseite;
        // die neue Seite hängt ihre eigenen Bereiche während ihres Renderns ein.
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
        }

        if (_pendingFocusPanel is { } focusPanel)
        {
            _pendingFocusPanel = null;
            await FocusPanelTitleAsync(focusPanel);
        }

        if (_pendingFocusReturnPanel is { } returnPanel)
        {
            _pendingFocusReturnPanel = null;
            await FocusToggleButtonAsync(returnPanel);
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
        _pendingFocusPanel = null;
        _pendingFocusReturnPanel = null;

        if (isCompact)
        {
            // Kompakte Breite: die Bereiche starten geschlossen und werden über
            // die beschrifteten Kopfbuttons ein- und ausgeklappt.
            _isNavigationOpen = false;
            _isContextOpen = false;
            _openPanels.Clear();
        }
        else
        {
            // Desktopbreite: alle Bereiche stehen sichtbar nebeneinander; aus
            // der kompakten Breite übernommene Schließzustände werden aufgehoben.
            _isNavigationOpen = true;
            _isContextOpen = true;
            _openPanels.Clear();
        }

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        PageRegions.Changed -= HandlePageRegionsChanged;
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
            // Der Circuit ist beendet, bevor das Modul freigegeben werden
            // konnte; die Media-Query-Listener sterben mit der Seite.
        }
    }

    private void HandlePageRegionsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private bool IsOpen(ShellPanel panel) =>
        panel == ShellPanel.Navigation ? _isNavigationOpen : _isContextOpen;

    private void SetOpen(ShellPanel panel, bool isOpen)
    {
        if (panel == ShellPanel.Navigation)
        {
            _isNavigationOpen = isOpen;
        }
        else
        {
            _isContextOpen = isOpen;
        }
    }

    private Task TogglePanelAsync(ShellPanel panel) =>
        IsOpen(panel) ? ClosePanelAsync(panel) : OpenPanelAsync(panel);

    private Task OpenPanelAsync(ShellPanel panel)
    {
        SetOpen(panel, isOpen: true);
        _openPanels.Remove(panel);
        _openPanels.Add(panel);
        _pendingFocusPanel = panel;
        _pendingFocusReturnPanel = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task ClosePanelAsync(ShellPanel panel)
    {
        SetOpen(panel, isOpen: false);
        _openPanels.Remove(panel);
        _pendingFocusReturnPanel = panel;
        _pendingFocusPanel = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task EscapeTopPanelAsync()
    {
        if (_openPanels.Count == 0)
        {
            return Task.CompletedTask;
        }

        return ClosePanelAsync(_openPanels[^1]);
    }

    private async Task FocusPanelTitleAsync(ShellPanel panel)
    {
        if (!_isCompactMode)
        {
            return;
        }

        if (panel == ShellPanel.Navigation)
        {
            var navigation = _primaryNavigation;
            if (navigation is not null)
            {
                await navigation.FocusTitleAsync();
            }
        }
        else
        {
            var context = _contextPanel;
            if (context is not null)
            {
                await context.FocusTitleAsync();
            }
        }
    }

    private async Task FocusToggleButtonAsync(ShellPanel panel)
    {
        if (!_isCompactMode)
        {
            return;
        }

        if (panel == ShellPanel.Navigation)
        {
            await _navigationToggleButton.FocusAsync();
        }
        else
        {
            await _contextToggleButton.FocusAsync();
        }
    }

    private Task<IJSObjectReference> EnsureModuleAsync() =>
        _moduleTask ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
