using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout;

/// <summary>
/// Nimmt die Seitenbereiche der gerade angezeigten Fachseite auf: Breadcrumbs,
/// Seitenaktionen und optionalen Kontext. Fachseiten hängen ihre Inhalte hier
/// ein und kennen ausschließlich diese Registrierung, nicht das CSS-Seitenraster
/// des Hauptlayouts. Der Zustand ist flüchtiger Circuit-State ohne fachliche
/// Wahrheit; das Hauptlayout setzt ihn beim Seitenwechsel zurück.
/// </summary>
public sealed class PageRegionState
{
    private RenderFragment? _breadcrumbs;
    private RenderFragment? _actions;
    private RenderFragment? _context;

    /// <summary>Aktuell eingehängter Breadcrumb-Bereich; ohne Inhalt <see langword="null"/>.</summary>
    public RenderFragment? Breadcrumbs => _breadcrumbs;

    /// <summary>Aktuell eingehängter Aktionsbereich; ohne Inhalt <see langword="null"/>.</summary>
    public RenderFragment? Actions => _actions;

    /// <summary>Aktuell eingehängter Kontextbereich; ohne Inhalt <see langword="null"/>.</summary>
    public RenderFragment? Context => _context;

    /// <summary>
    /// Wird ausgelöst, wenn ein Bereich ein- oder ausgehängt wurde und das
    /// Hauptlayout seine Slots neu zeichnen muss.
    /// </summary>
    public event Action? Changed;

    /// <summary>Hängt den Breadcrumb-Bereich der Fachseite ein; <see langword="null"/> entfernt ihn.</summary>
    public void SetBreadcrumbs(RenderFragment? content) => Set(ref _breadcrumbs, content);

    /// <summary>Hängt den Aktionsbereich der Fachseite ein; <see langword="null"/> entfernt ihn.</summary>
    public void SetActions(RenderFragment? content) => Set(ref _actions, content);

    /// <summary>Hängt den Kontextbereich der Fachseite ein; <see langword="null"/> entfernt ihn.</summary>
    public void SetContext(RenderFragment? content) => Set(ref _context, content);

    /// <summary>Entfernt alle eingehängten Bereiche; das Hauptlayout ruft dies beim Seitenwechsel auf.</summary>
    public void Clear()
    {
        var hasContent = _breadcrumbs is not null || _actions is not null || _context is not null;
        _breadcrumbs = null;
        _actions = null;
        _context = null;

        if (hasContent)
        {
            Changed?.Invoke();
        }
    }

    private void Set(ref RenderFragment? field, RenderFragment? content)
    {
        if (ReferenceEquals(field, content))
        {
            return;
        }

        field = content;
        Changed?.Invoke();
    }
}
