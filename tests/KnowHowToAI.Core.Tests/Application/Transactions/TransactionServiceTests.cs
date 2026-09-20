using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

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
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
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
    public async Task ListOpenAsync_ReturnsOpenTransactionsFromRepository()
    {
        var tx = CreateTransaction(GeneratedTransactionId, TransactionState.Open);
        var repository = new TransactionRepositoryFake { OpenTransactions = [tx] };
        var service = new TransactionService(
            repository,
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.ListOpenAsync();

        Assert.Single(result);
        Assert.Equal(GeneratedTransactionId, result[0].TransactionId);
    }

    [Fact]
    public async Task GetAsync_MissingTransaction_ReturnsStableTransactionNotFoundError()
    {
        var requestedTransactionId = new TransactionId(Guid.Parse("2344830c-5dd1-4d10-9d00-7784c4c28210"));
        var service = new TransactionService(
            new TransactionRepositoryFake(),
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.GetAsync(requestedTransactionId);

        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, result.Code);
        Assert.Equal(
            requestedTransactionId.ToString(),
            result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Theory]
    [InlineData(TransactionState.Committed)]
    [InlineData(TransactionState.Discarded)]
    public async Task GetAsync_ClosedTransaction_ReturnsTransactionMetadataSuccessfully(TransactionState state)
    {
        var transactionId = new TransactionId(Guid.Parse("12345678-1234-1234-1234-123456789abc"));
        var closedTransaction = CreateTransaction(transactionId, state);
        var service = new TransactionService(
            new TransactionRepositoryFake { FoundTransaction = closedTransaction },
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.GetAsync(transactionId);

        Assert.True(result.IsSuccess);
        Assert.Equal(state, result.Value!.State);
        Assert.Equal(transactionId, result.Value.TransactionId);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task ValidateAsync_MissingOrClosedTransaction_ReturnsStableError(string errorCode)
    {
        var transactionId = new TransactionId(Guid.Parse("071d5332-2a14-41c5-a3d0-4c6dfe8c41e9"));
        var expectedError = new DomainError(
            errorCode,
            "Guard fehlgeschlagen.",
            new Dictionary<string, string> { [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString() });
        var service = new TransactionService(
            new TransactionRepositoryFake(),
            new ValidationDataRepositoryFake
            {
                ReadResult = Result<WorkingSnapshotValidationData>.Failure(expectedError)
            },
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.ValidateAsync(transactionId);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(transactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public async Task ValidateAsync_ValidSnapshot_ForwardsWarningsUnchanged()
    {
        var transactionId = new TransactionId(Guid.Parse("071d5332-2a14-41c5-a3d0-4c6dfe8c41e9"));
        var service = new TransactionService(
            new TransactionRepositoryFake(),
            new ValidationDataRepositoryFake
            {
                ReadResult = Result<WorkingSnapshotValidationData>.Success(new WorkingSnapshotValidationData([], [], [], [], [], 17))
            },
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.ValidateAsync(transactionId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(17, result.Value.ChangeVersion);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task CommitAsync_MissingOrClosedTransaction_ReturnsRejection(string errorCode)
    {
        var transactionId = new TransactionId(Guid.Parse("4bd1a82e-17d7-4148-a8e2-089464e70dd7"));
        var expectedError = new DomainError(
            errorCode,
            "Nicht bearbeitbar.",
            new Dictionary<string, string> { [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString() });
        var service = new TransactionService(
            new TransactionRepositoryFake
            {
                CommitResult = new CommitTransactionResult(null, null, expectedError)
            },
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.CommitAsync(transactionId, "Freigabe");

        Assert.False(result.IsCommitted);
        Assert.Null(result.Transaction);
        Assert.Equal(errorCode, result.Error!.Code);
        Assert.Equal(transactionId.ToString(), result.Error.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public async Task CommitAsync_SnapshotConflict_ReturnsConflictError()
    {
        var transactionId = new TransactionId(Guid.Parse("4bd1a82e-17d7-4148-a8e2-089464e70dd7"));
        var conflictError = new DomainError(
            TransactionValidationErrorCodes.SnapshotConflict,
            "Konflikt.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString(),
                [TransactionValidationErrorCodes.BaseSnapshotIdDetail] = "100",
                [TransactionValidationErrorCodes.CurrentSnapshotIdDetail] = "101"
            });
        var service = new TransactionService(
            new TransactionRepositoryFake
            {
                CommitResult = new CommitTransactionResult(null, null, conflictError)
            },
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.CommitAsync(transactionId, null);

        Assert.False(result.IsCommitted);
        Assert.Equal(TransactionValidationErrorCodes.SnapshotConflict, result.Error!.Code);
    }

    [Fact]
    public async Task CommitAsync_SuccessWithWarnings_ForwardsTransactionAndWarnings()
    {
        var transactionId = new TransactionId(Guid.Parse("4bd1a82e-17d7-4148-a8e2-089464e70dd7"));
        var committedTransaction = CreateTransaction(transactionId, TransactionState.Committed);
        var warnings = new[] { new DomainWarning(QualityWarningCodes.NodeTooLarge, "Zu groß") };
        var validationReport = new TransactionValidationReport([], warnings, [], []);
        var service = new TransactionService(
            new TransactionRepositoryFake
            {
                CommitResult = new CommitTransactionResult(committedTransaction, validationReport, null)
            },
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.CommitAsync(transactionId, "Freigabe");

        Assert.True(result.IsCommitted);
        Assert.NotNull(result.Transaction);
        Assert.Equal(TransactionState.Committed, result.Transaction.State);
        Assert.NotNull(result.ValidationReport);
        Assert.Single(result.ValidationReport.Warnings);
        Assert.Equal(QualityWarningCodes.NodeTooLarge, result.ValidationReport.Warnings[0].Code);
    }

    [Fact]
    public async Task CommitAsync_ForwardsTransactionMessageAndConfiguredValidationPolicy()
    {
        var transactionId = new TransactionId(Guid.Parse("4bd1a82e-17d7-4148-a8e2-089464e70dd7"));
        var repository = new TransactionRepositoryFake();
        var service = new TransactionService(
            repository,
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        await service.CommitAsync(transactionId, "Freigabe");

        Assert.Equal(transactionId, repository.CommitRequest!.TransactionId);
        Assert.Equal("Freigabe", repository.CommitRequest.CommitMessage);
        Assert.Equal(4096, repository.CommitRequest.QualityWarningThresholds.ContentSizeWarningBytes);
        Assert.True(repository.CommitRequest.WarnOnPossibleEmbeddedHeading);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task DiscardAsync_MissingOrClosedTransaction_ReturnsStableError(string errorCode)
    {
        var transactionId = new TransactionId(Guid.Parse("20d73a7b-1c4d-4742-919e-d6bf82043277"));
        var expectedError = new DomainError(
            errorCode,
            "Nicht verwerfbar.",
            new Dictionary<string, string> { [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString() });
        var repository = new TransactionRepositoryFake
        {
            DiscardResult = Result<KnowledgeTransaction>.Failure(expectedError)
        };
        var service = new TransactionService(
            repository,
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.DiscardAsync(transactionId);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(transactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public async Task DiscardAsync_OpenTransaction_ReturnsDiscardedTransaction()
    {
        var transactionId = new TransactionId(Guid.Parse("20d73a7b-1c4d-4742-919e-d6bf82043277"));
        var discardedTx = CreateTransaction(transactionId, TransactionState.Discarded);
        var repository = new TransactionRepositoryFake
        {
            DiscardResult = Result<KnowledgeTransaction>.Success(discardedTx)
        };
        var service = new TransactionService(
            repository,
            new ValidationDataRepositoryFake(),
            new FixedIdentifierGenerator { FixedTransactionId = GeneratedTransactionId },
            ValidationPolicy());

        var result = await service.DiscardAsync(transactionId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionState.Discarded, result.Value!.State);
        Assert.Equal(transactionId, repository.DiscardedTransactionId);
    }

    private static KnowledgeTransaction CreateTransaction(TransactionId transactionId, TransactionState state) =>
        new(
            transactionId,
            new SnapshotId(41),
            new SnapshotId(42),
            state,
            ChangeVersion: 0,
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            CommittedAtUtc: state == TransactionState.Committed ? DateTimeOffset.UnixEpoch : null,
            Purpose: null,
            Actor: null,
            Client: null,
            CommitMessage: null);

    private static ValidationPolicy ValidationPolicy() => new()
    {
        ContentSizeWarningBytes = 4096,
        ChildCountWarning = 25,
        HierarchyDepthWarning = 8,
        PossibleEmbeddedHeadingWarning = true
    };

    private sealed class TransactionRepositoryFake : ITransactionRepository
    {
        public BeginTransactionRequest? BeginRequest { get; private set; }

        public TransactionId? DiscardedTransactionId { get; private set; }

        public CommitTransactionRequest? CommitRequest { get; private set; }

        public KnowledgeTransaction? FoundTransaction { get; init; }

        public CommitTransactionResult CommitResult { get; init; } = new(null, null, null);

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
            Task.FromResult(FoundTransaction);

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default)
        {
            CommitRequest = request;
            return Task.FromResult(CommitResult);
        }

        public IReadOnlyList<KnowledgeTransaction> OpenTransactions { get; set; } = [];

        public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OpenTransactions);

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
