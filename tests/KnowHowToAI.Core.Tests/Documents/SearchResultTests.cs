using KnowHowToAI.Core.Documents;

namespace KnowHowToAI.Core.Tests.Documents;

public class SearchResultTests
{
    [Theory]
    [InlineData(2, 5, true)]
    [InlineData(3, 3, false)]
    [InlineData(0, 0, false)]
    public void SearchResult_TruncatedReflectsTotalCountVersusResultsCount(int resultsCount, int totalCount, bool expectedTruncated)
    {
        IReadOnlyList<DocumentSummary> results = Enumerable.Range(0, resultsCount)
            .Select(i => new DocumentSummary($"slug-{i}", $"Title {i}"))
            .ToList();

        var result = new SearchResult(results, Truncated: totalCount > results.Count);

        Assert.Equal(expectedTruncated, result.Truncated);
    }

    [Fact]
    public void SearchResult_PositionalRecord_BoolPropertySupportsValueEquality()
    {
        IReadOnlyList<DocumentSummary> results = [new DocumentSummary("a", "A")];
        var first = new SearchResult(results, Truncated: false);
        var second = new SearchResult(results, Truncated: false);

        Assert.Equal(first, second);
    }
}
