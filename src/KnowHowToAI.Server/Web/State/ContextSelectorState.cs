using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Betriebsmodus des globalen Selektors.
/// </summary>
public enum ContextSelectorMode
{
    /// <summary>
    /// Vollständige Auswahl von Lesekontext (Current, Snapshot, Release, Working Transaction) und Zielgruppe.
    /// Kann vom Benutzer abgebrochen werden.
    /// </summary>
    Full,

    /// <summary>
    /// Modaler Pflichtauswahl-Selektor gemäß O-008: Es fehlt eine gültige Zielgruppe,
    /// daher muss eine Zielgruppe für den aktuellen Kontext gewählt werden.
    /// Kann nicht ohne Auswahl abgebrochen werden.
    /// </summary>
    MandatoryAudience
}

/// <summary>
/// Flüchtiger Circuit-State für den globalen Zielgruppen- und Lesekontext-Selektor.
/// Koordiniert das Öffnen und Schließen des modalen Auswahldialogs über Komponentengrenzen hinweg.
/// </summary>
public sealed class ContextSelectorState
{
    public bool IsOpen { get; private set; }

    public ContextSelectorMode Mode { get; private set; } = ContextSelectorMode.Full;

    public ReadContext? InitialReadContext { get; private set; }

    public string? InitialAudienceId { get; private set; }

    public event Action? Changed;

    public void Open(
        ContextSelectorMode mode,
        ReadContext? initialReadContext = null,
        string? initialAudienceId = null)
    {
        Mode = mode;
        InitialReadContext = initialReadContext;
        InitialAudienceId = initialAudienceId;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        Changed?.Invoke();
    }
}
