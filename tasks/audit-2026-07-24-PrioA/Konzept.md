# Audit Prio A — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Methodik:** Aus dem Gesamt-Audit (86 Findings) wurden die 5 Findings extrahiert, die als „wirklich wichtig oder klarer Bug" eingestuft wurden. Alle übrigen Findings (81) wurden bewusst weggelassen — Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand | Querverweise |
|---|---|---|---|---|
| [F-SE-001](#f-se-001--like-wildcard-injection-in-buildlikepattern) | LIKE-Wildcard-Injection in `BuildLikePattern` | High | ~45 Min | F-PE-002, F-MC-001 |
| [F-PE-002](#f-pe-002--searchdocsasync-ohne-toplimit) | `SearchDocsAsync` ohne `TOP`/`LIMIT` | High | ~30 Min | F-SE-001, F-MC-001 |
| [F-CD-001](#f-cd-001--string-enum-validation-in-logging-options) | String-Enum-Validation in `Logging`-Options | High | ~20 Min | — |
| [F-AR-002](#f-ar-002--core-services-ohne-ilogger-injection) | Core-Services ohne `ILogger<T>`-Injection | High | ~1,5 h | F-AR-001, F-DK-001 |
| [F-MC-001](#f-mc-001--tool-description-qualität) | Tool-Description-Qualität (Edge-Cases & Fehler-Semantik) | High | ~30 Min + Doku | F-SE-001, F-PE-002 |

**Gesamt-Aufwand:** ~3,25 Stunden reines Implementieren.

---

## F-SE-001 — LIKE-Wildcard-Injection in `BuildLikePattern`

> **Schweregrad:** High · **Dimension:** Sicherheit
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:79-94`
> **Querverweise:** F-PE-002, F-MC-001

### Problem

`SqlDocumentsStore.SearchDocsAsync` baut aus dem LLM-Argument `query` ein SQL-LIKE-Pattern via `BuildLikePattern` (`$"%{query}%"`). Der LLM-kontrollierte `query` wird *unverändert* in das Pattern interpoliert. `%` und `_` sind in SQL LIKE Wildcards.

### Vektoren

- **Vektor 1 — Wildcard-Smuggling (Mittel):** LLM schickt `query = "%"` → Pattern `%%%` → matched jede Zeile. Token-Budget-Sprengung bei großen Tabellen.
- **Vektor 2 — DoS via Pattern-Länge (Hoch):** LLM schickt `query` mit 1.000.000 Zeichen → SQL-Server scannt jede Zeile, vergleicht jede der 4 Spalten mit dem 1-MB-Pattern. Bei 10.000 Zeilen = ~40 Sekunden blockierter MCP-Thread. **Trivialer DoS gegen den lokalen SQL-Server.**
- **Vektor 3 — Plan-Compiler-Bombe (Mittel-Hoch, versionabhängig):** Alternierende Wildcard-Gruppen können Query-Optimizer-Timeouts auslösen.

### Aktuelle Mitigations

- `LIKE @Pattern` benutzt SQL-Parameter → keine klassische SQL-Injection
- `BuildLikePattern` ist die *einzige* Wand zwischen LLM und SQL-String

**Aber:** `BuildLikePattern` *bewusst* nutzt Wildcard-Bedeutung. Es *erlaubt* Substring-Matching, das ist der Sinn. Daher ist die Wand löchrig.

### Fix-Empfehlung

```csharp
// Neue Konstante in KnowHowToAiOptions.Search (neu), Default 200
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

    var pattern = BuildLikePattern(query);
    // ... ExecuteAsync
}

private static string BuildLikePattern(string query)
{
    // LIKE-Escape: % -> [%], _ -> [_], [ -> [[]
    var escaped = query
        .Replace("[", "[[]")  // erst [ escapen
        .Replace("%", "[%]")
        .Replace("_", "[_]");
    return $"%{escaped}%";
}
```

### Tests

- `BuildLikePattern_EscapesPercent` (Input `50%` → Output `%50[%]%`)
- `BuildLikePattern_EscapesUnderscore` (Input `a_b` → Output `%a[_]b%`)
- `BuildLikePattern_AllowsNormalSubstring`
- `BuildLikePattern_ThrowsOnTooLongQuery`
- `SearchDocsAsync_EmptyQuery_ReturnsEmpty`

`BuildLikePattern` ist `private static` → via Reflection testen oder `internal` + `InternalsVisibleTo`.

### Aufwand

- ~30 Min Code + Tests
- ~10 Min Options-Eintrag + Doku
- **Insgesamt: ~45 Min, 1 Commit**

### Risiko

Niedrig. Additiv-defensiv: bestehende Queries (normale Strings ohne Sonderzeichen) liefern identische Ergebnisse. Nur Queries mit `%`/`_`/`[` ändern Verhalten — von "Wildcard-Match" zu "Literal-Match", was die *richtige* Semantik ist.

---

---

## F-PE-002 — `SearchDocsAsync` ohne `TOP`/`LIMIT`

> **Schweregrad:** High · **Dimension:** Performance
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:79-92`
> **Querverweise:** F-SE-001, F-MC-001

### Problem

```csharp
public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(string query, CancellationToken cancellationToken)
{
    await using var connection = new SqlConnection(_connectionString);
    var rows = await connection.QueryAsync<DocumentSummary>(new CommandDefinition(
        $"""
        SELECT slug AS Slug, title AS Title FROM {_table}
        WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
        ORDER BY title;
        """,
        new { Pattern = BuildLikePattern(query) },
        cancellationToken: cancellationToken));
    return [.. rows];
}
```

**Probleme:**
1. **Kein `TOP`/`LIMIT`:** Wenn `query` breit matched (z.B. "e", "a", "der"), können hunderte oder tausende `DocumentSummary`-Datensätze zurückkommen.
2. **Sortierung alphabetisch, nicht nach Relevanz:** "Relevanz" ist nicht ohne Full-Text-Index berechenbar, aber alphabetische Sortierung ist *die schlechteste* für LLM-UX — die relevantesten Treffer landen verstreut.
3. **Token-Budget-Sprengung:** 1000 Treffer × 100 Token = 100.000 Token für eine einzige `search_docs`-Antwort. Claude Sonnet hat 200k Kontext, aber 100k in einer einzelnen Antwort ist ein "ich kann nicht mehr"-Limit.

`docs/04` Zeile 48 sagt:
> "Kein Ranking: Ergebnisse werden alphabetisch nach `title` sortiert, nicht nach Relevanz."

Das ist die *bewusste* Entscheidung. ABER: die fehlende `TOP`-Begrenzung ist nicht bewusst — die ist schlicht ein Loch.

### Fix-Empfehlung

```sql
SELECT TOP (@MaxResults) slug AS Slug, title AS Title
FROM dbo.<DocumentsTableName>
WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
ORDER BY
    CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,  -- Title-Treffer zuerst
    title;
```

Mit `MaxResults` aus `KnowHowToAiOptions.Search` (neu), Default z.B. 50.

**Logik:**
- Title-Treffer vor Content-Treffer (heuristisches Ranking)
- Cap bei `MaxResults` (Default 50)
- Optional: Pagination via `OFFSET`/`FETCH NEXT` (komplexer, nicht für v1)

### Aufwand

- ~30 Min + neuer Options-Eintrag + Doku + Test
- **Insgesamt: ~30 Min, 1 Commit**

### Risiko

Niedrig. Cap ist additiv-defensiv. Title-Ranking ist Heuristik, kann vom User über weitere Optionen verfeinert werden.

---

## F-CD-001 — String-Enum-Validation in `Logging`-Options

> **Schweregrad:** High · **Dimension:** Konfiguration
> **Datei:** `src/KnowHowToAI.Cli/Program.cs:174, 177`

### Problem

```csharp
.MinimumLevel.Is(Enum.Parse<LogEventLevel>(loggingOptions.MinimumLevel))
// ...
rollingInterval: Enum.Parse<RollingInterval>(loggingOptions.RollingInterval),
```

`Enum.Parse<T>("Banana")` wirft `ArgumentException: Requested value 'Banana' was not found.` Das passiert erst, *nachdem* `LoadOptions` erfolgreich war — also beim Logger-Setup in `RunValidate`/`RunImport`/`RunExport`/`RunServer`. Die Exception wird vom Top-Level-`catch` in `Program.cs` gefangen und führt zu Exit-Code 2 mit der rohen Exception-Message.

**Szenario:** User kopiert `appsettings.json`, ändert `"MinimumLevel": "Information"` auf `"MinimumLevel": "information"` (kleingeschrieben) → `Enum.Parse` ist case-sensitive, wirft. User sieht: `'information' was not found.`

### Fix-Empfehlung

```csharp
private static LogEventLevel ParseLogLevel(string value) =>
    Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level)
        ? level
        : throw new InvalidOperationException(
            $"Ungültiger Logging.MinimumLevel '{value}'. " +
            $"Erlaubt: {string.Join(", ", Enum.GetNames<LogEventLevel>())}.");

private static RollingInterval ParseRollingInterval(string value) =>
    Enum.TryParse<RollingInterval>(value, ignoreCase: true, out var interval)
        ? interval
        : throw new InvalidOperationException(
            $"Ungültiger Logging.RollingInterval '{value}'. " +
            $"Erlaubt: {string.Join(", ", Enum.GetNames<RollingInterval>())}.");
```

### Tests

- `ParseLogLevel_AcceptsLowercaseInput`
- `ParseLogLevel_RejectsInvalidWithAllowedValuesList`
- `ParseRollingInterval_AcceptsLowercaseInput`
- `ParseRollingInterval_RejectsInvalidWithAllowedValuesList`

### Aufwand

- ~20 Min + Tests + Doku-Hinweis in `docs/03`
- **Insgesamt: ~20 Min, 1 Commit**

### Risiko

Niedrig. Funktional keine Änderung außer besserer Fehlermeldung.

---

## F-AR-002 — Core-Services ohne `ILogger<T>`-Injection

> **Schweregrad:** High · **Dimension:** Architektur
> **Dateien:** `Sync/ImportService.cs:9`, `Sync/ExportService.cs:8`, `Sync/SqlDocumentsStore.cs:11`, `Validation/DocsValidator.cs:8`
> **Querverweise:** F-AR-001 (DI-Inkonsistenz), F-DK-001, F-SE-002 (geloggt wird)

### Problem

Vier Core-Services nehmen **kein** `ILogger<T>` entgegen:

| Service | Aktueller Constructor | Was fehlt |
|---|---|---|
| `ImportService` | `(Func<...>, int)` | Beobachtbarkeit pro Import-Lauf |
| `ExportService` | `(Func<...>)` | Beobachtbarkeit pro Export-Lauf |
| `SqlDocumentsStore` | `(string, string)` | SQL-Operation-Logging |
| `DocsValidator` | `(int)` | Validator-Start/Ende-Logging |

**Konsequenzen:**
- `SqlDocumentsStore.ReplaceAllAsync` weiß nicht, in welcher Bibliothek es gerade läuft (kann nicht loggen "Import für 'sage100'-Bibliothek gestartet")
- `DocsValidator.Validate` kann nicht loggen "validate gestartet für 142 Dateien in 'C:\...'", sondern muss vom Aufrufer mit-protokolliert werden
- `ImportService.ImportAsync` kann nicht loggen "Transaktion gestartet, 142 Dokumente eingefügt, Transaktion committed"
- Bei Fehlern in Core (z.B. `ReplaceAllAsync` wirft SQL-Exception): nur der Top-Level-`catch (Exception ex)` in `Program.cs` loggt. Kein lokaler Kontext.

Die `01-code-style.mdc` Zeile 19 sagt: "Kein Validierungs-/Fehlerbehandlungs-Ballast für Fälle, die nicht eintreten können." **Aber: Logging ist *kein* Ballast, sondern Sichtbarkeit.**

### Fix-Empfehlung

**Schritt 1 — `Microsoft.Extensions.Logging.Abstractions` zu Core:**

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
```

Leichtgewichtig (nur Interfaces, ~30 KB), keine konkrete Logger-Implementierung. Core bleibt unabhängig von konkreten Logging-Backends.

**Schritt 2 — `ILogger<T>` in den vier Services injizieren:**

```csharp
public sealed class SqlDocumentsStore(
    string connectionString,
    string documentsTableName,
    ILogger<SqlDocumentsStore> logger)
{
    public async Task ReplaceAllAsync(IReadOnlyList<Document> documents, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "ReplaceAll startet: {DocumentCount} Dokumente in Tabelle {Table}",
            documents.Count, _table);
        var sw = Stopwatch.StartNew();
        // ...
        logger.LogInformation(
            "ReplaceAll abgeschlossen: {DocumentCount} Dokumente in {Elapsed}ms",
            documents.Count, sw.ElapsedMilliseconds);
    }
}
```

**Schritt 3 — DI-Composition-Root in `Program.cs`** (siehe F-AR-001):

```csharp
static SqlDocumentsStore BuildStore(KnowHowToAiOptions options, ILogger<SqlDocumentsStore> logger)
    => new(options.ConnectionString, options.DocumentsTableName, logger);
```

**Schritt 4 — Tests anpassen mit `NullLogger<T>.Instance`:**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
var service = new ImportService(
    (_, _) => Task.CompletedTask,
    NullLogger<ImportService>.Instance,
    maxContentLengthWarning: 8000);
```

### Aufwand

- ~5 Min für NuGet-Referenz
- ~1 h für die vier Services (je ~15 Min: Constructor, 2-3 Log-Calls pro öffentlicher Methode, Tests anpassen)
- ~30 Min für die Composition-Root-Anpassung
- **Insgesamt: ~1,5 h, 1-2 Commits** (NuGet + Service-Updates ggf. separat)

### Risiko

Niedrig. `ILogger<T>` ist additiv. Tests werden mit `NullLogger<T>.Instance` ausgestattet, was null Impact hat.

**Achtung:** `ImportService` und `ExportService` sind *positional records* (C# 12+). Neuer Parameter zwingt zu Update aller Aufrufer (Tests, `Program.cs`, jede zukünftige Verwendung).

### Querverweis

F-AR-001 (DI-Inkonsistenz) wurde **nicht** als Prio A aufgenommen, weil es funktional läuft und nur inkonsistent ist. Es wird in der Umsetzung von F-AR-002 *mitgemacht*, weil die Composition-Root-Factory sowieso gebaut wird.

---

## F-MC-001 — Tool-Description-Qualität

> **Schweregrad:** High · **Dimension:** MCP-Tool-API
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:16, 25, 34`
> **Querverweise:** F-SE-001, F-PE-002

### Problem

Die drei Tool-Descriptions sind sehr knapp und lassen LLMs über zentrale Edge-Cases im Dunkeln.

**`list_children` (Zeile 16):**
> "Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug leer ist)."

Was fehlt:
- Was passiert, wenn `parentSlug` nicht existiert? Aktuell: leere Liste. LLM erwartet vielleicht einen Fehler.
- Was ist die Reihenfolge der Treffer? Aktuell: unspezifiziert (siehe F-PE-003). LLM sollte das wissen, um ggf. selbst zu sortieren.
- Wann ist `parentSlug` "leer"? `null`, `""`, beides? Aktuell: nur `null` matcht Root. LLM schickt vielleicht `""` und ist verwirrt.
- Gibt es eine maximale Anzahl? Aktuell: keine Cap.

**`search_docs` (Zeile 25):**
> "Durchsucht Titel, Inhalt, Tags und Synonyme nach einem Suchbegriff."

Was fehlt:
- Welche Such-Semantik? `LIKE '%query%'` (Substring, case-insensitive auf Windows-Collation, case-sensitive auf Linux-Collation — siehe F-SE-004).
- Ranking? Nein, alphabetisch sortiert. LLM weiß nicht, dass die *ersten* Treffer nicht die relevantesten sind.
- Max-Treffer-Anzahl? Aktuell: keine Cap (siehe F-PE-002).
- Special Characters? `%` und `_` werden in LIKE-Pattern als Wildcards interpretiert (siehe F-SE-001). LLM schickt `query = "50%"` und bekommt Treffer, die "50" + beliebiges Zeichen enthalten.

**`get_doc` (Zeile 34):**
> "Lädt Titel und Inhalt eines einzelnen Dokuments anhand seines Slugs."

Was fehlt:
- Was, wenn Slug nicht existiert? Aktuell: `null`. LLM muss das erkennen.
- Wie groß kann der Inhalt sein? `NVARCHAR(MAX)`, also mehrere MB. LLM hat Token-Budget.
- Enthält der Inhalt YAML-Front-Matter? Nein (das ist in `title`/`tags`/`synonyms` aufgeteilt).

### Fix-Empfehlung (Beispiel `list_children`)

```csharp
[Description("""
    Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn parentSlug
    weggelassen oder null ist). Sortierung: alphabetisch nach Slug.

    Edge Cases:
    - parentSlug = null oder weggelassen: listet Root-Dokumente
    - parentSlug = "" (leerer String): wirft ArgumentException, nicht das gleiche wie null
    - parentSlug existiert nicht als Dokument: leere Liste, kein Fehler
    - parentSlug ist kein gültiger Slug (z.B. "Foo Bar"): wird vom Server akzeptiert,
      liefert leere Liste

    Beispiel:
    - list_children(parentSlug=null) → DocumentSummary[] der Root-Dokumente
    - list_children(parentSlug="it") → DocumentSummary[] der direkten Kinder von "it"

    Es gibt keine Cap; bei sehr breiten Verzeichnissen ggf. >100 Treffer.
    """)]
```

Analog für `search_docs` und `get_doc`. Für `search_docs` insbesondere: Hinweis auf LIKE-Semantik, Wildcard-Literal-Verhalten (kommt mit F-SE-001), `TOP`-Cap (kommt mit F-PE-002), Hash-Logging (kommt mit F-SE-002).

### Aufwand

- ~30 Min für alle drei Tools + Doku-Update in `docs/02` Abschnitt 4.D
- **Insgesamt: ~30 Min + Doku, 1 Commit** (nach F-SE-001/002 + F-PE-002, weil die Description das geänderte Verhalten dokumentieren muss)

### Risiko

Niedrig. Reine Text-Änderung in `[Description(...)]`-Attributen. Kein Code-Behavior-Change.

---

## Warum diese 6 und nicht andere?

### Aufgenommen (Prio A — "wirklich wichtig oder klarer Bug"):

1. **F-SE-001** — Klares Sicherheits-Risiko (DoS-Vektor) mit dokumentiertem Vektor.
2. **F-PE-002** — Klare LLM-UX-Gefahr (Token-Budget-Sprengung). Trifft die Kernfunktion (`search_docs`).
3. **F-CD-001** — Klarer UX-Bug: kryptische Fehlermeldung statt sprechender Hint. Trivial zu fixen.
4. **F-AR-002** — "Wenn die App in Produktion geht, ist das kritisch" — Beobachtbarkeit ist Pflicht, nicht Kür.
5. **F-MC-001** — LLM-UX-Kernproblem. Hängt eng mit F-SE-001 und F-PE-002 zusammen, weil die Description das jeweilige Verhalten dokumentieren muss.

### Bewusst weggelassen (Kurzbegründung):

- **F-SE-002 (PII via LLM-Args im Serilog-File):** Bewusste User-Entscheidung — Ralf hat direkten SQL-Zugriff, PII im Log ist kein Problem. Voller Arg-Dump in den Logs wird beibehalten.
- **F-AR-001 (DI-Inkonsistenz):** Funktioniert, ist nur Inkonsistenz. Wird in F-AR-002 *mitgemacht*.
- **F-DP-001 (Preview-Dependencies):** Bewusste Wahl, läuft. Niedrig-Priorität.
- **F-TS-001 (SqlDocumentsStore ohne Tests):** Per `02-testing.mdc` explizit akzeptiert. Backlog.
- **F-CQ-001, F-PE-001:** Bereits ✅ umgesetzt (Commits `d262095`, `27570cd`).
- **F-DK-001:** Obsolet nach F-PE-001-Fix.
- **Alle Medium/Low/Info-Findings:** Per Definition nicht Prio A. Beispiele:
  - AiNetLinter-Verstöße (`F-CQ-002/004/005`) — "unschön aber kein Weltuntergang" (Ralf)
  - Doku-Drifts (`F-DK-002` bis `F-DK-008`) — nice-to-have
  - Test-Edge-Cases (`F-TS-002` bis `F-TS-011`) — Ergänzungen, nicht-blockierend
  - Performance-Niedrig (`F-PE-003/004/005/006/007/008`) — Mikro-Optimierungen
  - MCP-UX-Erweiterungen (`F-MC-002` bis `F-MC-007`) — Beispiele, Cancellation, Naming
  - Architecture-Cleanups (`F-AR-003/004/005/006/007`) — Refactor-Optionen

## Empfohlene Umsetzungs-Reihenfolge

1. **F-CD-001** (~20 Min) — isoliert, trivial, kann sofort
2. **F-SE-001** (~45 Min) — vor F-PE-002, weil F-PE-002 das Verhalten dokumentieren muss
3. **F-PE-002** (~30 Min) — baut auf F-SE-001 (gleicher Code-Pfad)
4. **F-MC-001** (~30 Min + Doku) — baut auf F-SE-001 und F-PE-002 auf, dokumentiert das geänderte Verhalten
5. **F-AR-002** (~1,5 h) — eigenständig, am Schluss weil umfangreichster Eingriff

**Gesamt-Aufwand in dieser Reihenfolge:** ~3,25 h, 5 Commits.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).
