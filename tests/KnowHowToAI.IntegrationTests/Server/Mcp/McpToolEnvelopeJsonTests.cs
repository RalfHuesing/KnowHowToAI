using System.Text.Json;
using System.Text.Json.Serialization;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Vertragstests für den gemeinsamen MCP-Antwort-Envelope: JSON-Feldnamen,
/// Nullability und Feldgrenzen.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpToolEnvelopeJsonTests
{
    private const string SampleTransactionId = "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90";

    private sealed record SampleData([property: JsonPropertyName("transactionId")] string TransactionId);

    [Fact]
    public void ErrorEnvelope_SerializesStableCamelCaseFieldNames()
    {
        var envelope = McpToolEnvelope<SampleData>.Failure(new DomainError(
            "TransactionNotFound",
            "Die angefragte Transaction existiert nicht.",
            new Dictionary<string, string> { ["transactionId"] = SampleTransactionId }));

        var json = JsonSerializer.Serialize(envelope);

        Assert.Equal(
            "{\"code\":\"TransactionNotFound\",\"message\":\"Die angefragte Transaction existiert nicht.\"," +
            "\"details\":{\"transactionId\":\"" + SampleTransactionId + "\"}}",
            json);
    }

    [Fact]
    public void ErrorEnvelope_FieldNames_AreIndependentOfSerializerOptions()
    {
        var envelope = McpToolEnvelope<SampleData>.Failure(
            new DomainError("SnapshotConflict", "Der Snapshot hat sich geändert."));

        var json = JsonSerializer.Serialize(envelope);
        var camelCaseJson = JsonSerializer.Serialize(
            envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(json, camelCaseJson);
        Assert.Contains("\"code\"", json);
        Assert.Contains("\"message\"", json);
    }

    [Fact]
    public void SuccessEnvelope_OmitsNullableFieldsAndKeepsPayloadUnderData()
    {
        var envelope = McpToolEnvelope<SampleData>.Success(new SampleData(SampleTransactionId));

        var json = JsonSerializer.Serialize(envelope);

        Assert.Equal(
            "{\"code\":\"Success\",\"data\":{\"transactionId\":\"" + SampleTransactionId + "\"}}",
            json);
        Assert.DoesNotContain("message", json);
        Assert.DoesNotContain("details", json);
        Assert.DoesNotContain("warnings", json);
    }

    [Fact]
    public void SuccessEnvelope_WithoutPayload_OmitsDataField()
    {
        Assert.Equal("""{"code":"Success"}""", JsonSerializer.Serialize(McpToolEnvelope<object>.Success()));
    }

    [Fact]
    public void SuccessEnvelope_SerializesWarningsWithStableFieldNames()
    {
        var envelope = McpToolEnvelope<SampleData>.Success(
            new SampleData(SampleTransactionId),
            [
                new McpWarning(
                    "NodeTooLarge",
                    "Der normalisierte Content übersteigt die Warnschwelle.",
                    new Dictionary<string, string> { ["actualBytes"] = "5123", ["thresholdBytes"] = "4096" })
            ]);

        var json = JsonSerializer.Serialize(envelope);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(
            new[] { "code", "data", "warnings" },
            root.EnumerateObject().Select(property => property.Name).ToArray());
        var warning = root.GetProperty("warnings")[0];
        Assert.Equal("NodeTooLarge", warning.GetProperty("code").GetString());
        Assert.Equal(
            "Der normalisierte Content übersteigt die Warnschwelle.",
            warning.GetProperty("message").GetString());
        Assert.Equal(
            "5123",
            warning.GetProperty("details").GetProperty("actualBytes").GetString());
        Assert.Equal(
            "4096",
            warning.GetProperty("details").GetProperty("thresholdBytes").GetString());
    }

    [Fact]
    public void EmptyWarningsAndDetails_AreOmittedInBothOutcomes()
    {
        var success = McpToolEnvelope<object>.Success(warnings: []);
        var failure = McpToolEnvelope<object>.Failure(
            new DomainError("TransactionClosed", "Die Transaction ist bereits geschlossen."),
            []);

        Assert.Equal("""{"code":"Success"}""", JsonSerializer.Serialize(success));
        Assert.DoesNotContain("warnings", JsonSerializer.Serialize(failure));
        Assert.DoesNotContain("details", JsonSerializer.Serialize(failure));
    }

    [Fact]
    public void Envelope_AcceptsSuccessWithoutMessageDetailsOrPayload()
    {
        var envelope = new McpToolEnvelope<object>("Success");

        Assert.True(envelope.IsSuccess);
        Assert.Null(envelope.Message);
        Assert.Null(envelope.Details);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public void Envelope_RejectsEmptyCode()
    {
        Assert.ThrowsAny<ArgumentException>(() => new McpToolEnvelope<object>("  "));
    }

    [Fact]
    public void Envelope_RejectsSuccessWithMessageOrDetails()
    {
        Assert.Throws<ArgumentException>(
            () => new McpToolEnvelope<object>("Success", message: "nicht erlaubt"));
        Assert.Throws<ArgumentException>(
            () => new McpToolEnvelope<object>(
                "Success", details: new Dictionary<string, string> { ["key"] = "value" }));
    }

    [Fact]
    public void Envelope_RejectsFailureWithoutMessage()
    {
        Assert.Throws<ArgumentException>(
            () => new McpToolEnvelope<object>("TransactionNotFound"));
    }

    [Fact]
    public void Envelope_RejectsFailureWithPayload()
    {
        Assert.Throws<ArgumentException>(
            () => new McpToolEnvelope<object>(
                "TransactionNotFound", message: "Fehler", payload: new SampleData(SampleTransactionId)));
    }
}
