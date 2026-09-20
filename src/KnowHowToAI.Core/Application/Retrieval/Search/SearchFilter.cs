using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Optionale Suchfacetten. Werte einer Facette werden als Oder, Facetten
/// untereinander als Und ausgewertet.
/// </summary>
public sealed record SearchFilter(
    IReadOnlyList<AudienceId>? ResolvedAudienceIds = null,
    IReadOnlyList<Availability>? Availabilities = null,
    IReadOnlyList<Freshness>? Freshnesses = null,
    IReadOnlyList<string>? FindingCodes = null)
{
    public bool IsEmpty =>
        (ResolvedAudienceIds?.Count ?? 0) == 0
        && (Availabilities?.Count ?? 0) == 0
        && (Freshnesses?.Count ?? 0) == 0
        && (FindingCodes?.Count ?? 0) == 0;

    public string Fingerprint => string.Join(
        "|",
        $"audience:{Join(ResolvedAudienceIds?.Select(audience => audience.Value))}",
        $"availability:{Join(Availabilities?.Select(value => value.ToString()))}",
        $"freshness:{Join(Freshnesses?.Select(value => value.ToString()))}",
        $"finding:{Join(FindingCodes)}");

    public bool Matches(SearchHit hit)
    {
        ArgumentNullException.ThrowIfNull(hit);

        return (ResolvedAudienceIds is not { Count: > 0 }
                || (hit.ResolvedAudienceId is { } audience && ResolvedAudienceIds.Contains(audience)))
            && (Availabilities is not { Count: > 0 } || Availabilities.Contains(hit.Availability))
            && (Freshnesses is not { Count: > 0 } || Freshnesses.Contains(hit.Freshness))
            && (FindingCodes is not { Count: > 0 } || (hit.Findings ?? []).Any(FindingCodes.Contains));
    }

    private static string Join(IEnumerable<string>? values) => values is null
        ? string.Empty
        : string.Join(",", values.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal));
}
