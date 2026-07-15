using System.Text.RegularExpressions;

namespace KnowHowToAI.Core.Sync;

// Der Tabellenname aus KnowHowToAiOptions.DocumentsTableName wird direkt in SQL-Strings
// interpoliert (Tabellennamen lassen sich nicht als Parameter binden) — diese Prüfung ist
// deshalb die einzige Absicherung gegen SQL-Injection über einen manipulierten Konfigurationswert.
public static class SqlIdentifierValidator
{
    private static readonly Regex Pattern = new("^[A-Za-z_][A-Za-z0-9_]{0,99}$", RegexOptions.Compiled);

    public static void EnsureValid(string tableName)
    {
        if (!Pattern.IsMatch(tableName))
        {
            throw new ArgumentException(
                $"'{tableName}' ist kein gültiger Tabellenname (erlaubt: Buchstaben, Ziffern, Unterstrich, max. 100 Zeichen, darf nicht mit einer Ziffer beginnen).",
                nameof(tableName));
        }
    }
}
