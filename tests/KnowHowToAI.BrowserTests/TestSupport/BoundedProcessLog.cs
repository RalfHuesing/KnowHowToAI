using System.Text.RegularExpressions;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Sammelt die Konsolenausgabe eines Serverprozesses begrenzt und redigiert:
/// Es bleiben nur die letzten Zeilen im Speicher, jede Zeile wird gekappt und
/// potenziell geheime Werte werden ersetzt. Die Sammlung dient ausschließlich
/// der Diagnose fehlgeschlagener Testläufe, nicht als Verhaltensassertion.
/// </summary>
public sealed partial class BoundedProcessLog
{
    private const int MaxLines = 100;
    private const int MaxLineLength = 200;

    private readonly object _lock = new();
    private readonly Queue<string> _lines = new();
    private int _discardedLineCount;

    [GeneratedRegex(@"(?i)\b(password|passwort|pwd|secret|token)\b\s*[:=]\s*\S+")]
    private static partial Regex SecretPattern();

    public void Append(string line)
    {
        var redacted = SecretPattern().Replace(line, "$1=<redacted>");
        if (redacted.Length > MaxLineLength)
            redacted = string.Concat(redacted.AsSpan(0, MaxLineLength), "…");

        lock (_lock)
        {
            if (_lines.Count == MaxLines)
            {
                _ = _lines.Dequeue();
                _discardedLineCount++;
            }

            _lines.Enqueue(redacted);
        }
    }

    /// <summary>
    /// Letzte aufgezeichnete Zeilen; verworfene ältere Zeilen sind nur als
    /// Anzahl zusammengefasst, damit Diagnosen begrenzt bleiben.
    /// </summary>
    public string[] Snapshot()
    {
        lock (_lock)
        {
            var snapshot = new List<string>(_lines.Count + 1);
            if (_discardedLineCount > 0)
                snapshot.Add($"<{_discardedLineCount} ältere Zeilen verworfen>");
            snapshot.AddRange(_lines);
            return [.. snapshot];
        }
    }
}
