using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Transactions;

[Trait("Category", "Unit")]
public sealed class TransactionServiceTests
{
    private static readonly TransactionId GeneratedTransactionId = new(Guid.Parse("f0a71c07-3f92-45f6-900a-5cc7a3bdc9b4"));

    [Fact]
    public async Task BeginAsync_GeneratesIdAndForwardsAuditMetadataToRepository()
    {
        var repository = new TransactionRepositoryFake();
        var service = new TransactionService(
            repository,
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator(),
            ValidationPolicy());

        var result = await service.BeginAsync(new BeginTransactionOptions("Import", "Agent", "MCP"));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedTransactionId, result.Value!.TransactionId);
        Assert.Equal(GeneratedTransactionId, repository.BeginRequest!.TransactionId);
        Assert.Equal("Import", repository.BeginRequest.Purpose);
        Assert.Equal("Agent", repository.BeginRequest.Actor);
        Assert.Equal("MCP", repository.BeginRequest.Client);
    }

    [Fact]
    public async Task GetAsync_MissingTransaction_ReturnsStableTransactionNotFoundError()
    {
        var requestedTransactionId = new TransactionId(Guid.Parse("2344830c-5dd1-4d10-9d00-7784c4c28210"));
        var service = new TransactionService(
            new TransactionRepositoryFake(),
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator(),
            ValidationPolicy());

        var result = await service.GetAsync(requestedTransactionId);

        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, result.Code);
        Assert.Equal(
            requestedTransactionId.ToString(),
            result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public async Task DiscardAsync_ForwardsRepositoryDomainErrorUnchanged()
    {
        var transactionId = new TransactionId(Guid.Parse("20d73a7b-1c4d-4742-919e-d6bf82043277"));
        var expectedError = new DomainError("TransactionClosed", "Bereits geschlossen.");
        var repository = new TransactionRepositoryFake
        {
            DiscardResult = Result<KnowledgeTransaction>.Failure(expectedError)
        };
        var service = new TransactionService(
            repository,
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator(),
            ValidationPolicy());

        var result = await service.DiscardAsync(transactionId);

        Assert.False(result.IsSuccess);
        Assert.Same(expectedError, result.Error);
        Assert.Equal(transactionId, repository.DiscardedTransactionId);
    }

    [Fact]
    public async Task ValidateAsync_ForwardsOpenTransactionGuardErrorUnchanged()
    {
        var transactionId = new TransactionId(Guid.Parse("071d5332-2a14-41c5-a3d0-4c6dfe8c41e9"));
        var expectedError = new DomainError(TransactionValidationErrorCodes.TransactionClosed, "Bereits geschlossen.");
        var service = new TransactionService(
            new TransactionRepositoryFake(),
            new ValidationDataRepositoryFake
            {
                ReadResult = Result<WorkingSnapshotValidationData>.Failure(expectedError)
            },
            new FixedIdentifierGenerator(),
            ValidationPolicy());

        var result = await service.ValidateAsync(transactionId);

        Assert.False(result.IsSuccess);
        Assert.Same(expectedError, result.Error);
    }

    [Fact]
    public async Task CommitAsync_ForwardsTransactionMessageAndConfiguredValidationPolicy()
    {
        var transactionId = new TransactionId(Guid.Parse("4bd1a82e-17d7-4148-a8e2-089464e70dd7"));
        var repository = new TransactionRepositoryFake();
        var service = new TransactionService(
            repository,
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator(),
            ValidationPolicy());

        await service.CommitAsync(transactionId, "Freigabe");

        Assert.Equal(transactionId, repository.CommitRequest!.TransactionId);
        Assert.Equal("Freigabe", repository.CommitRequest.CommitMessage);
        Assert.Equal(4096, repository.CommitRequest.QualityWarningThresholds.ContentSizeWarningBytes);
        Assert.True(repository.CommitRequest.WarnOnPossibleEmbeddedHeading);
    }

    private static ValidationPolicy ValidationPolicy() => new()
    {
        ContentSizeWarningBytes = 4096,
        ChildCountWarning = 25,
        HierarchyDepthWarning = 8,
        PossibleEmbeddedHeadingWarning = true
    };

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => GeneratedTransactionId;

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId() => throw new NotSupportedException();
    }

    private sealed class TransactionRepositoryFake : ITransactionRepository
    {
        public BeginTransactionRequest? BeginRequest { get; private set; }

        public TransactionId? DiscardedTransactionId { get; private set; }

        public CommitTransactionRequest? CommitRequest { get; private set; }

        public Result<KnowledgeTransaction> DiscardResult { get; init; } =
            Result<KnowledgeTransaction>.Success(new KnowledgeTransaction(
                GeneratedTransactionId,
                new SnapshotId(41),
                new SnapshotId(42),
                TransactionState.Open,
                ChangeVersion: 0,
                CreatedAtUtc: DateTimeOffset.UnixEpoch,
                CommittedAtUtc: null,
                Purpose: null,
                Actor: null,
                Client: null,
                CommitMessage: null));

        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default)
        {
            BeginRequest = request;
            return Task.FromResult(new KnowledgeTransaction(
                GeneratedTransactionId,
                new SnapshotId(41),
                new SnapshotId(42),
                TransactionState.Open,
                ChangeVersion: 0,
                CreatedAtUtc: DateTimeOffset.UnixEpoch,
                CommittedAtUtc: null,
                request.Purpose,
                request.Actor,
                request.Client,
                CommitMessage: null));
        }

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeTransaction?>(null);

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default)
        {
            CommitRequest = request;
            return Task.FromResult(new CommitTransactionResult(null, null, null));
        }

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
        {
            DiscardedTransactionId = transactionId;
            return Task.FromResult(DiscardResult);
        }
    }

    private sealed class ValidationDataRepositoryFake : IWorkingSnapshotValidationDataRepository
    {
        public Result<WorkingSnapshotValidationData> ReadResult { get; init; } =
            Result<WorkingSnapshotValidationData>.Failure(new DomainError(
                TransactionValidationErrorCodes.TransactionNotFound,
                "Nicht verwendet."));

        public Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
            TransactionId transactionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadResult);
    }
}
