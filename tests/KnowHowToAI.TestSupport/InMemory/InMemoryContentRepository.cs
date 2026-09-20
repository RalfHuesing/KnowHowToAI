using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IContentRepository"/>: filtert die Zielgruppen-Contents des
/// gemeinsamen <see cref="InMemoryKnowledgeStore"/> nach SnapshotId.
/// </summary>
public sealed class InMemoryContentRepository(InMemoryKnowledgeStore store) : IContentRepository
{
    public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NodeContent>>(
            store.Contents.Where(content => content.SnapshotId == snapshotId).ToArray());
}
