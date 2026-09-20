using System.Collections;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Ergebnis einer Suchabfrage im Repository: Trefferseite mit Treffern und optionaler
/// gelesener ChangeVersion. Die <see cref="IReadOnlyList{T}"/>-Implementierung dient
/// als komfortabler, indexierbarer Zugriff auf <see cref="Hits"/>.
/// Bei Suche mit <c>AudienceId</c> enthalten <c>Audiences</c> und <c>Resolutions</c> den Zielgruppen- und
/// Resolution-Order-Stand desselben konsistenten SQL-Lesezeitpunkts; die Search-Validierung
/// braucht sie, um dieselben Fehlercodes wie die Rollenauflösung zu liefern.
/// </summary>
public sealed record SearchRepositoryResult(
    IReadOnlyList<SearchHit> Hits,
    long? ChangeVersion = null,
    IReadOnlyList<Audience>? Audiences = null,
    IReadOnlyList<AudienceResolution>? Resolutions = null) : IReadOnlyList<SearchHit>
{
    public int Count => Hits.Count;

    public SearchHit this[int index] => Hits[index];

    public IEnumerator<SearchHit> GetEnumerator() => Hits.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
