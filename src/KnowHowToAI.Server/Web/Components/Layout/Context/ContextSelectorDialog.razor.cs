using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Hostet ausschließlich den nativen Dialog-Lifecycle des globalen
/// Kontextselektors. Auswahl, Validierung und Navigation liegen im Formular.
/// </summary>
public sealed partial class ContextSelectorDialog : ComponentBase, IAsyncDisposable
{
    private AppDialog? _dialog;
    private bool _isOpen;

    [Inject]
    private ContextSelectorState State { get; set; } = default!;

    internal string DialogTitle => State.Mode == ContextSelectorMode.MandatoryRole
        ? "Rolle auswählen"
        : "Wissenskontext und Rolle anpassen";

    protected override void OnInitialized()
    {
        State.Changed += HandleStateChanged;
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!State.IsOpen && _isOpen)
        {
            _isOpen = false;
        }

        return Task.CompletedTask;
    }

    private void HandleStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task OpenDialogAsync()
    {
        if (!State.IsOpen || _isOpen || _dialog is null)
            return;

        _isOpen = true;
        await _dialog.OpenAsync();
    }

    private async Task HandleDialogClosedAsync()
    {
        if (State.Mode == ContextSelectorMode.MandatoryRole && State.IsOpen)
        {
            if (_dialog is not null)
            {
                await _dialog.OpenAsync();
            }

            return;
        }

        _isOpen = false;
        State.Close();
    }

    public ValueTask DisposeAsync()
    {
        State.Changed -= HandleStateChanged;
        return ValueTask.CompletedTask;
    }
}
