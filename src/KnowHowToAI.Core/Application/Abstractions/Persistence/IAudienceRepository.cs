using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für Zielgruppen und deren explizite Auflösungsreihenfolgen.</summary>
public interface IAudienceRepository
{
    Task<IReadOnlyList<Audience>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AudienceResolution>> ListResolutionsBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default);
}
