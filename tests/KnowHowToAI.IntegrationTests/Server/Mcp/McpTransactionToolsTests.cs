using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Transactions;
using KnowHowToAI.Server.Mcp.Tools.Transactions;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der Transaction-Tools: dünne Delegation an den
/// TransactionService mit protokollkonformer Error-Struktur, ID-Round-Trip
/// und Befundtransparenz. Keine SQL- oder STDIO-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpTransactionToolsTests
{
    private static readonly TransactionId SampleTransactionId =
        new(Guid.Parse("0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90"));

    private static readonly TransactionId OtherTransactionId =
        new(Guid.Parse("2344830c-5dd1-4d10-9d00-7784c4c28210"));

    [Fact]
    public async Task BeginTransaction_DelegatesAuditMetadataAndReturnsTransactionData()
    {
        var repository = new ScriptedTransactionRepository();
        var tools = CreateTools(repository);

        var envelope = await tools.BeginTransaction("Import", "Agent", "MCP");

        var request = Assert.Single(repository.BeginRequests);
        Assert.Equal("Import", request.Purpose);
        Assert.Equal("Agent", request.Actor);
        Assert.Equal("MCP", request.Client);
        Assert.True(envelope.IsSuccess);
        Assert.Equal(SampleTransactionId.ToString(), envelope.Data!.TransactionId);
        Assert.Equal("41", envelope.Data.BaseSnapshotId);
        Assert.Equal("42", envelope.Data.WorkingSnapshotId);
        Assert.Equal("Open", envelope.Data.State);
    }

    [Fact]
    public async Task BeginTransaction_WithoutAuditMetadata_DelegatesEmptyOptions()
    {
        var repository = new ScriptedTransactionRepository();
        var tools = CreateTools(repository);

        var envelope = await tools.BeginTransaction();

        Assert.True(envelope.IsSuccess);
        var request = Assert.Single(repository.BeginRequests);
        Assert.Null(request.Purpose);
        Assert.Null(request.Actor);
        Assert.Null(request.Client);
    }

    [Fact]
    public async Task GetTransaction_UnknownTransaction_ReturnsStableTransactionNotFoundEnvelope()
    {
        var tools = CreateTools(new ScriptedTransactionRepository());

        var envelope = await tools.GetTransaction(OtherTransactionId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, envelope.Code);
        Assert.Equal(
            OtherTransactionId.ToString(),
            envelope.Details![TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task GetTransaction_KnownTransaction_ReturnsMetadataWithoutReformatting()
    {
        var tools = CreateTools(new ScriptedTransactionRepository
        {
            FoundResponse = CommittedTransaction()
        });

        var envelope = await tools.GetTransaction(SampleTransactionId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal("Committed", envelope.Data!.State);
        Assert.Equal("Freigabe", envelope.Data.CommitMessage);
        Assert.Equal(SampleTransactionId.ToString(), envelope.Data.TransactionId);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("17")]
    public async Task TransactionTools_MalformedTransactionId_IsRejectedWithoutServiceCall(string rawTransactionId)
    {
        var repository = new ScriptedTransactionRepository();
        var tools = CreateTools(repository);

        var getEnvelope = await tools.GetTransaction(rawTransactionId);
        var validateEnvelope = await tools.ValidateTransaction(rawTransactionId);
        var commitEnvelope = await tools.CommitTransaction(rawTransactionId);
        var discardEnvelope = await tools.DiscardTransaction(rawTransactionId);

        Assert.All(
            new[] { getEnvelope.Code, validateEnvelope.Code, commitEnvelope.Code, discardEnvelope.Code },
            code => Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, code));
        Assert.Empty(repository.DiscardRequests);
        Assert.Empty(repository.CommitRequests);
    }

    [Fact]
    public async Task ValidateTransaction_OpenTransaction_ReturnsStructuredReportAndStaysUnchanged()
    {
        var repository = new ScriptedTransactionRepository();
        var tools = CreateTools(repository);

        var first = await tools.ValidateTransaction(SampleTransactionId.ToString());
        var second = await tools.ValidateTransaction(SampleTransactionId.ToString());

        Assert.True(first.IsSuccess);
        Assert.True(first.Data!.IsValid);
        Assert.Empty(first.Data.Errors);
        Assert.Empty(first.Data.Warnings);
        Assert.Equal(JsonSerializer.Serialize(first.Data), JsonSerializer.Serialize(second.Data));
        Assert.Equal(0, repository.CommitRequests.Count);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task ValidateTransaction_MissingOrClosedTransaction_ReturnsStableErrorEnvelope(string errorCode)
    {
        var expectedError = new DomainError(
            errorCode,
            "Nicht prüfbar.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.TransactionIdDetail] = SampleTransactionId.ToString()
            });
        var tools = CreateTools(validationData: Result<WorkingSnapshotValidationData>.Failure(expectedError));

        var envelope = await tools.ValidateTransaction(SampleTransactionId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(errorCode, envelope.Code);
        Assert.Equal(
            SampleTransactionId.ToString(),
            envelope.Details![TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task CommitTransaction_DelegatesCommitMessageAndReturnsCommittedStateWithWarnings()
    {
        var warnings = new[] { new DomainWarning("NodeTooLarge", "Content übersteigt die Warnschwelle.") };
        var repository = new ScriptedTransactionRepository
        {
            CommitResponse = new CommitTransactionResult(
                CommittedTransaction(),
                new TransactionValidationReport([], warnings, [], []),
                null)
        };
        var tools = CreateTools(repository);

        var envelope = await tools.CommitTransaction(SampleTransactionId.ToString(), "Freigabe");

        var request = Assert.Single(repository.CommitRequests);
        Assert.Equal(SampleTransactionId, request.TransactionId);
        Assert.Equal("Freigabe", request.CommitMessage);
        Assert.True(envelope.IsSuccess);
        Assert.Equal("Committed", envelope.Data!.State);
        Assert.Equal("NodeTooLarge", Assert.Single(envelope.Warnings!).Code);
    }

    [Fact]
    public async Task CommitTransaction_SnapshotConflict_ReturnsConflictDetailsWithoutStateChange()
    {
        var repository = new ScriptedTransactionRepository
        {
            CommitResponse = new CommitTransactionResult(
                null,
                null,
                new DomainError(
                    TransactionValidationErrorCodes.SnapshotConflict,
                    "Der Basis-Snapshot ist nicht mehr aktuell.",
                    new Dictionary<string, string>
                    {
                        [TransactionValidationErrorCodes.BaseSnapshotIdDetail] = "100",
                        [TransactionValidationErrorCodes.CurrentSnapshotIdDetail] = "101"
                    }))
        };
        var tools = CreateTools(repository);

        var envelope = await tools.CommitTransaction(SampleTransactionId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.SnapshotConflict, envelope.Code);
        Assert.Equal("100", envelope.Details![TransactionValidationErrorCodes.BaseSnapshotIdDetail]);
        Assert.Equal("101", envelope.Details![TransactionValidationErrorCodes.CurrentSnapshotIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task CommitTransaction_ClosedTransaction_ReturnsStableErrorEnvelope()
    {
        var repository = new ScriptedTransactionRepository
        {
            CommitResponse = new CommitTransactionResult(
                null,
                null,
                new DomainError(
                    TransactionValidationErrorCodes.TransactionClosed,
                    "Die Transaction ist bereits geschlossen.",
                    new Dictionary<string, string>
                    {
                        [TransactionValidationErrorCodes.TransactionIdDetail] = SampleTransactionId.ToString()
                    }))
        };
        var tools = CreateTools(repository);

        var envelope = await tools.CommitTransaction(SampleTransactionId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, envelope.Code);
    }

    [Fact]
    public async Task DiscardTransaction_DelegatesDiscardAndReturnsPayloadFreeSuccess()
    {
        var repository = new ScriptedTransactionRepository();
        var tools = CreateTools(repository);

        var envelope = await tools.DiscardTransaction(SampleTransactionId.ToString());

        Assert.Equal(SampleTransactionId, Assert.Single(repository.DiscardRequests));
        Assert.True(envelope.IsSuccess);
        Assert.Equal("Success", envelope.Code);
        Assert.Null(envelope.Data);
        Assert.Null(envelope.Message);
        Assert.Null(envelope.Warnings);
        Assert.Equal(0, repository.CommitRequests.Count);
    }

    [Fact]
    public async Task DiscardTransaction_ClosedTransaction_ReturnsStableErrorEnvelope()
    {
        var repository = new ScriptedTransactionRepository
        {
            DiscardResponse = Result<KnowledgeTransaction>.Failure(new DomainError(
                TransactionValidationErrorCodes.TransactionClosed,
                "Die Transaction ist bereits geschlossen.",
                new Dictionary<string, string>
                {
                    [TransactionValidationErrorCodes.TransactionIdDetail] = SampleTransactionId.ToString()
                }))
        };
        var tools = CreateTools(repository);

        var envelope = await tools.DiscardTransaction(SampleTransactionId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionClosed, envelope.Code);
        Assert.Equal(
            SampleTransactionId.ToString(),
            envelope.Details![TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public async Task SuccessAndErrorEnvelopes_SerializeWithStableFieldNames()
    {
        var repository = new ScriptedTransactionRepository();
        var tools = CreateTools(repository);

        var successJson = JsonSerializer.Serialize(await tools.BeginTransaction());
        var closedRepository = new ScriptedTransactionRepository
        {
            DiscardResponse = Result<KnowledgeTransaction>.Failure(new DomainError(
                TransactionValidationErrorCodes.TransactionClosed,
                "Die Transaction ist bereits geschlossen.",
                new Dictionary<string, string>
                {
                    [TransactionValidationErrorCodes.TransactionIdDetail] = SampleTransactionId.ToString()
                }))
        };
        var errorJson = JsonSerializer.Serialize(
            await CreateTools(closedRepository).DiscardTransaction(SampleTransactionId.ToString()));

        using var success = JsonDocument.Parse(successJson);
        Assert.Equal("Success", success.RootElement.GetProperty("code").GetString());
        Assert.Equal(SampleTransactionId.ToString(),
            success.RootElement.GetProperty("data").GetProperty("transactionId").GetString());
        Assert.DoesNotContain("\"message\"", successJson);

        using var error = JsonDocument.Parse(errorJson);
        Assert.Equal("TransactionClosed", error.RootElement.GetProperty("code").GetString());
        Assert.Equal("Die Transaction ist bereits geschlossen.", error.RootElement.GetProperty("message").GetString());
        Assert.False(error.RootElement.TryGetProperty("data", out _));
    }

    private static TransactionTools CreateTools(
        ScriptedTransactionRepository? repository = null,
        Result<WorkingSnapshotValidationData>? validationData = null) =>
        new(new TransactionService(
            repository ?? new ScriptedTransactionRepository(),
            new ScriptedValidationDataRepository { ValidationDataResponse = validationData ?? ValidData() },
            new FixedIdentifierGenerator(),
            ValidationPolicy()));

    private static Result<WorkingSnapshotValidationData> ValidData() =>
        Result<WorkingSnapshotValidationData>.Success(new WorkingSnapshotValidationData([], [], [], [], []));

    private static ValidationPolicy ValidationPolicy() => new()
    {
        ContentSizeWarningBytes = 4096,
        ChildCountWarning = 25,
        HierarchyDepthWarning = 8,
        PossibleEmbeddedHeadingWarning = true
    };

    private static KnowledgeTransaction OpenTransaction() => new(
        SampleTransactionId,
        new SnapshotId(41),
        new SnapshotId(42),
        TransactionState.Open,
        ChangeVersion: 0,
        CreatedAtUtc: DateTimeOffset.UnixEpoch,
        CommittedAtUtc: null,
        Purpose: "Import",
        Actor: "Agent",
        Client: "MCP",
        CommitMessage: null);

    private static KnowledgeTransaction CommittedTransaction() => new(
        SampleTransactionId,
        new SnapshotId(41),
        new SnapshotId(42),
        TransactionState.Committed,
        ChangeVersion: 1,
        CreatedAtUtc: DateTimeOffset.UnixEpoch,
        CommittedAtUtc: DateTimeOffset.UnixEpoch,
        Purpose: null,
        Actor: null,
        Client: null,
        CommitMessage: "Freigabe");

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => SampleTransactionId;

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId() => throw new NotSupportedException();
    }

    private sealed class ScriptedTransactionRepository : ITransactionRepository
    {
        public KnowledgeTransaction BeginResponse { get; init; } = OpenTransaction();

        public KnowledgeTransaction? FoundResponse { get; init; }

        public CommitTransactionResult CommitResponse { get; init; } =
            new(null, null, null);

        public Result<KnowledgeTransaction> DiscardResponse { get; init; } =
            Result<KnowledgeTransaction>.Success(OpenTransaction());

        public List<BeginTransactionRequest> BeginRequests { get; } = [];

        public List<CommitTransactionRequest> CommitRequests { get; } = [];

        public List<TransactionId> DiscardRequests { get; } = [];

        public Task<KnowledgeTransaction> BeginAsync(
            BeginTransactionRequest request, CancellationToken cancellationToken = default)
        {
            BeginRequests.Add(request);
            return Task.FromResult(BeginResponse);
        }

        public Task<KnowledgeTransaction?> FindAsync(
            TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(FoundResponse);

        public Task<CommitTransactionResult> CommitAsync(
            CommitTransactionRequest request, CancellationToken cancellationToken = default)
        {
            CommitRequests.Add(request);
            return Task.FromResult(CommitResponse);
        }

        public Task<Result<KnowledgeTransaction>> DiscardAsync(
            TransactionId transactionId, CancellationToken cancellationToken = default)
        {
            DiscardRequests.Add(transactionId);
            return Task.FromResult(DiscardResponse);
        }
    }

    private sealed class ScriptedValidationDataRepository : IWorkingSnapshotValidationDataRepository
    {
        public Result<WorkingSnapshotValidationData> ValidationDataResponse { get; init; } =
            Result<WorkingSnapshotValidationData>.Success(
                new WorkingSnapshotValidationData([], [], [], [], []));

        public Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
            TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ValidationDataResponse);
    }
}
