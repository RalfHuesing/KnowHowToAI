namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine paginierte Releaseliste.
/// </summary>
/// <param name="Items">Liste der Release-ViewModels der aktuellen Seite.</param>
/// <param name="NextCursor">Opaker Cursor für die nächste Releaseseite (falls vorhanden).</param>
public sealed record ReleasePageViewModel(
    IReadOnlyList<ReleaseItemViewModel> Items,
    string? NextCursor);
