using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IAudienceRepository"/>: filtert Zielgruppen und Auflösungsreihenfolgen
/// des gemeinsamen <see cref="InMemoryKnowledgeStore"/> nach SnapshotId.
/// </summary>
public sealed class InMemoryAudienceRepository(InMemoryKnowledgeStore store) : IAudienceRepository
{
    public Task<IReadOnlyList<Audience>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Audience>>(
            store.Audiences.Where(audience => audience.SnapshotId == snapshotId).ToArray());

    public Task<IReadOnlyList<AudienceResolution>> ListResolutionsBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AudienceResolution>>(
            store.Resolutions.Where(resolution => resolution.SnapshotId == snapshotId).ToArray());
}
