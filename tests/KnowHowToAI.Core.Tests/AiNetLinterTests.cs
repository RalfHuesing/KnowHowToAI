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
        var solutionPath = Path.Combine(solutionRoot, "KnowHowToAI.slnx");
        var outputDir = Path.Combine(solutionRoot, "tests", "KnowHowToAI.Core.Tests", "AiNetLinter", "output");
        Directory.CreateDirectory(outputDir);
        var reportPath = Path.Combine(outputDir, "lint-report.md");
        var cancellationToken = TestContext.Current.CancellationToken;

        // AiNetLinter-Bug (Program.cs "Schneller Pfad"): --sync-cursor-rules OHNE --playbook in
        // derselben Ausführung wie ein Lint-Lauf überspringt den eigentlichen Audit (AuditCommand)
        // komplett und liefert immer Exit 0, egal wie viele Verstöße vorliegen — nur der Sync läuft.
        // Deshalb zwingend zwei getrennte Prozessaufrufe, sonst bleiben echte Verstöße unbemerkt.
        var (lintExitCode, lintStdout, lintStderr) = await RunAiNetLinterAsync(
            exePath, ["--config", configPath, "--path", solutionPath], cancellationToken);
        await File.WriteAllTextAsync(reportPath, lintStdout, cancellationToken);

        Assert.True(lintExitCode == 0,
            $"AiNetLinter meldet Verstöße (Exit {lintExitCode}). Report: {reportPath}\n{lintStdout}\n{lintStderr}");

        var (syncExitCode, syncStdout, syncStderr) = await RunAiNetLinterAsync(
            exePath,
            ["--config", configPath, "--path", solutionPath, "--sync-cursor-rules", "--cursor-rules-path", Path.Combine(solutionRoot, ".agents", "rules")],
            cancellationToken);

        Assert.True(syncExitCode == 0,
            $"AiNetLinter konnte .agents/rules/AiNetLinter.mdc nicht synchronisieren (Exit {syncExitCode}).\n{syncStdout}\n{syncStderr}");
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunAiNetLinterAsync(
        string exePath, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(exePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, stdout, stderr);
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
