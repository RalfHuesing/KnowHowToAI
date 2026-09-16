namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Abfrage-Parameter für <see cref="NavigationService.ListRolesAsync"/>.
/// Kapselt Read-Kontext und optionale Paging-Parameter.
/// </summary>
public sealed record ListRolesQuery(
    ReadContext Context,
    int? Limit = null,
    string? Cursor = null)
{
    public ListRolesQuery() : this(new ReadContext()) { }
}
