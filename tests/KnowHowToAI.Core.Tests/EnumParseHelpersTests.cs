using KnowHowToAI.Core.Configuration;
using Serilog;
using Serilog.Events;

namespace KnowHowToAI.Core.Tests;

public class EnumParseHelpersTests
{
    [Theory]
    [InlineData("Information")]
    [InlineData("information")]
    [InlineData("INFORMATION")]
    public void Parse_LogEventLevel_AcceptsCaseInsensitiveInput(string value)
    {
        Assert.Equal(LogEventLevel.Information, EnumParseHelpers.Parse<LogEventLevel>(value));
    }

    [Theory]
    [InlineData("Day")]
    [InlineData("day")]
    [InlineData("Hour")]
    [InlineData("hour")]
    public void Parse_RollingInterval_AcceptsCaseInsensitiveInput(string value)
    {
        Assert.Equal(Enum.Parse<RollingInterval>(value, ignoreCase: true), EnumParseHelpers.Parse<RollingInterval>(value));
    }

    [Fact]
    public void Parse_LogEventLevel_RejectsInvalidValue_ThrowsWithAllowedValuesList()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EnumParseHelpers.Parse<LogEventLevel>("foo"));
        Assert.Contains("Information", ex.Message);
    }

    [Fact]
    public void Parse_RollingInterval_RejectsInvalidValue_ThrowsWithAllowedValuesList()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EnumParseHelpers.Parse<RollingInterval>("yearly"));
        Assert.Contains("Day", ex.Message);
        Assert.Contains("Hour", ex.Message);
    }

    [Fact]
    public void Parse_EmptyString_ThrowsWithAllowedValuesList()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EnumParseHelpers.Parse<LogEventLevel>(""));
        Assert.Contains("LogEventLevel", ex.Message);
        Assert.Contains("Information", ex.Message);
    }
}
