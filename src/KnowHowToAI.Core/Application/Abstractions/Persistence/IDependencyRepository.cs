using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für die gespeicherte Content-Provenienz eines Snapshots.</summary>
public interface IDependencyRepository
{
    Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default);
}
