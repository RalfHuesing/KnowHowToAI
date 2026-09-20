namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Abfrage-Parameter für <see cref="NavigationService.ListAudiencesAsync"/>.
/// Kapselt Read-Kontext und optionale Paging-Parameter.
/// </summary>
public sealed record ListAudiencesQuery(
    ReadContext Context,
    int? Limit = null,
    string? Cursor = null)
{
    public ListAudiencesQuery() : this(new ReadContext()) { }
}
