using System.Text.Json;
using System.Text.Json.Serialization;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Mapping;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Vertragstests für die gemeinsamen Selektor-Felder der Read-Tools:
/// gegenseitiger Ausschluss von transactionId/snapshotId, ID-Round-Trip
/// ohne Umformatierung und Zuordnung zum Read-Kontext.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpReadContextMapperTests
{
    private static readonly TransactionId SampleTransactionId =
        new(Guid.Parse("0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90"));

    private sealed record TransactionPayload([property: JsonPropertyName("transactionId")] string TransactionId);

    [Fact]
    public void TransactionAndSnapshotSelectorTogether_AreRejectedAsInvalidReadContext()
    {
        var request = new McpReadContextRequest(
            TransactionId: SampleTransactionId.ToString(),
            SnapshotId: "17");

        var result = McpReadContextMapper.ToApplicationContext(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
        Assert.Equal(SampleTransactionId.ToString(), result.Details["transactionId"]);
        Assert.Equal("17", result.Details["snapshotId"]);
    }

    [Fact]
    public void TransactionSelector_MapsToWorkingReadContext()
    {
        var result = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(TransactionId: SampleTransactionId.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Equal(SampleTransactionId, result.Value!.TransactionId);
        Assert.Null(result.Value.SnapshotId);
        Assert.False(result.Value.IncludeDeleted);
    }

    [Fact]
    public void SnapshotSelector_MapsToHistoricalReadContext()
    {
        var result = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(SnapshotId: "17", IncludeDeleted: true));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.TransactionId);
        Assert.Equal(new SnapshotId(17), result.Value.SnapshotId);
        Assert.True(result.Value.IncludeDeleted);
    }

    [Fact]
    public void WithoutSelectors_MapsToCurrentReadContext()
    {
        var result = McpReadContextMapper.ToApplicationContext(new McpReadContextRequest());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.TransactionId);
        Assert.Null(result.Value.SnapshotId);
        Assert.False(result.Value.IncludeDeleted);
    }

    [Theory]
    [InlineData("not-a-guid", null)]
    [InlineData("", null)]
    [InlineData(null, "abc")]
    [InlineData(null, "0")]
    [InlineData(null, "-7")]
    [InlineData(null, "")]
    public void InvalidSelectorValue_IsRejectedAsInvalidReadContextWithRawValueDetail(
        string? transactionId, string? snapshotId)
    {
        var result = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(TransactionId: transactionId, SnapshotId: snapshotId));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, result.Error!.Code);
        Assert.Contains(result.Details, detail =>
            detail.Key is "transactionId" or "snapshotId" && detail.Value == (transactionId ?? snapshotId));
    }

    [Fact]
    public void TransactionIdFromResponse_IsUsableAsFollowUpParameterWithoutReformatting()
    {
        var responseJson = JsonSerializer.Serialize(McpToolEnvelope<TransactionPayload>.Success(
            new TransactionPayload(SampleTransactionId.ToString())));

        var rawId = JsonDocument.Parse(responseJson).RootElement
            .GetProperty("data").GetProperty("transactionId").GetString();
        var result = McpReadContextMapper.ToApplicationContext(new McpReadContextRequest(TransactionId: rawId));

        Assert.True(result.IsSuccess);
        Assert.Equal(SampleTransactionId, result.Value!.TransactionId);
    }

    [Fact]
    public void SnapshotIdFromResponse_IsUsableAsFollowUpParameterWithoutReformatting()
    {
        var snapshotId = new SnapshotId(4242).ToString();

        var result = McpReadContextMapper.ToApplicationContext(new McpReadContextRequest(SnapshotId: snapshotId));

        Assert.True(result.IsSuccess);
        Assert.Equal(snapshotId, result.Value!.SnapshotId!.Value.ToString());
    }
}
