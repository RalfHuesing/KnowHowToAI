using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

public sealed class InMemoryReleaseRepository : IReleaseRepository
{
    private readonly Dictionary<ReleaseId, Release> _releases = new();

    public InMemoryReleaseRepository(IEnumerable<Release>? initialReleases = null)
    {
        if (initialReleases != null)
        {
            foreach (var release in initialReleases)
                _releases[release.ReleaseId] = release;
        }
    }

    public void Add(Release release) => _releases[release.ReleaseId] = release;

    public Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
    {
        _releases.TryGetValue(releaseId, out var release);
        return Task.FromResult(release);
    }
}
