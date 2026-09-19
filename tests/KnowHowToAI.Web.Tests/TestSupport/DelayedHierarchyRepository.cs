using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Web.Tests.TestSupport;

internal sealed class DelayedHierarchyRepository : IHierarchyRepository
{
    private readonly IHierarchyRepository _inner;
    private readonly Func<SnapshotId, CancellationToken, Task<IReadOnlyList<Node>>> _handler;

    public DelayedHierarchyRepository(
        IHierarchyRepository inner,
        Func<SnapshotId, CancellationToken, Task<IReadOnlyList<Node>>> handler)
    {
        _inner = inner;
        _handler = handler;
    }

    public Task<IReadOnlyList<Node>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        _handler(snapshotId, cancellationToken);
}
