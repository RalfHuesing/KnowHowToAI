using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für explizite Rollen-Contents eines vollständigen Snapshots.</summary>
public interface IContentRepository
{
    Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default);
}
