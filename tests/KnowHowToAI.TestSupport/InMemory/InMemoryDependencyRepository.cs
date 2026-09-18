using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IDependencyRepository"/>: filtert die Content-Provenienz des
/// gemeinsamen <see cref="InMemoryKnowledgeStore"/> nach SnapshotId.
/// </summary>
public sealed class InMemoryDependencyRepository(InMemoryKnowledgeStore store) : IDependencyRepository
{
    public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ContentDependency>>(
            store.Dependencies.Where(dependency => dependency.SnapshotId == snapshotId).ToArray());
}
