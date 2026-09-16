using KnowHowToAI.Server.Configuration;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Verhaltenstests für die Protokollierungs-Konfiguration (Kanalwahl und Level).
/// </summary>
[Trait("Category", "Unit")]
public sealed class LoggingOptionsValidatorTests
{
    private static readonly LoggingOptionsValidator Sut = new();

    [Fact]
    public void ValidConfiguration_WithStderrOnly_Succeeds()
    {
        var result = Sut.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidConfiguration_WithOptionalFile_Succeeds()
    {
        var result = Sut.Validate(null, ValidOptions() with { FilePath = "logs/server.log" });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("verbose")]
    [InlineData("Debug")]
    [InlineData("Information")]
    [InlineData("warning")]
    [InlineData("Error")]
    [InlineData("FATAL")]
    public void KnownMinimumLevels_AcceptCaseInsensitive(string minimumLevel)
    {
        var result = Sut.Validate(null, ValidOptions() with { MinimumLevel = minimumLevel });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void UnknownMinimumLevel_FailsWithConfiguredValue()
    {
        var result = Sut.Validate(null, ValidOptions() with { MinimumLevel = "Chatty" });

        Assert.False(result.Succeeded);
        var failures = string.Join(" ", result.Failures ?? []);
        Assert.Contains("MinimumLevel", failures);
        Assert.Contains("Chatty", failures);
    }

    [Fact]
    public void BlankFilePath_FailsWithClearHint()
    {
        var result = Sut.Validate(null, ValidOptions() with { FilePath = "   " });

        Assert.False(result.Succeeded);
        var failures = string.Join(" ", result.Failures ?? []);
        Assert.Contains("FilePath", failures);
    }

    private static LoggingOptions ValidOptions() => new()
    {
        MinimumLevel = "Information",
        FilePath = null
    };
}
