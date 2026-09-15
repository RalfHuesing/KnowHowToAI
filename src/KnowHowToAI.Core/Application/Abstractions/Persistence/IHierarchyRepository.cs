using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für die globale Hierarchie eines vollständigen Snapshots.</summary>
public interface IHierarchyRepository
{
    Task<IReadOnlyList<Node>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default);
}
