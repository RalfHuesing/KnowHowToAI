using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Konfigurierbarer In-Memory-<see cref="IRetrievalRepository"/>-Stub für
/// Search-Szenarien: liefert die konfigurierten Treffer (<see cref="ResultsToReturn"/>)
/// samt Zielgruppen-Auflösungsdaten zurück und merkt sich Aufrufanzahl
/// (<see cref="CallCount"/>) und letzte Anfrage (<see cref="LastRequest"/>) für
/// Assertionen.
/// </summary>
public sealed class InMemoryRetrievalRepository(SnapshotId snapshotId) : IRetrievalRepository
{
    /// <summary>Anzahl der SearchAsync-Aufrufe seit Erzeugung des Stubs.</summary>
    public int CallCount { get; private set; }

    /// <summary>Letzte an SearchAsync übergebene Anfrage.</summary>
    public SearchRequest? LastRequest { get; private set; }

    /// <summary>Treffer, die der Stub als Suchergebnis zurückliefert.</summary>
    public List<SearchHit> ResultsToReturn { get; set; } = [];

    /// <summary>Optionale ChangeVersion im Suchergebnis.</summary>
    public long? ChangeVersionToReturn { get; set; }

    /// <summary>Zielgruppen, die der Stub als aktive Zielgruppen zurückliefert.</summary>
    public List<Audience> Audiences { get; } = [];

    /// <summary>Auflösungsreihenfolgen, die der Stub zurückliefert.</summary>
    public List<AudienceResolution> Resolutions { get; } = [];

    /// <summary>Konfiguriert eine aktive, nicht gelöschte Zielgruppe mit Selbst-Auflösung.</summary>
    public void ConfigureActiveAudience(AudienceId audienceId, bool isDeleted = false)
    {
        Audiences.Add(new Audience(snapshotId, audienceId, audienceId.Value, null, isDeleted));
        Resolutions.Add(new AudienceResolution(snapshotId, audienceId, audienceId, 1));
    }

    public Task<Result<SearchRepositoryResult>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastRequest = request;
        var cursor = SearchCursor.TryDecode(request.Cursor);
        var hits = ResultsToReturn
            .Where(hit => request.Filter is null || request.Filter.IsEmpty || request.Filter.Matches(hit))
            .Where(hit => cursor is null
                || hit.SortOrder > cursor.LastSortOrder
                || (hit.SortOrder == cursor.LastSortOrder
                    && hit.NodeId.Value.CompareTo(cursor.LastNodeId.Value) > 0))
            .Take(request.Limit)
            .ToArray();
        return Task.FromResult(Result<SearchRepositoryResult>.Success(new SearchRepositoryResult(
            hits, ChangeVersionToReturn, Audiences, Resolutions)));
    }
}
