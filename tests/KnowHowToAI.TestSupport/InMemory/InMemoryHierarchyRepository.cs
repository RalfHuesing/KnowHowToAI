using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IHierarchyRepository"/>: filtert die Nodes des gemeinsamen
/// <see cref="InMemoryKnowledgeStore"/> nach SnapshotId.
/// </summary>
public sealed class InMemoryHierarchyRepository(InMemoryKnowledgeStore store) : IHierarchyRepository
{
    public Task<IReadOnlyList<Node>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Node>>(
            store.Nodes.Where(node => node.SnapshotId == snapshotId).ToArray());
}
