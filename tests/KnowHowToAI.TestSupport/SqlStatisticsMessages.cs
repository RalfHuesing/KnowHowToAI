using System.Globalization;
using System.Text.RegularExpressions;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Parst die InfoMessage-Ausgaben von <c>SET STATISTICS IO/TIME</c>. Die
/// Meldungen des SQL Servers sind sprachabhängig (lokale Testdatenbanken
/// antworten auf Deutsch, CI auf Englisch); die Muster decken beide Sprachen ab
/// und nehmen immer den letzten Treffer (die Gesamtausführungszeit).
/// </summary>
public static partial class SqlStatisticsMessages
{
    /// <summary>
    /// Logische Lesevorgänge je Tabelle über alle Meldungen aufsummiert;
    /// Total ist die Summe über alle Tabellen.
    /// </summary>
    public static (int Total, Dictionary<string, int> ByTable) ParseLogicalReads(string messageText)
    {
        var byTable = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in LogicalReadsRegex().Matches(messageText))
        {
            var tableName = match.Groups[1].Value;
            var reads = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            byTable[tableName] = byTable.GetValueOrDefault(tableName) + reads;
        }

        return (byTable.Values.Sum(), byTable);
    }

    /// <summary>CPU- und verstrichene Zeit der letzten gemeldeten Ausführung in ms.</summary>
    public static (long CpuMilliseconds, long ElapsedMilliseconds) ParseExecutionTimes(string messageText)
    {
        Match? lastMatch = null;
        foreach (Match match in ExecutionTimesRegex().Matches(messageText))
            lastMatch = match;

        return lastMatch is null
            ? (0, 0)
            : (long.Parse(lastMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                long.Parse(lastMatch.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    [GeneratedRegex(@"(?:Table|Tabelle): ""([^""]+)""\. (?:Scan count|Anzahl von [ÜU]berpr[üu]fungen): \d+, (?:logical reads|logische Lesevorg[aä]nge): (\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex LogicalReadsRegex();

    [GeneratedRegex(@"(?:SQL Server Execution Times|SQL Server-Ausf[üu]hrungszeiten):\s*CPU[- ](?:time|Zeit) = (\d+) ms,\s*(?:elapsed time|verstrichene Zeit) = (\d+) ms", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex ExecutionTimesRegex();
}
