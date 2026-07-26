using System.Text.RegularExpressions;

namespace KnowHowToAI.Core.Sync;

// Der Tabellenname aus KnowHowToAiOptions.DocumentsTableName wird direkt in SQL-Strings
// interpoliert (Tabellennamen lassen sich nicht als Parameter binden) — diese Prüfung ist
// deshalb die einzige Absicherung gegen SQL-Injection über einen manipulierten Konfigurationswert.
//
// Wir erzwingen lowercase-only, damit der Identifier plattform-konsistent ist (Windows-Default-
// Collation ist case-insensitive, Linux-Default kann case-sensitive sein) und konsistent mit
// SlugRules.
public static class SqlIdentifierValidator
{
    private static readonly Regex Pattern = new("^[a-z_][a-z0-9_]{0,99}$", RegexOptions.Compiled);

    // Häufigste SQL Server Reserved Words, die nicht als Tabellenname funktionieren.
    // Vollständige Liste siehe https://learn.microsoft.com/en-us/sql/t-sql/language-elements/reserved-words
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "user", "table", "select", "from", "where", "insert", "update", "delete", "create",
        "drop", "alter", "index", "view", "database", "schema", "primary", "foreign",
        "key", "order", "group", "having", "union", "join", "into", "values", "default",
        "null", "check", "constraint", "trigger", "procedure", "function", "cursor",
    };

    public static void EnsureValid(string tableName)
    {
        if (!Pattern.IsMatch(tableName))
        {
            throw new ArgumentException(
                $"'{tableName}' ist kein gültiger Tabellenname (erlaubt: lowercase a-z, Ziffern, Unterstrich, max. 100 Zeichen, muss mit Buchstabe oder Unterstrich beginnen).",
                nameof(tableName));
        }

        if (ReservedWords.Contains(tableName))
        {
            throw new ArgumentException(
                $"'{tableName}' ist ein SQL Server Reserved Word und kann nicht als Tabellenname verwendet werden.",
                nameof(tableName));
        }
    }
}
