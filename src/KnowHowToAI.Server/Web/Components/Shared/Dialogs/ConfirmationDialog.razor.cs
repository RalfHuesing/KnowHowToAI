using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.Dialogs;

/// <summary>
/// Bestätigungsdialog auf Basis des nativen Dialogwrappers
/// <see cref="AppDialog"/>: zeigt Titel, kurze Auswirkung, die primäre Aktion
/// und „Abbrechen“. Beim Öffnen steht der Fokus auf der sicheren Aktion
/// „Abbrechen“ (erstes Element), Fokusfalle und Fokusrückgabe übernimmt die
/// Dialogisolation von <see cref="AppDialog"/>; Escape entspricht
/// „Abbrechen“. Eine destruktive Aktion ist optisch und inhaltlich
/// eindeutig; eine Texteingabe zur Bestätigung gibt es bewusst nicht.
/// Während eines Requests sind beide Aktionen gegen Doppelaufruf geschützt:
/// Der Bestätigungs-Callback läuft höchstens einmal gleichzeitig, beide
/// Aktionen sind deaktiviert, und der Abschluss schließt über
/// <see cref="CloseAsync"/>, ohne einen Abbruch auszulösen.
/// </summary>
public sealed partial class ConfirmationDialog : ComponentBase
{
    private static int _instanceCounter;

    private readonly string _messageId = $"confirmation-dialog-message-{Interlocked.Increment(ref _instanceCounter)}";
    private AppDialog? _dialog;
    private bool _isBusy;
    private bool _isCloseWithoutCancel;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public string ConfirmText { get; set; } = "Bestätigen";

    [Parameter]
    public bool IsDestructive { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnConfirm { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnCancel { get; set; }

    public async Task OpenAsync()
    {
        _isBusy = false;
        _isCloseWithoutCancel = false;

        if (_dialog is not null)
        {
            await _dialog.OpenAsync();
        }
    }

    /// <summary>
    /// Schließt programmatisch, beispielsweise nach bestätigter Aktion, ohne
    /// <see cref="OnCancel"/> auszulösen.
    /// </summary>
    public async Task CloseAsync()
    {
        _isCloseWithoutCancel = true;

        if (_dialog is not null)
        {
            await _dialog.CloseAsync();
        }
    }

    private async Task HandleDialogClosedAsync()
    {
        if (_isCloseWithoutCancel)
        {
            _isCloseWithoutCancel = false;
            return;
        }

        // Escape über das native Close-Ereignis entspricht „Abbrechen“.
        await OnCancel.InvokeAsync();
    }

    private async Task ConfirmAsync()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        StateHasChanged();
        try
        {
            await OnConfirm.InvokeAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task CancelAsync()
    {
        if (_isBusy || _isCloseWithoutCancel)
        {
            return;
        }

        // Der Abbruch läuft über die Dialogisolation; das native
        // Close-Ereignis löst deshalb keinen zweiten Abbruch aus.
        _isCloseWithoutCancel = true;
        await OnCancel.InvokeAsync();
        await CloseAsync();
    }
}
