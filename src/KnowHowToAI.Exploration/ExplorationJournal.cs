namespace KnowHowToAI.Exploration;

public enum FindingSeverity
{
    Info = 0,
    Improvement = 1,
    Bug = 2
}

/// <summary>Ein dokumentierter Befund aus einem Szenario-Lauf.</summary>
public sealed record ExplorationFinding(FindingSeverity Severity, string Area, string Scenario, string Message);

/// <summary>
/// Sammelt Befunde eines Bereichs-Laufs. Bugs führen zu Exitcode 1, damit der
/// Agententreiber (oder die CI) Fehlschläge maschinell erkennt.
/// </summary>
public sealed class ExplorationJournal(string area)
{
    private readonly List<ExplorationFinding> _findings = [];

    public bool HasBugs => _findings.Any(finding => finding.Severity == FindingSeverity.Bug);

    public void Info(string scenario, string message) => Add(FindingSeverity.Info, scenario, message);

    public void Improvement(string scenario, string message) => Add(FindingSeverity.Improvement, scenario, message);

    public void Bug(string scenario, string message) => Add(FindingSeverity.Bug, scenario, message);

    public void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine($"─── Befunde ({area}) ───");
        if (_findings.Count == 0)
        {
            Console.WriteLine("Keine Befunde.");
            return;
        }

        foreach (var finding in _findings)
            Console.WriteLine($"[{finding.Severity}] {finding.Scenario}: {finding.Message}");

        var bugs = _findings.Count(finding => finding.Severity == FindingSeverity.Bug);
        var improvements = _findings.Count(finding => finding.Severity == FindingSeverity.Improvement);
        var infos = _findings.Count(finding => finding.Severity == FindingSeverity.Info);
        Console.WriteLine($"{_findings.Count} Befunde: {bugs} Bug(s), {improvements} Verbesserung(en), {infos} Info(s)");
    }

    private void Add(FindingSeverity severity, string scenario, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _findings.Add(new ExplorationFinding(severity, area, scenario, message));
    }
}
