using KnowHowToAI.Server.Hosting;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Verhaltenstests für die Pfadauflösung der Protokollierungsdateien.
/// </summary>
[Trait("Category", "Unit")]
public sealed class LoggingSetupTests
{
    [Fact]
    public void ResolveLogFilePath_RelativePath_ResolvesRelativeToBaseDirectory()
    {
        var resolved = LoggingSetup.ResolveLogFilePath("logs/server.log");

        var expected = Path.Combine(AppContext.BaseDirectory, "logs/server.log");
        Assert.Equal(expected, resolved);
    }

    [Fact]
    public void ResolveLogFilePath_AbsolutePath_ReturnsUnchanged()
    {
        var absolutePath = Path.Combine(Path.GetTempPath(), "logs", "server.log");

        var resolved = LoggingSetup.ResolveLogFilePath(absolutePath);

        Assert.Equal(absolutePath, resolved);
    }
}
