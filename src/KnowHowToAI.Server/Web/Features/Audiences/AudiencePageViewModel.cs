namespace KnowHowToAI.Server.Web.Features.Audiences;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine paginierte Zielgruppenliste.
/// </summary>
/// <param name="Items">Liste der Zielgruppen-ViewModels der aktuellen Seite.</param>
/// <param name="NextCursor">Opaker Cursor für die nächste Seite (falls vorhanden).</param>
public sealed record AudiencePageViewModel(
    IReadOnlyList<AudienceItemViewModel> Items,
    string? NextCursor,
    long? ChangeVersion = null);
