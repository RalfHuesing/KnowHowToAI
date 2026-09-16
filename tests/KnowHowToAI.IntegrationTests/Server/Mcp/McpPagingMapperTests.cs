using KnowHowToAI.Server.Mcp.Mapping;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Vertragstests für den gemeinsamen limit-Feldvertrag der Listen-, Search- und
/// Diff-Tools: Default bei fehlendem oder nicht positivem Wert, Klemmen am Maximum.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpPagingMapperTests
{
    [Theory]
    [InlineData(null, 50, 200, 50)]
    [InlineData(0, 50, 200, 50)]
    [InlineData(-3, 50, 200, 50)]
    [InlineData(1, 50, 200, 1)]
    [InlineData(120, 50, 200, 120)]
    [InlineData(500, 50, 200, 200)]
    public void NormalizeLimit_AppliesDefaultAndMaximumContract(
        int? limit, int defaultPageSize, int maximumPageSize, int expectedLimit)
    {
        var normalized = McpPagingMapper.NormalizeLimit(limit, defaultPageSize, maximumPageSize);

        Assert.Equal(expectedLimit, normalized);
    }
}
