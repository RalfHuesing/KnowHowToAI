using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für Node-Mutationen auf einem Working Snapshot.
/// Speichert die vollständige normalisierte Hierarchie nach einer Mutation atomar.
/// </summary>
public interface INodeMutationRepository
{
    /// <summary>Ersetzt die Hierarchie eines Working Snapshots vollständig durch die gegebene Liste.</summary>
    Task SaveAsync(
        SnapshotId snapshotId,
        IReadOnlyList<Node> nodes,
        CancellationToken cancellationToken = default);
}
