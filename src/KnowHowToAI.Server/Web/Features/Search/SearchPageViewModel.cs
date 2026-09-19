namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>
/// Unveränderliches UI-Datenmodell für eine paginierte Suchergebnisseite.
/// </summary>
/// <param name="Query">Suchtext der Anfrage.</param>
/// <param name="Items">Liste der Suchtreffer-ViewModels der aktuellen Seite.</param>
/// <param name="NextCursor">Opaker Cursor für die nächste Trefferseite (falls vorhanden).</param>
public sealed record SearchPageViewModel(
    string Query,
    IReadOnlyList<SearchHitViewModel> Items,
    string? NextCursor);
