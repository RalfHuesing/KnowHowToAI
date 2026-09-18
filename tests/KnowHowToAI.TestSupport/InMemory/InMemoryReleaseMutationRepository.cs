using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IReleaseMutationRepository"/>: verwaltet Releases in einer
/// Liste, vergibt Release-IDs fortlaufend und listet deterministisch nach Release-ID
/// auf. Über <see cref="CreateError"/> lässt sich eine Rejection (z. B. Namenskonflikt)
/// einschleusen; <see cref="LastRequest"/> merkt sich die letzte Create-Anfrage für
/// Assertionen.
/// </summary>
public sealed class InMemoryReleaseMutationRepository : IReleaseMutationRepository
{
    /// <summary>Bisher registrierte Releases; Tests können direkt seeden.</summary>
    public List<Release> ExistingReleases { get; } = [];

    /// <summary>Letzte an <c>CreateAsync</c> übergebene Anfrage.</summary>
    public CreateReleaseRecord? LastRequest { get; private set; }

    /// <summary>Optionale Rejection-Injection: Fehler, den CreateAsync vorab zurückliefert.</summary>
    public DomainError? CreateError { get; set; }

    public Task<Result<Release>> CreateAsync(
        CreateReleaseRecord request,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        if (CreateError is not null)
            return Task.FromResult(Result<Release>.Failure(CreateError));

        var release = new Release(
            new ReleaseId(ExistingReleases.Count + 1),
            request.SnapshotId,
            request.Name,
            request.Description,
            request.CreatedAtUtc);

        ExistingReleases.Add(release);
        return Task.FromResult(Result<Release>.Success(release));
    }

    public Task<IReadOnlyList<Release>> ListAsync(
        int limit,
        long? afterReleaseId,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Release> query = ExistingReleases.OrderBy(release => release.ReleaseId.Value);
        if (afterReleaseId.HasValue)
            query = query.Where(release => release.ReleaseId.Value > afterReleaseId.Value);

        return Task.FromResult<IReadOnlyList<Release>>(query.Take(limit).ToList());
    }
}
