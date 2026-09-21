namespace KnowHowToAI.TestSupport;

/// <summary>
/// Prüft und dedupliziert Datenbankziele, bevor ein Browser-Test-Cleanup beginnen darf.
/// Credentials sind bewusst kein Bestandteil des Vergleichs und werden nie ausgegeben.
/// </summary>
public readonly record struct SqlCleanupTarget(string Server, string Database)
{
    internal string NormalizedIdentity =>
        $"{Normalize(Server)}\u001f{Normalize(Database)}";

    private static string Normalize(string value)
    {
        var normalized = value.Trim();
        if (normalized.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[4..];

        return normalized.TrimEnd('.').ToUpperInvariant();
    }
}

/// <summary>
/// Erzwingt die Sicherheitsgrenzen für alle testinfrastrukturellen SQL-Cleanups.
/// </summary>
public static class SqlCleanupTargetGuard
{
    private static readonly HashSet<string> SystemDatabases = new(StringComparer.OrdinalIgnoreCase)
    {
        "master",
        "model",
        "msdb",
        "tempdb"
    };

    public static IReadOnlyList<SqlCleanupTarget> ValidateAndDedupe(
        SqlCleanupTarget productTarget,
        IEnumerable<SqlCleanupTarget> browserTargets)
    {
        ArgumentNullException.ThrowIfNull(browserTargets);

        var result = new List<SqlCleanupTarget>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in browserTargets)
        {
            var database = target.Database.Trim();
            if (SystemDatabases.Contains(database))
            {
                throw new InvalidOperationException(
                    $"SQL-Cleanup verweigert: Systemdatenbank '{target.Database}' am Server '{target.Server}'.");
            }

            if (string.Equals(target.NormalizedIdentity, productTarget.NormalizedIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"SQL-Cleanup verweigert: Browser-Ziel '{target.Server}/{target.Database}' entspricht der Produktverbindung.");
            }

            if (seen.Add(target.NormalizedIdentity))
                result.Add(target);
        }

        return result;
    }
}
