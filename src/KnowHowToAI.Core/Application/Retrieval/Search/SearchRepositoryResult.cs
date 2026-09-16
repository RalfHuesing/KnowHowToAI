using System.Collections;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Ergebnis einer Suchabfrage im Repository: Trefferseite mit Treffern und optionaler
/// gelesener ChangeVersion. Die <see cref="IReadOnlyList{T}"/>-Implementierung dient
/// als komfortabler, indexierbarer Zugriff auf <see cref="Hits"/>.
/// Bei Suche mit <c>RoleId</c> enthalten <c>Roles</c> und <c>Resolutions</c> den Rollen- und
/// Resolution-Order-Stand desselben konsistenten SQL-Lesezeitpunkts; die Search-Validierung
/// braucht sie, um dieselben Fehlercodes wie die Rollenauflösung zu liefern.
/// </summary>
public sealed record SearchRepositoryResult(
    IReadOnlyList<SearchHit> Hits,
    long? ChangeVersion = null,
    IReadOnlyList<Role>? Roles = null,
    IReadOnlyList<RoleResolution>? Resolutions = null) : IReadOnlyList<SearchHit>
{
    public int Count => Hits.Count;

    public SearchHit this[int index] => Hits[index];

    public IEnumerator<SearchHit> GetEnumerator() => Hits.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
