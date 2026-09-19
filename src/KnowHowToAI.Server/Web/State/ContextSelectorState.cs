using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Betriebsmodus des globalen Selektors.
/// </summary>
public enum ContextSelectorMode
{
    /// <summary>
    /// Vollständige Auswahl von Lesekontext (Current, Snapshot, Release, Working Transaction) und Rolle.
    /// Kann vom Benutzer abgebrochen werden.
    /// </summary>
    Full,

    /// <summary>
    /// Modaler Pflichtauswahl-Selektor gemäß O-008: Es fehlt eine gültige Rolle,
    /// daher muss eine Rolle für den aktuellen Kontext gewählt werden.
    /// Kann nicht ohne Auswahl abgebrochen werden.
    /// </summary>
    MandatoryRole
}

/// <summary>
/// Flüchtiger Circuit-State für den globalen Rollen- und Lesekontext-Selektor.
/// Koordiniert das Öffnen und Schließen des modalen Auswahldialogs über Komponentengrenzen hinweg.
/// </summary>
public sealed class ContextSelectorState
{
    public bool IsOpen { get; private set; }

    public ContextSelectorMode Mode { get; private set; } = ContextSelectorMode.Full;

    public ReadContext? InitialReadContext { get; private set; }

    public string? InitialRoleId { get; private set; }

    public event Action? Changed;

    public void Open(
        ContextSelectorMode mode,
        ReadContext? initialReadContext = null,
        string? initialRoleId = null)
    {
        Mode = mode;
        InitialReadContext = initialReadContext;
        InitialRoleId = initialRoleId;
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
