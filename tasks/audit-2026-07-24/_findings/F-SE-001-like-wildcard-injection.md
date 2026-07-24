# F-SE-001 — LIKE-Wildcard-Injection in `BuildLikePattern`

> **Schweregrad:** High
> **Dimension:** 2 — Sicherheit
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:79-94`
> **Querverweise:** F-PE-002 (kein `TOP`-Cap), F-MC-001 (Tool-Description erwähnt Wildcard-Semantik nicht)

## Problem

`SqlDocumentsStore.SearchDocsAsync` baut aus dem LLM-Argument `query` ein
SQL-LIKE-Pattern via `BuildLikePattern` (`$"%{query}%"`). Der LLM-kontrollierte
`query` wird *unverändert* in das Pattern interpoliert. `%` und `_` sind in SQL
LIKE Wildcards.

## Vektoren

### Vektor 1 — Wildcard-Smuggling (Mittel)
LLM (oder kompromittierter Client) schickt:
- `query = "%"` → Pattern `%%%` → matched jede Zeile mit mindestens einem Zeichen
- `query = "_"` → Pattern `%_%` → matched jede Zeile mit mindestens einem Zeichen
- `query = "____"` → Pattern `%____%` → matched jede Zeile mit ≥ 4 Zeichen

**Wirkung:** Token-Budget-Sprengung (siehe F-PE-002), da bei großen Tabellen
tausende Treffer zurückkommen.

### Vektor 2 — DoS via Pattern-Länge (Hoch)
LLM schickt `query` mit z.B. 1.000.000 Zeichen. Pattern wird
`%<1.000.000 Zeichen>%`. SQL-Server scannt jede Zeile, vergleicht jede der
4 Spalten mit dem 1-MB-Pattern. Bei einer Tabelle mit 10.000 Zeilen sind das
40.000 Pattern-Matches à ~1 ms = ~40 Sekunden blockierter MCP-Thread.

**Wiederholt:** trivialer DoS gegen den lokalen SQL-Server.

### Vektor 3 — Plan-Compiler-Bombe (Mittel-Hoch, versionabhängig)
LLM schickt `query` mit alternierenden Wildcard-Gruppen. Einige SQL-Server-
Versionen (z.B. mit bestimmten Cost-Based-Optimizer-Heuristiken) brauchen
sehr lange, um den Plan für komplexe LIKE-Patterns zu erstellen.

**Reproduzierbar?** Nicht ohne SQL-Server-Setup. Aber: dokumentiert in SQL-Server-
Bug-Reports als "Query-Optimizer-Timeout" auf bestimmten Pattern-Klassen.

## Aktuelle Mitigations

- `LIKE @Pattern` benutzt SQL-Parameter → keine klassische SQL-Injection
- `BuildLikePattern` ist die *einzige* Wand zwischen LLM und SQL-String

Aber: `BuildLikePattern` *bewusst* nutzt Wildcard-Bedeutung. Es *erlaubt*
Substring-Matching, das ist der Sinn. Daher ist die Wand löchrig.

## Fix

### Variante (empfohlen) — Wildcards escapen + Längen-Cap

```csharp
// Neue Konstante in KnowHowToAiOptions.Search (neu), Default z.B. 200
// Plus Konstante in SqlDocumentsStore für das SQL-Escape-Pattern

private const int MaxQueryLength = 200;

public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(string query, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return [];  // Empty query = empty result, nicht "match-all"
    }

    if (query.Length > MaxQueryLength)
    {
        throw new ArgumentException(
            $"search_docs query ist {query.Length} Zeichen lang, max {MaxQueryLength}.",
            nameof(query));
    }

    // ... rest wie gehabt, mit escaped Pattern
    var pattern = BuildLikePattern(query);
    // ... ExecuteAsync
}

private static string BuildLikePattern(string query)
{
    // LIKE-Escape: % -> [%], _ -> [_], [ -> [[], \ -> [\]
    // Da Escape-Char nicht explizit gesetzt, ist das Standard LIKE-Escape (Bracket-Form)
    var escaped = query
        .Replace("[", "[[]")  // erst [ escapen
        .Replace("%", "[%]")
        .Replace("_", "[_]");
    return $"%{escaped}%";
}
```

### Variante (alternativ) — Längen-Cap + klarer Fehler

Falls Wildcard-Suche gewünscht bleibt (z.B. "Lass mich `%` als Joker benutzen"):

```csharp
public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(string query, CancellationToken cancellationToken)
{
    if (query.Length > MaxQueryLength) { throw ... }
    // kein Escape, aber Längen-Cap
}
```

**Nicht empfohlen** — Variante 1 (Escape) ist die saubere Lösung, weil sie
explizit semantisch klar macht, was `query` bedeutet (Substring, nicht Wildcard).

## Tests

```csharp
public class SqlDocumentsStorePatternTests
{
    [Theory]
    [InlineData("foo", "%foo%")]
    [InlineData("50%", "%50[%]%")]
    [InlineData("a_b", "%a[_]b%")]
    [InlineData("[abc]", "%[[]abc]%")]
    public void BuildLikePattern_EscapesWildcards(string input, string expected)
    {
        Assert.Equal(expected, InvokeBuildLikePattern(input));
    }

    [Fact]
    public void BuildLikePattern_EmptyString_ReturnsEmptyPattern()
    {
        // oder throw, je nach Variante
    }

    [Fact]
    public async Task SearchDocsAsync_TooLongQuery_ThrowsArgumentException()
    {
        var longQuery = new string('a', 201);
        await Assert.ThrowsAsync<ArgumentException>(
            () => new SqlDocumentsStore("...", "documents").SearchDocsAsync(longQuery, default));
    }
}
```

`InvokeBuildLikePattern` ist via Reflection auf die `private static` Methode, oder
die Methode wird via `internal` + `InternalsVisibleTo` für Tests sichtbar gemacht.

## Aufwand

- ~30 Minuten Code + Tests
- ~10 Minuten für den Options-Eintrag + Doku
- Insgesamt: ~45 Minuten, 1 Commit

## Risiko

Niedrig. Die Änderung ist additiv-defensiv: bestehende Queries (normale Strings
ohne Sonderzeichen) liefern identische Ergebnisse. Nur Queries mit `%`/`_`/`[`/
`\` ändern ihr Verhalten — von "Wildcard-Match" zu "Literal-Match", was die
*richtige* Semantik ist.

## Migrations-Plan

1. Neuer `KnowHowToAiOptions.Search`-Bereich mit `MaxQueryLength` (Default 200)
2. `SearchDocsAsync` validiert + escaped
3. Tests schreiben (mit In-Memory-Store oder SQLite, siehe F-TS-001)
4. Tool-Description anpassen (F-MC-001): "Sonderzeichen `%`, `_`, `[`, `\` werden
   literal behandelt, nicht als Wildcards"
