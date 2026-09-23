using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Web.Tests.Workflow;

[Trait("Category", "Unit")]
public sealed class WebWriteCoordinatorTests
{
    private static readonly SnapshotId CurrentId = new(17);
    private static readonly TransactionId FixedTransactionId = new(Guid.Parse("2309a64b-e96a-49e9-89e4-48658eb18231"));

    [Fact]
    public async Task FirstWrite_BeginsExpectedDraftAndReturnsItsId()
    {
        var fixture = new Fixture();
        var mutationTransactionId = (TransactionId?)null;
        long? expectedVersion = null;

        var result = await fixture.Coordinator.WriteAsync(
            CurrentId.Value,
            (transactionId, version, _) =>
            {
                mutationTransactionId = transactionId;
                expectedVersion = version;
                return Task.FromResult(Result<WriteValue>.Success(new WriteValue(1)));
            },
            value => value.ChangeVersion);

        Assert.True(result.Mutation.IsSuccess);
        Assert.True(result.StartedTransaction);
        Assert.Equal(FixedTransactionId, result.TransactionId);
        Assert.Equal(result.TransactionId, mutationTransactionId);
        Assert.Equal(0, expectedVersion);
        Assert.Equal(1, fixture.TransactionRepository.BeginCalls);
        Assert.Equal("Wissenspflege", fixture.TransactionRepository.LastBeginRequest!.Purpose);
        Assert.Equal("Dummy Actor", fixture.TransactionRepository.LastBeginRequest.Actor);
        Assert.Equal("Web UI", fixture.TransactionRepository.LastBeginRequest.Client);
        Assert.Equal(result.TransactionId, fixture.Workspace.ActiveTransactionId);
        Assert.Contains($"transactionId={FixedTransactionId.Value:D}", fixture.NavigationManager.Uri);
        Assert.Equal(1, fixture.Workspace.CurrentChangeVersion);
        Assert.False(fixture.Workspace.CurrentContext.IsDirty);
    }

    [Fact]
    public async Task FollowingWrite_UsesSelectedDraftWithoutBeginningAnother()
    {
        var fixture = new Fixture();
        var first = await fixture.Coordinator.WriteAsync(
            CurrentId.Value,
            (_, _, _) => Task.FromResult(Result<WriteValue>.Success(new WriteValue(1))),
            value => value.ChangeVersion);

        var result = await fixture.Coordinator.WriteAsync(
            CurrentId.Value,
            (transactionId, version, _) =>
            {
                Assert.Equal(first.TransactionId, transactionId);
                Assert.Equal(1, version);
                return Task.FromResult(Result<WriteValue>.Success(new WriteValue(2)));
            },
            value => value.ChangeVersion);

        Assert.False(result.StartedTransaction);
        Assert.Equal(first.TransactionId, result.TransactionId);
        Assert.Equal(1, fixture.TransactionRepository.BeginCalls);
        Assert.Equal(2, fixture.Workspace.CurrentChangeVersion);
    }

    [Fact]
    public async Task ConcurrentFirstWrites_ShareOneDraftAndSerializeChangeVersions()
    {
        var fixture = new Fixture();
        var firstMutationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondMutationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.TransactionRepository.PauseBegin();

        var firstWrite = fixture.Coordinator.WriteAsync(
            CurrentId.Value,
            (_, version, _) =>
            {
                Assert.Equal(0, version);
                firstMutationStarted.SetResult();
                return Task.FromResult(Result<WriteValue>.Success(new WriteValue(1)));
            },
            value => value.ChangeVersion);

        await fixture.TransactionRepository.BeginStarted.Task;
        var secondWrite = fixture.Coordinator.WriteAsync(
            CurrentId.Value,
            (_, version, _) =>
            {
                Assert.Equal(1, version);
                secondMutationStarted.SetResult();
                return Task.FromResult(Result<WriteValue>.Success(new WriteValue(2)));
            },
            value => value.ChangeVersion);

        fixture.TransactionRepository.ReleaseBegin();
        await Task.WhenAll(firstMutationStarted.Task, secondMutationStarted.Task);
        var secondResult = await secondWrite;
        var firstResult = await firstWrite;

        Assert.Equal(firstResult.TransactionId, secondResult.TransactionId);
        Assert.Equal(1, fixture.TransactionRepository.BeginCalls);
        Assert.True(firstResult.StartedTransaction);
        Assert.False(secondResult.StartedTransaction);
    }

    [Fact]
    public async Task StaleCurrent_RejectsBeforeBeginAndMutation()
    {
        var fixture = new Fixture(currentSnapshotId: new SnapshotId(18));
        var mutationCalled = false;

        var result = await fixture.Coordinator.WriteAsync(
            CurrentId.Value,
            (_, _, _) =>
            {
                mutationCalled = true;
                return Task.FromResult(Result<WriteValue>.Success(new WriteValue(1)));
            },
            value => value.ChangeVersion);

        Assert.False(result.Mutation.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.SnapshotConflict, result.Mutation.Code);
        Assert.False(mutationCalled);
        Assert.Equal(0, fixture.TransactionRepository.BeginCalls);
        Assert.Null(result.TransactionId);
        Assert.False(fixture.Workspace.ActiveTransactionId.HasValue);
    }

    [Fact]
    public async Task MutationFailure_AfterBeginStillReturnsDraftAndKeepsItSelected()
    {
        var fixture = new Fixture();
        fixture.EditState.SetDirty(true);
        var result = await fixture.Coordinator.WriteAsync(
            CurrentId.Value,
            (_, _, _) => Task.FromResult(Result<WriteValue>.Failure(new DomainError("ChangeVersionConflict", "stale"))),
            value => value.ChangeVersion);

        Assert.False(result.Mutation.IsSuccess);
        Assert.Equal("ChangeVersionConflict", result.Mutation.Code);
        Assert.True(result.StartedTransaction);
        Assert.Equal(FixedTransactionId, result.TransactionId);
        Assert.Equal(result.TransactionId, fixture.Workspace.ActiveTransactionId);
        Assert.True(fixture.EditState.IsDirty);
        Assert.Equal(1, fixture.TransactionRepository.BeginCalls);
    }

    private sealed record WriteValue(long ChangeVersion);

    private sealed class Fixture
    {
        public Fixture(SnapshotId? currentSnapshotId = null)
        {
            Workspace = new WorkspaceState();
            EditState = new WorkspaceEditState();
            Workspace.SetAudience("Developer");
            Workspace.SetLoadedSnapshotId((currentSnapshotId ?? CurrentId).Value);
            var transactionRepo = TransactionRepository = new FakeTransactionRepository(currentSnapshotId ?? CurrentId);
            var transactionService = new TransactionService(
                transactionRepo,
                new FakeValidationRepository(),
                new FixedIdentifierGenerator(),
                new ValidationPolicy());
            NavigationManager = new TestNavigationManager();
            Coordinator = new WebWriteCoordinator(
                transactionService,
                new FakeSnapshotRepository(currentSnapshotId ?? CurrentId),
                new FakeCurrentUserService(),
                Workspace,
                NavigationManager);
        }

        public WorkspaceState Workspace { get; }
        public WorkspaceEditState EditState { get; }
        public FakeTransactionRepository TransactionRepository { get; }
        public WebWriteCoordinator Coordinator { get; }
        public TestNavigationManager NavigationManager { get; }
    }

    private sealed class FakeSnapshotRepository(SnapshotId snapshotId) : ISnapshotRepository
    {
        public Task<Snapshot?> FindAsync(SnapshotId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Snapshot?>(id == snapshotId ? new Snapshot(id, null, SnapshotState.Committed, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch) : null);

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Snapshot(snapshotId, null, SnapshotState.Committed, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));

        public Task<IReadOnlyList<Snapshot>> ListCommittedAsync(int limit, SnapshotId? beforeSnapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Snapshot>>([]);
    }

    private sealed class FakeTransactionRepository(SnapshotId snapshotId) : ITransactionRepository
    {
        private TaskCompletionSource? _beginRelease;
        public TaskCompletionSource BeginStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int BeginCalls { get; private set; }
        public BeginTransactionRequest? LastBeginRequest { get; private set; }

        public void PauseBegin() => _beginRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ReleaseBegin() => _beginRelease?.SetResult();

        public async Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default)
        {
            BeginCalls++;
            LastBeginRequest = request;
            BeginStarted.SetResult();
            if (_beginRelease is not null)
                await _beginRelease.Task.WaitAsync(cancellationToken);
            return new KnowledgeTransaction(
                request.TransactionId,
                snapshotId,
                new SnapshotId(snapshotId.Value + 1),
                TransactionState.Open,
                0,
                DateTimeOffset.UnixEpoch,
                null,
                request.Purpose,
                request.Actor,
                request.Client,
                null);
        }

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeTransaction?>(null);

        public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeTransaction>>([]);

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeValidationRepository : IWorkingSnapshotValidationDataRepository
    {
        public Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => FixedTransactionId;
        public NodeId CreateNodeId() => new(Guid.NewGuid());
        public ContentRevisionId CreateContentRevisionId() => new(Guid.NewGuid());
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser GetCurrentUser() => new("dummy", "Dummy Actor");
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/knowledge?audienceId=Developer");

        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();

        protected override void NavigateToCore(string uri, NavigationOptions options) => Uri = ToAbsoluteUri(uri).ToString();
    }
}
