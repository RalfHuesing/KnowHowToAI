using System.Text.Json;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Contracts.Transactions;
using KnowHowToAI.Server.Mcp.Mapping;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Mapping-Vertragstests der Transaction-Tools: Domain-Ergebnisse auf den
/// gemeinsamen Envelope und die Transaktions-DTOs, ID-Round-Trip und
/// Befundtransparenz.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpTransactionMapperTests
{
    private const string HeadingNotAllowed = "HeadingNotAllowed";

    private static readonly TransactionId SampleTransactionId =
        new(Guid.Parse("0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90"));

    [Fact]
    public void TransactionData_UsesStableCamelCaseFieldNamesAndOmitsNulls()
    {
        var json = JsonSerializer.Serialize(McpTransactionMapper.ToData(MinimalTransaction()));

        using var document = JsonDocument.Parse(json);
        var data = document.RootElement;
        Assert.Equal(
            new[] { "transactionId", "baseSnapshotId", "workingSnapshotId", "state", "createdAtUtc" },
            data.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(SampleTransactionId.ToString(), data.GetProperty("transactionId").GetString());
        Assert.Equal("41", data.GetProperty("baseSnapshotId").GetString());
        Assert.Equal("42", data.GetProperty("workingSnapshotId").GetString());
        Assert.Equal("Open", data.GetProperty("state").GetString());
    }

    private static KnowledgeTransaction MinimalTransaction() => new(
        SampleTransactionId,
        new SnapshotId(41),
        new SnapshotId(42),
        TransactionState.Open,
        ChangeVersion: 0,
        CreatedAtUtc: DateTimeOffset.UnixEpoch,
        CommittedAtUtc: null,
        Purpose: null,
        Actor: null,
        Client: null,
        CommitMessage: null);

    [Fact]
    public void TransactionData_IncludesClosedTransactionMetadataAndCommitMessage()
    {
        var json = JsonSerializer.Serialize(McpTransactionMapper.ToData(CommittedTransaction()));

        using var document = JsonDocument.Parse(json);
        var data = document.RootElement;
        Assert.Equal("Committed", data.GetProperty("state").GetString());
        Assert.Equal("Freigabe", data.GetProperty("commitMessage").GetString());
        Assert.True(data.TryGetProperty("committedAtUtc", out _));
        Assert.Equal("Import", data.GetProperty("purpose").GetString());
    }

    [Fact]
    public void ValidationReport_MapsErrorsWarningsAndHintsToStructuredData()
    {
        var report = new TransactionValidationReport(
            [new DomainError(HeadingNotAllowed, "Überschrift ist unzulässig.", new Dictionary<string, string>
            {
                [TransactionValidationCodes.NodeIdDetail] = SampleTransactionId.ToString(),
                [TransactionValidationCodes.RoleIdDetail] = "Developer"
            })],
            [new DomainWarning("NodeTooLarge", "Content übersteigt die Warnschwelle.", new Dictionary<string, string>
            {
                [TransactionValidationCodes.NodeIdDetail] = SampleTransactionId.ToString(),
                ["actualBytes"] = "5123",
                ["thresholdBytes"] = "4096"
            })],
            [new StaleContent(new NodeId(Guid.Parse("1f7c6a1e-4c1e-4c1e-9f31-5d3e2a8d7b90")),
                new RoleId("Developer"),
                new ContentRevisionId(Guid.Parse("2f7c6a1e-4c1e-4c1e-9f31-5d3e2a8d7b90")))],
            [new RefactoringCandidate(new NodeId(Guid.Parse("3f7c6a1e-4c1e-4c1e-9f31-5d3e2a8d7b90")), ["NodeTooLarge"])]);

        var data = McpTransactionMapper.ToData(report);

        Assert.False(data.IsValid);
        var error = Assert.Single(data.Errors);
        Assert.Equal(HeadingNotAllowed, error.Code);
        Assert.Equal("Developer", error.Details![TransactionValidationCodes.RoleIdDetail]);
        var warning = Assert.Single(data.Warnings);
        Assert.Equal("NodeTooLarge", warning.Code);
        var stale = Assert.Single(data.StaleContents);
        Assert.Equal("Developer", stale.RoleId);
        var candidate = Assert.Single(data.RefactoringCandidates);
        Assert.Equal(["NodeTooLarge"], candidate.ReasonCodes);
    }

    [Fact]
    public void ValidationReport_WithoutDetails_OmitsDetailsFieldOnIssues()
    {
        var report = new TransactionValidationReport(
            [new DomainError(HeadingNotAllowed, "Überschrift ist unzulässig.")], [], [], []);

        var json = JsonSerializer.Serialize(McpTransactionMapper.ToData(report));

        using var document = JsonDocument.Parse(json);
        Assert.DoesNotContain("details", document.RootElement.GetProperty("errors")[0].ToString());
    }

    [Fact]
    public void ResultEnvelope_MapsSuccessAndStableError()
    {
        var success = McpTransactionMapper.ToEnvelope(Result<KnowledgeTransaction>.Success(OpenTransaction()));
        var failure = McpTransactionMapper.ToEnvelope(Result<KnowledgeTransaction>.Failure(new DomainError(
            TransactionValidationErrorCodes.TransactionNotFound,
            "Die angefragte Transaction existiert nicht.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.TransactionIdDetail] = SampleTransactionId.ToString()
            })));

        Assert.True(success.IsSuccess);
        Assert.Equal("Open", success.Data!.State);
        Assert.False(failure.IsSuccess);
        Assert.Equal("TransactionNotFound", failure.Code);
        Assert.Equal(
            SampleTransactionId.ToString(),
            failure.Details![TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Null(failure.Data);
    }

    [Fact]
    public void CommitEnvelope_SuccessSurfacesValidationWarningsNextToPayload()
    {
        var warnings = new[] { new DomainWarning("NodeTooLarge", "Content übersteigt die Warnschwelle.") };
        var result = new CommitTransactionResult(
            CommittedTransaction(), new TransactionValidationReport([], warnings, [], []), null);

        var envelope = McpTransactionMapper.ToEnvelope(result);

        Assert.True(envelope.IsSuccess);
        Assert.Equal("Committed", envelope.Data!.State);
        var warning = Assert.Single(envelope.Warnings!);
        Assert.Equal("NodeTooLarge", warning.Code);
    }

    [Fact]
    public void CommitEnvelope_RejectionKeepsFindingsVisibleAndCarriesStableError()
    {
        var warnings = new[] { new DomainWarning("NodeTooLarge", "Content übersteigt die Warnschwelle.") };
        var report = new TransactionValidationReport([], warnings, [], []);
        var conflict = new DomainError(
            TransactionValidationErrorCodes.SnapshotConflict,
            "Der Basis-Snapshot ist nicht mehr aktuell.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.BaseSnapshotIdDetail] = "100",
                [TransactionValidationErrorCodes.CurrentSnapshotIdDetail] = "101"
            });
        var result = new CommitTransactionResult(null, report, conflict);

        var envelope = McpTransactionMapper.ToEnvelope(result);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("SnapshotConflict", envelope.Code);
        Assert.Equal("100", envelope.Details![TransactionValidationErrorCodes.BaseSnapshotIdDetail]);
        Assert.Equal("101", envelope.Details![TransactionValidationErrorCodes.CurrentSnapshotIdDetail]);
        Assert.Equal("NodeTooLarge", Assert.Single(envelope.Warnings!).Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public void ParseTransactionId_AcceptsOutputFormatAndPreservesRoundTrip()
    {
        var result = McpTransactionMapper.ParseTransactionId(SampleTransactionId.ToString());

        Assert.True(result.IsSuccess);
        Assert.Equal(SampleTransactionId, result.Value);
        Assert.Equal(SampleTransactionId.ToString(), result.Value!.ToString());
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("{0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90}")]
    [InlineData("")]
    [InlineData("17")]
    public void ParseTransactionId_RejectsForeignFormatsAsTransactionNotFound(string rawTransactionId)
    {
        var result = McpTransactionMapper.ParseTransactionId(rawTransactionId);

        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, result.Error!.Code);
        Assert.Equal(rawTransactionId, result.Error.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public void ParseTransactionId_MissingValue_ReportsTransactionNotFoundWithEmptyDetail()
    {
        var result = McpTransactionMapper.ParseTransactionId(null);

        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.TransactionNotFound, result.Error!.Code);
        Assert.Equal(
            string.Empty,
            result.Error.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
    }

    private static KnowledgeTransaction OpenTransaction() => new(
        SampleTransactionId,
        new SnapshotId(41),
        new SnapshotId(42),
        TransactionState.Open,
        ChangeVersion: 7,
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
        ChangeVersion: 8,
        CreatedAtUtc: DateTimeOffset.UnixEpoch,
        CommittedAtUtc: DateTimeOffset.UnixEpoch,
        Purpose: "Import",
        Actor: "Agent",
        Client: "MCP",
        CommitMessage: "Freigabe");
}
