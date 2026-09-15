using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für Content-Mutationen auf einem Working Snapshot.
/// Speichert Contents und die dazugehörigen Dependencies atomar.
/// </summary>
public interface IContentMutationRepository
{
    /// <summary>
    /// Ersetzt den Content-Zustand eines Working Snapshots vollständig.
    /// Contents und Dependencies werden in einer einzigen atomaren Operation geschrieben.
    /// </summary>
    Task SaveAsync(
        SnapshotId snapshotId,
        IReadOnlyList<NodeContent> contents,
        IReadOnlyList<ContentDependency> dependencies,
        CancellationToken cancellationToken = default);
}
