namespace KnowHowToAI.Server.Web.State;

/// <summary>Einzelner Eintrag der globalen Toastregion.</summary>
public sealed record ToastEntry(Guid Id, string Message);

/// <summary>
/// Flüchtiger Circuit-Zustand der einzigen globalen Toastregion: enthält
/// ausschließlich Bestätigungen nichtkritischer, abgeschlossener Aktionen.
/// Kritische Informationen gehören in den Seitenzustand (Inline-Alert,
/// Statusbanner) und nie nur in einen Toast. Meldungen laufen nicht
/// zeitgesteuert ab; sie bleiben bis zum Schließen durch den Benutzer
/// bestehen und werden mit dem Ende des Circuits verworfen.
/// </summary>
public sealed class ToastState
{
    private readonly List<ToastEntry> _entries = [];

    public event Action? Changed;

    public IReadOnlyList<ToastEntry> Entries => _entries;

    public void Show(string message)
    {
        _entries.Add(new ToastEntry(Guid.NewGuid(), message));
        Changed?.Invoke();
    }

    public void Dismiss(Guid id)
    {
        if (_entries.RemoveAll(entry => entry.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }
}
