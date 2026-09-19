using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für aktuelle und historische Snapshot-Metadaten.</summary>
public interface ISnapshotRepository
{
    Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default);

    Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Snapshot>> ListCommittedAsync(
        int limit,
        SnapshotId? beforeSnapshotId,
        CancellationToken cancellationToken = default);
}
