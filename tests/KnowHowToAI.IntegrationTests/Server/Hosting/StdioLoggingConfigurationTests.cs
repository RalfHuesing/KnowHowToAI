using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using Serilog;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Belegt den Protokollkanal: Ausgaben landen ausschließlich auf stderr (optional in
/// Datei); stdout bleibt vollständig leer, damit es exklusiv dem MCP-Protokoll dient.
/// </summary>
[Trait("Category", "Unit")]
public sealed class StdioLoggingConfigurationTests
{
    [Fact]
    public void StderrConfiguration_WritesLogsToStderrOnly()
    {
        const string password = "correct-horse-battery-staple";
        const string payload = "# Vollständiger Content wird nicht protokolliert";
        var (capturedStdOut, capturedStdErr) = CaptureConsoles();
        try
        {
            var configuration = new LoggerConfiguration();
            LoggingSetup.ApplyTo(configuration, new LoggingOptions { MinimumLevel = "Information" });
            using var logger = configuration.CreateLogger();

            logger.Information("Server gestartet für {UserName} mit {Password}", "alice", password);
            logger.Information("Node {NodeId} mit {ContentMd}", "node-1", payload);

            var stdErr = capturedStdErr.ToString();
            Assert.Contains("Server gestartet", stdErr);
            Assert.Contains("<redacted>", stdErr);
            Assert.DoesNotContain(password, stdErr);
            Assert.DoesNotContain(payload, stdErr);
            Assert.Equal(string.Empty, capturedStdOut.ToString());
        }
        finally
        {
            RestoreConsoles();
        }
    }

    [Fact]
    public void FileConfiguration_WritesToOptionalFileAndKeepsStdOutClean()
    {
        const string password = "correct-horse-battery-staple";
        var logFilePath = Path.Combine(RepoTempDirectory(), $"m61-logging-{Guid.NewGuid():N}.log");
        var (capturedStdOut, capturedStdErr) = CaptureConsoles();
        try
        {
            var configuration = new LoggerConfiguration();
            LoggingSetup.ApplyTo(
                configuration,
                new LoggingOptions { MinimumLevel = "Information", FilePath = logFilePath });
            using (var logger = configuration.CreateLogger())
            {
                logger.Information("Migration abgeschlossen für {Password}", password);
            }

            var stdOut = capturedStdOut.ToString();
            Assert.Equal(string.Empty, stdOut);
            var writtenLogFiles = Directory.GetFiles(RepoTempDirectory(), "m61-logging-*.log");
            var fileContent = Assert.Single(writtenLogFiles.Select(File.ReadAllText));
            Assert.Contains("Migration abgeschlossen", fileContent);
            Assert.Contains("<redacted>", fileContent);
            Assert.DoesNotContain(password, fileContent);
        }
        finally
        {
            RestoreConsoles();
            foreach (var writtenLogFile in Directory.GetFiles(RepoTempDirectory(), "m61-logging-*.log"))
            {
                File.Delete(writtenLogFile);
            }
        }
    }

    private static string RepoTempDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KnowHowToAI.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var tempDirectory = Path.Combine(directory.FullName, "temp");
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private static (StringWriter StdOut, StringWriter StdErr) CaptureConsoles()
    {
        var stdOut = new StringWriter();
        var stdErr = new StringWriter();
        Console.SetOut(stdOut);
        Console.SetError(stdErr);
        return (stdOut, stdErr);
    }

    private static void RestoreConsoles()
    {
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    }
}
