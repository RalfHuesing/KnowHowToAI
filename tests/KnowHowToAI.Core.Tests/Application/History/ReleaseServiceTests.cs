using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.History;

[Trait("Category", "Unit")]
public sealed class ReleaseServiceTests
{
    private static readonly SnapshotId SnapshotId = new(17);
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateReleaseAsync_RepositoryRejectsDuplicateName_ForwardsStableError()
    {
        var expectedError = new DomainError(ReleaseErrorCodes.ReleaseNameConflict, "Bereits vergeben.");
        var repository = new ReleaseMutationRepositoryFake(Result<Release>.Failure(expectedError));
        var service = new ReleaseService(
            new SnapshotRepositoryFake(),
            repository,
            new ClockFake(),
            RetrievalPolicy());

        var result = await service.CreateReleaseAsync("v1", SnapshotId);

        Assert.False(result.IsSuccess);
        Assert.Same(expectedError, result.Error);
        Assert.Equal("v1", repository.Name);
        Assert.Equal(Now, repository.CreatedAtUtc);
    }

    private static RetrievalPolicy RetrievalPolicy() => new()
    {
        DefaultPageSize = 10,
        MaximumPageSize = 100,
        SearchPageSize = 10,
        SearchMaximumPageSize = 100,
        SnippetMaximumCharacters = 100
    };

    private sealed class SnapshotRepositoryFake : ISnapshotRepository
    {
        private static readonly Snapshot CommittedSnapshot = new(SnapshotId, null, SnapshotState.Committed, Now, Now);

        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Snapshot?>(snapshotId == SnapshotId ? CommittedSnapshot : null);

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(CommittedSnapshot);
    }

    private sealed class ReleaseMutationRepositoryFake(Result<Release> createResult) : IReleaseMutationRepository
    {
        public string? Name { get; private set; }

        public DateTimeOffset? CreatedAtUtc { get; private set; }

        public Task<Result<Release>> CreateAsync(ReleaseId releaseId, string name, SnapshotId snapshotId, DateTimeOffset createdAtUtc, CancellationToken cancellationToken = default)
        {
            Name = name;
            CreatedAtUtc = createdAtUtc;
            return Task.FromResult(createResult);
        }

        public Task<IReadOnlyList<Release>> ListAsync(int limit, long? afterReleaseId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ClockFake : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
