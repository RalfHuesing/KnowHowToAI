using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Mapping;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Vertragstests für die Abbildung des Application-Result-Vertrags auf den
/// MCP-Antwort-Envelope: stabile Fehlercodes, Details und Warnungen.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpResultMapperTests
{
    private sealed record SamplePayload(string SnapshotId);

    [Fact]
    public void DomainError_IsMappedToFailureEnvelopeWithCodeMessageAndDetails()
    {
        var result = Result<SamplePayload>.Failure(new DomainError(
            "SnapshotConflict",
            "Der Base Snapshot ist nicht mehr aktuell.",
            new Dictionary<string, string> { ["snapshotId"] = "17" }));

        var envelope = McpResultMapper.ToEnvelope(result);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("SnapshotConflict", envelope.Code);
        Assert.Equal("Der Base Snapshot ist nicht mehr aktuell.", envelope.Message);
        Assert.Equal("17", envelope.Details!["snapshotId"]);
        Assert.Null(envelope.Data);
        Assert.Null(envelope.Warnings);
    }

    [Theory]
    [InlineData("InvalidReadContext")]
    [InlineData("TransactionNotFound")]
    [InlineData("TransactionClosed")]
    [InlineData("SnapshotNotFound")]
    [InlineData("SnapshotConflict")]
    [InlineData("InvalidCursor")]
    [InlineData("CursorExpired")]
    [InlineData("NodeNotFound")]
    [InlineData("RoleNotFound")]
    [InlineData("HeadingNotAllowed")]
    [InlineData("TextNotFound")]
    [InlineData("MultipleTextMatches")]
    [InlineData("InvalidDependency")]
    [InlineData("ReleaseNotFound")]
    public void DomainErrorCodes_AreMappedVerbatimOntoTheStableCatalog(string errorCode)
    {
        var envelope = McpResultMapper.ToEnvelope<SamplePayload>(
            Result<SamplePayload>.Failure(new DomainError(errorCode, "Fachfehler.")));

        Assert.Equal(errorCode, envelope.Code);
        Assert.Contains(errorCode, McpErrorCatalog.KnownErrorCodes);
    }

    [Fact]
    public void SuccessResult_IsMappedToSuccessEnvelopeKeepingPayloadAndWarnings()
    {
        var payload = new SamplePayload("17");
        var result = Result<SamplePayload>.Success(
            payload,
            [new DomainWarning("NodeTooLarge", "Content übersteigt die Warnschwelle.")]);

        var envelope = McpResultMapper.ToEnvelope(result);

        Assert.True(envelope.IsSuccess);
        Assert.Equal("Success", envelope.Code);
        Assert.Same(payload, envelope.Data);
        var warning = Assert.Single(envelope.Warnings!);
        Assert.Equal("NodeTooLarge", warning.Code);
        Assert.Equal("Content übersteigt die Warnschwelle.", warning.Message);
    }

    [Fact]
    public void DomainWarningDetails_AreMappedVerbatim()
    {
        var warning = McpResultMapper.ToWarning(new DomainWarning(
            "TooManyChildren",
            "Zu viele Child-Nodes.",
            new Dictionary<string, string> { ["actualCount"] = "80", ["thresholdCount"] = "50" }));

        Assert.Equal("TooManyChildren", warning.Code);
        Assert.Equal("80", warning.Details!["actualCount"]);
        Assert.Equal("50", warning.Details!["thresholdCount"]);
    }

    [Fact]
    public void SuccessWithoutValue_MapsToSuccessEnvelopeWithoutData()
    {
        var envelope = McpResultMapper.ToEnvelope(Result<object>.Success(null));

        Assert.True(envelope.IsSuccess);
        Assert.Null(envelope.Data);
    }
}
