using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Audiences;

/// <summary>
/// Flüchtiger Circuit-State für die Pflichtauswahl der Zielgruppe auf der Wissensseite.
/// </summary>
public sealed class ContextSelectorState
{
    public bool IsOpen { get; private set; }

    public ReadContext? InitialReadContext { get; private set; }

    public string? InitialAudienceId { get; private set; }

    public event Action? Changed;

    public void Open(ReadContext initialReadContext, string? initialAudienceId = null)
    {
        InitialReadContext = initialReadContext ?? throw new ArgumentNullException(nameof(initialReadContext));
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
