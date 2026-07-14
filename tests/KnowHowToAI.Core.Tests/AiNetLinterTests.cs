using System.Diagnostics;

namespace KnowHowToAI.Core.Tests;

public class AiNetLinterTests
{
    private const string ExeEnvironmentVariable = "AINETLINTER_EXE";
    private const string DefaultExePath = @"C:\Daten\AiNetLinter-win-x64\AiNetLinter.exe";

    [Fact]
    public async Task LintRun_ReportsNoViolations()
    {
        var exePath = Environment.GetEnvironmentVariable(ExeEnvironmentVariable) ?? DefaultExePath;
        Assert.SkipUnless(File.Exists(exePath),
            $"AiNetLinter nicht gefunden unter '{exePath}' (Umgebungsvariable {ExeEnvironmentVariable} optional). Tool ist ein lokales Dev-Zusatzwerkzeug, kein CI-Hard-Requirement.");

        var solutionRoot = FindSolutionRoot();
        var configPath = Path.Combine(solutionRoot, "tests", "KnowHowToAI.Core.Tests", "AiNetLinter", "rules", "KnowHowToAI.rules.json");
        var outputDir = Path.Combine(solutionRoot, "tests", "KnowHowToAI.Core.Tests", "AiNetLinter", "output");
        Directory.CreateDirectory(outputDir);
        var reportPath = Path.Combine(outputDir, "lint-report.md");

        var startInfo = new ProcessStartInfo(exePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add(configPath);
        startInfo.ArgumentList.Add("--path");
        startInfo.ArgumentList.Add(Path.Combine(solutionRoot, "KnowHowToAI.slnx"));
        startInfo.ArgumentList.Add("--sync-cursor-rules");
        startInfo.ArgumentList.Add("--cursor-rules-path");
        startInfo.ArgumentList.Add(Path.Combine(solutionRoot, ".agents", "rules"));

        var cancellationToken = TestContext.Current.CancellationToken;
        using var process = Process.Start(startInfo)!;
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        await File.WriteAllTextAsync(reportPath, stdout, cancellationToken);

        Assert.True(process.ExitCode == 0,
            $"AiNetLinter meldet Verstöße (Exit {process.ExitCode}). Report: {reportPath}\n{stdout}\n{stderr}");
    }

    // Kein --baseline: Projekt ist aktuell verstoßfrei, es gibt nichts einzufrieren.
    // Baseline nachziehen, sobald der erste dokumentierte Altlast-Verstoß entsteht.
    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KnowHowToAI.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("KnowHowToAI.slnx nicht gefunden — Solution-Root konnte nicht ermittelt werden.");
    }
}
