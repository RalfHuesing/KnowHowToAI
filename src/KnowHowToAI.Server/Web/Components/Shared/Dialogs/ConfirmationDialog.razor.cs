using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Shared.Dialogs;

/// <summary>
/// Bestätigungsdialog auf Basis des nativen Dialogwrappers
/// <see cref="AppDialog"/>: zeigt Titel, kurze Auswirkung, die primäre Aktion
/// und „Abbrechen“. Beim Öffnen steht der Fokus auf der sicheren Aktion
/// „Abbrechen“ (erstes Element), Fokusfalle und Fokusrückgabe übernimmt die
/// Dialogisolation von <see cref="AppDialog"/>; Escape entspricht
/// „Abbrechen“. Eine destruktive Aktion ist optisch und inhaltlich
/// eindeutig; optionale Eingaben erläutern die bestätigte Aktion, ersetzen aber
/// niemals deren expliziten Button.
/// Während eines Requests sind beide Aktionen gegen Doppelaufruf geschützt:
/// Der Bestätigungs-Callback läuft höchstens einmal gleichzeitig, beide
/// Aktionen sind deaktiviert, und der Abschluss schließt über
/// <see cref="CloseAsync"/>, ohne einen Abbruch auszulösen.
/// </summary>
public sealed partial class ConfirmationDialog : ComponentBase
{
    private static int _instanceCounter;

    private readonly string _messageId = $"confirmation-dialog-message-{Interlocked.Increment(ref _instanceCounter)}";
    private readonly string _inputId = $"confirmation-dialog-input-{Interlocked.Increment(ref _instanceCounter)}";
    private AppDialog? _dialog;
    private bool _isBusy;
    private bool _isCloseWithoutCancel;
    private bool _isOpen;
    private string? _inputValue;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public string ConfirmText { get; set; } = "Bestätigen";

    [Parameter]
    public bool IsDestructive { get; set; }

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public EventCallback OnConfirm { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Optionale Beschriftung eines Eingabefelds für die bestätigte Aktion.</summary>
    [Parameter]
    public string? InputLabel { get; set; }

    /// <summary>Startwert des optionalen Eingabefelds beim Öffnen.</summary>
    [Parameter]
    public string? InputValue { get; set; }

    /// <summary>Optionaler Bestätigungs-Callback, der den eingegebenen Wert erhält.</summary>
    [Parameter]
    public EventCallback<string?> OnConfirmWithInput { get; set; }

    private bool HasInput => !string.IsNullOrWhiteSpace(InputLabel);

    protected override Task OnParametersSetAsync() =>
        IsOpen == _isOpen ? Task.CompletedTask : IsOpen ? OpenAsync() : CloseAsync();

    public async Task OpenAsync()
    {
        _isOpen = true;
        _isBusy = false;
        _isCloseWithoutCancel = false;
        _inputValue = InputValue;

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
        _isOpen = false;
        _isCloseWithoutCancel = true;

        if (_dialog is not null)
            await _dialog.CloseAsync();
    }

    private async Task HandleDialogClosedAsync()
    {
        _isOpen = false;
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
            if (OnConfirmWithInput.HasDelegate)
            {
                await OnConfirmWithInput.InvokeAsync(_inputValue);
            }
            else
            {
                await OnConfirm.InvokeAsync();
            }
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
