using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Logging;

namespace KnowHowToAI.Core.Tests;

public class ResponseSizeTests
{
    [Fact]
    public void Measure_DocumentSummaryListWithThreeItems_ReturnsThree()
    {
        IReadOnlyList<DocumentSummary> summaries =
        [
            new("it", "IT"),
            new("it/netzwerk", "Netzwerk"),
            new("it/netzwerk/routing", "Routing"),
        ];

        Assert.Equal(3, ResponseSize.Measure(summaries));
    }

    [Fact]
    public void Measure_EmptyDocumentSummaryList_ReturnsZero()
    {
        IReadOnlyList<DocumentSummary> summaries = [];

        Assert.Equal(0, ResponseSize.Measure(summaries));
    }

    [Fact]
    public void Measure_DocumentDetailWithContent_ReturnsContentLength()
    {
        var detail = new DocumentDetail("Routing", "12345");

        Assert.Equal(5, ResponseSize.Measure(detail));
    }

    [Fact]
    public void Measure_DocumentDetailWithEmptyContent_ReturnsZero()
    {
        var detail = new DocumentDetail("Leer", "");

        Assert.Equal(0, ResponseSize.Measure(detail));
    }

    [Fact]
    public void Measure_NullDetail_ReturnsZero()
    {
        Assert.Equal(0, ResponseSize.Measure<DocumentDetail?>(null));
    }

    [Fact]
    public void Measure_UnknownType_ReturnsZero()
    {
        Assert.Equal(0, ResponseSize.Measure("irgendein Text"));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    public void Measure_SearchResult_ReturnsResultsCount(int resultsCount)
    {
        IReadOnlyList<DocumentSummary> results = Enumerable.Range(0, resultsCount)
            .Select(i => new DocumentSummary($"slug-{i}", $"Title {i}"))
            .ToList();
        var result = new SearchResult(results, Truncated: false);

        Assert.Equal(resultsCount, ResponseSize.Measure(result));
    }
}
