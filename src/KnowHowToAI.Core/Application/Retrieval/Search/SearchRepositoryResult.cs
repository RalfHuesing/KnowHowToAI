using System.Collections;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Ergebnis einer Suchabfrage im Repository mit Treffern und optionaler gelesener ChangeVersion.
/// Implementiert IReadOnlyList&lt;SearchHit&gt; für nahtlose Abwärtskompatibilität.
/// </summary>
public sealed record SearchRepositoryResult(
    IReadOnlyList<SearchHit> Hits,
    long? ChangeVersion = null) : IReadOnlyList<SearchHit>
{
    public int Count => Hits.Count;

    public SearchHit this[int index] => Hits[index];

    public IEnumerator<SearchHit> GetEnumerator() => Hits.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
