using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Domain.Common;

[Trait("Category", "Unit")]
public sealed class ResultContractTests
{
    [Fact]
    public void Success_ExposesStableSuccessCodeValueAndCopiedWarnings()
    {
        var sourceWarnings = new List<DomainWarning>
        {
            new("NodeTooLarge", "Der Inhalt überschreitet die Warnschwelle.")
        };

        var result = Result<string>.Success("gespeichert", sourceWarnings);
        sourceWarnings.Clear();

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultCodes.Success, result.Code);
        Assert.Equal("gespeichert", result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.Message);
        Assert.Empty(result.Details);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Failure_ExposesErrorCodeMachineReadableDetailsAndWarnings()
    {
        var sourceDetails = new Dictionary<string, string>
        {
            ["transactionId"] = "9f4a2c43-0a77-44be-8f98-f403444d3e9f"
        };
        var error = new DomainError("TransactionNotOpen", "Die Transaction ist nicht offen.", sourceDetails);
        var warning = new DomainWarning("NodeTooLarge", "Der Inhalt bleibt speicherbar.");

        var result = Result<string>.Failure(error, [warning]);
        sourceDetails["transactionId"] = "verändert";

        Assert.False(result.IsSuccess);
        Assert.Equal("TransactionNotOpen", result.Code);
        Assert.Equal("Die Transaction ist nicht offen.", result.Message);
        Assert.Equal("9f4a2c43-0a77-44be-8f98-f403444d3e9f", result.Details["transactionId"]);
        Assert.Same(error, result.Error);
        Assert.Null(result.Value);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void DomainWarning_CopiesDetailsForAnImmutableContract()
    {
        var sourceDetails = new Dictionary<string, string>
        {
            ["actualBytes"] = "8192"
        };

        var warning = new DomainWarning("NodeTooLarge", "Der Inhalt ist groß.", sourceDetails);
        sourceDetails["actualBytes"] = "1";

        Assert.Equal("8192", warning.Details["actualBytes"]);
    }
}
