---
status: draft
type: konzept
project_kind: brownfield
estimated_scope: medium
last_updated: 2026-07-26
open_questions: []
---

# Konzept: Audit Prio A — fünf Fixes für KnowHowToAI v1.0.2

## Ziel (Was)

Fünf als „High" eingestufte Findings aus dem Code-Audit v1.0.2 (HEAD `e5e0008`,
Stichtag 2026-07-24) in **fünf separaten Commits** umsetzen. Geschätzter
Gesamtaufwand ~3,25 Stunden reines Implementieren. Danach sind die fünf
dokumentierten Risiken (DoS-Vektor, Token-Budget-Sprengung, kryptische
Konfig-Fehlermeldung, fehlende Beobachtbarkeit, knappe LLM-Tool-Beschreibungen)
geschlossen — und der Stand ist Build+Tests+Lint+Doku-grün.

## Warum / Kontext

- **Audit-Quelle:** `tasks/audit-2026-07-24/README.md` (Executive Summary,
  Schweregrad-Verteilung 0 Critical / 10 High / 26 Medium / 21 Low / 28 Info,
  gesamt **85 Findings** in 9 Dimensionen; zzgl. `_demo-docs/`-Mini-Audit =
  86 gesamt). Diese 5 sind die *priorisierten* High-Findings, die im
  priorisierten Plan (`_plan/prioritized-fixes.md`) als erste bearbeitet
  werden.
- **Warum ausgerechnet diese 5:** dokumentiert in der Sektion „Warum diese
  5 und nicht andere" weiter unten — kurz: ein klares Sicherheits-Risiko
  (DoS), zwei LLM-UX-Gefahren (Token-Sprengung, knappe Description), ein
  trivialer UX-Bug (kryptische Fehlermeldung) und ein Architekturloch
  (fehlende Beobachtbarkeit). Alle übrigen 81 Findings wurden bewusst
  draußen gelassen.
- **Bestehender Stand:** v1.0.2-Release, Build grün, 49 → 55 Tests grün
  (nach F-PE-001/F-CQ-001/F-CQ-002 in Commits `d262095`/`27570cd`),
  AiNetLinter 0 Verstöße. Working-Tree clean, 4 Commits ahead of origin
  (nicht relevant für die Umsetzung; der Push bleibt beim Nutzer).
- **Bekannter Vorbehalt:** docs/03 Abschnitt 2 dokumentiert ein
  SQL-Server-Setup-Problem auf dem Dev-Rechner (TCP-Binding, Login-Aut).
  Davon ist die Umsetzung der 5 Fixes *nicht* abhängig (kein Fix braucht
  eine laufende DB), aber der End-to-End-Smoke ist ggf. blockiert. Siehe
  Definition of Done.

## Scope

### Muss-Haben (5 Fixes, ~3,25 h, 5 Commits)

| # | ID | Titel | Schwere | Aufwand | Hängt ab von |
|---|---|---|---|---|---|
| 1 | F-CD-001 | String-Enum-Validation in `Logging`-Options | High | ~20 Min | — |
| 2 | F-SE-001 | LIKE-Wildcard-Injection in `BuildLikePattern` | High | ~45 Min | — |
| 3 | F-PE-002 | `SearchDocsAsync` ohne `TOP`/`LIMIT` | High | ~30 Min | #2 (gleicher Code-Pfad) |
| 4 | F-MC-001 | Tool-Description-Qualität (Edge-Cases & Fehler-Semantik) | High | ~30 Min + Doku | #2, #3 (dokumentiert deren Verhalten) |
| 5 | F-AR-002 | Core-Services ohne `ILogger<T>`-Injection | High | ~1,5 h | — (eigenständig) |

### Nice-to-Have (im Konzept bewusst draußen)

- **F-MC-002 (Beispiel-Outputs in MCP-Tool-Description):** kann mit F-MC-001
  zusammen kommen, falls der Coder es für sinnvoll hält; Aufwand < 15 Min,
  LLM-UX-Mehrwert da, aber keine harte Prio-A-Anforderung. *Empfehlung:
  rein damit, Aufwand-Nachteil minimal.*
- **Beispiel-Logs / Beispiel-Responses im Doku-Update zu F-AR-002:** kann
  mit dem Doku-Commit zu F-AR-002 mitkommen, falls der Planer es einbaut.
- **F-AR-001 (DI-Inkonsistenz als eigenständiger Refactor):** funktional
  lauffähig, wird *innerhalb* von F-AR-002 implizit konsolidiert (eine
  Composition-Root-Factory in `Program.cs` deckt das nebenbei mit).

### Non-Goals (bewusst NICHT Teil davon)

- **Die übrigen 81 Audit-Findings** (F-SE-002 ausgenommen — siehe
  Verworfene Alternativen, F-AR-001, F-DP-001, F-TS-001, alle Medium/Low/
  Info-Findings) — entweder bereits umgesetzt (F-CQ-001/002, F-PE-001,
  F-DK-001 obsolet), bewusst beibehalten (F-SE-002, F-DP-001) oder
  Backlog (F-TS-001, F-DK-002 bis F-DK-008, F-PE-003 bis F-PE-008, F-MC-002
  bis F-MC-007, F-AR-003 bis F-AR-007, F-CQ-002 bis F-CQ-005, F-TS-002
  bis F-TS-011, F-CD-002 bis F-CD-004).
- **Versions-Bump / Release-Tag** — der Release-Workflow (`scripts/
  create-release.ps1`) ist eine separate, vom Nutzer angestoßene Aktion.
  Nicht Teil dieses Tasks.
- **Push zu `origin`** — bleibt beim Nutzer, wie in `.agents/rules/03-git-
  workflow.mdc` festgelegt.
- **End-to-End-Smoke, sofern das SQL-Setup-Problem noch offen ist** — siehe
  Definition of Done.

## Zielplattformen / Technischer Rahmen

| Bereich | Wahl | Begründung |
|---|---|---|
| Runtime | .NET 10 / C# 14 | bestehend — keine Änderung |
| Test-Framework | xUnit v3 | bestehend |
| Logging-Abstraktion | `Microsoft.Extensions.Logging.Abstractions` | neu in Core, nur Interfaces (~30 KB), keine konkrete Logger-Implementierung — Core bleibt unabhängig vom konkreten Logging-Backend (Serilog bleibt exklusiv in Cli) |
| DB-Zugriff | Dapper + `Microsoft.Data.SqlClient` | bestehend |
| SQL-Server | bestehende lokale Instanz `NB-RALF261022\MSSQLSERVER2022` | bestehend; Setup-Problem Vorbehalt (s. o.) |
| Linting | AiNetLinter | bestehend; `*.Core` weiterhin strikt, `*.Cli` mit `EnableTestSentinel: false` |

**Kein neuer Tech-Stack.** Alle fünf Fixes nutzen bestehende Bausteine;
einziger neuer Baustein ist die `Abstractions`-Logging-Library, die nur
Interfaces beisteuert.

## Verworfene Alternativen

- **F-SE-002 (PII via LLM-Args im Serilog-File) — bewusst beibehalten.**
  Ralf hat direkten SQL-Zugriff; PII im Log ist im aktuellen Setup kein
  Problem. Voller Arg-Dump in den Logs wird nicht entfernt. *Kann bei
  produktivem Einsatz mit echten PII-Pflichten re-evaluiert werden.*
- **F-AR-001 (DI-Inkonsistenz als eigenständiger Refactor) — abgedeckt
  durch F-AR-002.** Funktioniert, ist nur inkonsistent; die in F-AR-002
  entstehende Composition-Root-Factory löst die Inkonsistenz nebenbei.
- **F-DP-001 (Preview-Dependencies auf Stable downgraden) — bewusst
  behalten.** Niedrige Priorität; wenn 2.0-Stable der Preview-Deps kommt,
  Wiederevaluation. Kein Prio-A.
- **F-TS-001 (SQL-Integrationstest-Infrastruktur) — explizit Backlog.**
  Per `02-testing.mdc` akzeptiert. Ohne diese Infrastruktur ist
  `BuildLikePattern` testtechnisch nur über `InternalsVisibleTo` oder
  Reflection zugänglich (siehe Fix-Detail F-SE-001).
- **F-CQ-001/002, F-PE-001 — bereits umgesetzt** (Commits `27570cd`,
  `d262095`), im Audit-Plan als ✅ markiert.
- **F-DK-001 — obsolet nach F-PE-001.**
- **Alle Medium/Low/Info-Findings** — per Definition nicht Prio A. Repräsentanten:
  AiNetLinter-Mikro-Verstöße (F-CQ-002/004/005), Doku-Drifts (F-DK-002 bis
  F-DK-008), Test-Edge-Cases (F-TS-002 bis F-TS-011), Performance-Mikro-
  Optimierungen (F-PE-003 bis F-PE-008), MCP-UX-Erweiterungen (F-MC-002
  bis F-MC-007), Architecture-Cleanups (F-AR-003 bis F-AR-007).

## Wo im Projekt

| Datei | Fix | Art der Änderung |
|---|---|---|
| `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs` | F-SE-001, F-PE-002, F-AR-002 | `BuildLikePattern` → `internal static` + Escape; SQL um `TOP` + Title-Ranking; Konstruktor + 2-3 Log-Calls |
| `src/KnowHowToAI.Core/Sync/ImportService.cs` | F-AR-002 | positional record: +1 ctor-Param `ILogger<ImportService>` (zieht Update aller Aufrufer nach sich) |
| `src/KnowHowToAI.Core/Sync/ExportService.cs` | F-AR-002 | positional record: +1 ctor-Param `ILogger<ExportService>` (gleiches Aufrufer-Update) |
| `src/KnowHowToAI.Core/Validation/DocsValidator.cs` | F-AR-002 | Konstruktor + 1-2 Log-Calls |
| `src/KnowHowToAI.Core/Configuration/KnowHowToAiOptions.cs` | F-SE-001, F-PE-002 | neue Sub-Options-Klasse `KnowHowToAiSearchOptions` (Properties `MaxQueryLength`, `MaxResults`) analog zu `Logging`/`Validation` |
| `src/KnowHowToAI.Cli/Program.cs` | F-CD-001, F-AR-002 | `Enum.Parse` → `TryParse`+eigene `InvalidOperationException`; Composition-Root-Factory-Funktion; `SqlDocumentsStore`-Konstruktion in 3 Run-Methoden auf Factory umstellen |
| `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` | F-MC-001 | 3× `[Description(...)]`-String um Edge-Cases, Fehler-Semantik, Sortierung, Cap-Begründung erweitern |
| `src/KnowHowToAI.Cli/appsettings.json` | F-SE-001, F-PE-002 | `KnowHowToAi.Search.{MaxQueryLength,MaxResults}` ergänzen |
| `tests/KnowHowToAI.Core.Tests/...` (neu/erweitert) | F-SE-001, F-AR-002 | `BuildLikePattern` via `InternalsVisibleTo`; Tests mit `NullLogger<T>.Instance` |
| `docs/02-Architektur-und-Techstack.md` | F-MC-001, F-SE-001 | Abschnitt 4.D (Tool-Description-Quelldoku); LIKE-Semantik präziser |
| `docs/03-Projektstruktur-und-Konfiguration.md` | F-CD-001, F-SE-001, F-PE-002 | Abschnitt 2: JSON-Beispiel + Options-Tabelle um `Logging`-Enum-Hinweis und `Search`-Sub-Options erweitern |
| `docs/04-Datenmodell-Validierung-Edgecases.md` | F-SE-001, F-PE-002 | Abschnitt 1 (search_docs-Query): LIKE-Semantik, Wildcard-Literal-Verhalten, TOP-Cap, Title-Ranking präzise festhalten |
| `docs/05-Roadmap.md` | F-AR-002 (implizit) | keine Änderung nötig — `ILogger`-Injection ist *kein* Roadmap-Punkt, sondern Architektur-Verbesserung im laufenden v1 |

## Wie (grober Ansatz)

**Empfohlene Reihenfolge (5 Commits, ~3,25 h):**

```
1. F-CD-001   ~20 Min   isoliert, trivial, kann sofort
2. F-SE-001   ~45 Min   DoS-Vektor schließen, vor F-PE-002 (gleicher Code-Pfad)
3. F-PE-002   ~30 Min   baut auf F-SE-001 auf, gleiche Code-Stelle
4. F-MC-001   ~30 Min   dokumentiert das geänderte Verhalten aus #2 + #3
   + Doku
5. F-AR-002   ~1,5 h    eigenständig, am Schluss weil umfangreichster Eingriff
```

Tiebreak-Logik: (a) Security > Performance > Architektur > LLM-UX > Konvention;
(b) bei Gleichstand weniger Abhängigkeiten zwischen Fixes = früher;
(c) bei weiterem Gleichstand weniger Dateien angefasst = früher.

### Fix-Detail-Übersicht

Die detaillierten Beschreibungen (Problem, Vektoren, Mitigations, Fix-Code,
Test-Liste, Aufwand-Schätzung, Risiko-Bewertung) je Finding liegen in den
folgenden Sub-Sektionen. Pro Fix: ein Commit, Code + Tests + ggf. Doku in
demselben Commit (per `.agents/rules/03-git-workflow.mdc`).

#### Fix 1 — F-CD-001: String-Enum-Validation in `Logging`-Options

**Problem:** `Program.cs:174, 177` ruft `Enum.Parse<LogEventLevel>(...)` und
`Enum.Parse<RollingInterval>(...)`. Bei falschem Wert (z. B. `"information"`
kleingeschrieben) wirft das erst nach erfolgreichem `LoadOptions`, mit
kryptischer `ArgumentException: Requested value 'information' was not found.`

**Fix-Empfehlung:**

```csharp
// In Program.cs, neue private static Helper
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

Aufrufstellen `Enum.Parse<...>(...)` → `ParseLogLevel(...)` / `ParseRollingInterval(...)`.

**Tests:** `ParseLogLevel_AcceptsLowercaseInput`,
`ParseLogLevel_RejectsInvalidWithAllowedValuesList`,
`ParseRollingInterval_AcceptsLowercaseInput`,
`ParseRollingInterval_RejectsInvalidWithAllowedValuesList`.
Da `Program.cs` zu `*.Cli` gehört (`EnableTestSentinel: false` per
AiNetLinter-Override, siehe docs/03 Abschnitt 4), Test-Zugriff via
`InternalsVisibleTo` oder die Helper in eine kleine `internal static class`
im Cli-Projekt extrahieren.

**Doku:** `docs/03` Abschnitt 2: Hinweis auf case-insensitive
Enum-Parsing in der `Logging`-Tabelle ergänzen.

**Risiko:** Niedrig. Funktional keine Änderung außer besserer Fehlermeldung.

---

#### Fix 2 — F-SE-001: LIKE-Wildcard-Injection in `BuildLikePattern`

**Problem:** `SqlDocumentsStore.SearchDocsAsync:79-92` baut via
`BuildLikePattern` (`$"%{query}%"`) aus dem LLM-kontrollierten `query` ein
LIKE-Pattern *ohne* Wildcard-Escaping. `%` und `_` sind LIKE-Wildcards;
Längen-Cap fehlt komplett.

**Vektoren:**
- Wildcard-Smuggling (`query="%"` → matched alles)
- DoS via Pattern-Länge (LLM schickt 1 MB → SQL-Server scannt alle Zeilen
  × 4 Spalten)
- Plan-Compiler-Bombe (versionabhängig)

**Fix-Empfehlung:**

```csharp
// Neue Sub-Options in KnowHowToAiOptions.cs
public sealed record KnowHowToAiSearchOptions
{
    public int MaxQueryLength { get; init; } = 200;
    public int MaxResults { get; init; } = 50;  // siehe Fix 3
}

// In SqlDocumentsStore.SearchDocsAsync
public async Task<IReadOnlyList<DocumentSummary>> SearchDocsAsync(
    string query,
    int maxQueryLength,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(query)) return [];
    if (query.Length > maxQueryLength)
    {
        throw new ArgumentException(
            $"search_docs query ist {query.Length} Zeichen lang, max {maxQueryLength}.",
            nameof(query));
    }
    // ... rest mit BuildLikePattern(query)
}

internal static string BuildLikePattern(string query)
{
    // LIKE-Escape: % -> [%], _ -> [_], [ -> [[]
    var escaped = query
        .Replace("[", "[[]")
        .Replace("%", "[%]")
        .Replace("_", "[_]");
    return $"%{escaped}%";
}
```

`BuildLikePattern` wird `internal static` + `InternalsVisibleTo` für
`tests/KnowHowToAI.Core.Tests` (saubere Test-Strategie statt Reflection;
Standard-Pattern in .NET).

**Tests** (`tests/KnowHowToAI.Core.Tests/Sync/BuildLikePatternTests.cs` neu):
- `BuildLikePattern_EscapesPercent` (Input `50%` → Output `%50[%]%`)
- `BuildLikePattern_EscapesUnderscore` (Input `a_b` → Output `%a[_]b%`)
- `BuildLikePattern_EscapesOpeningBracket` (Input `[abc` → Output `%[[]abc%`)
- `BuildLikePattern_AllowsNormalSubstring` (Input `routing` → `%routing%`)
- `SearchDocsAsync_EmptyQuery_ReturnsEmpty`
- `SearchDocsAsync_QueryTooLong_ThrowsArgumentException`

Da `SearchDocsAsync` selbst eine SQL-Operation auslöst, wird der reine
`BuildLikePattern`-Test isoliert geprüft; die `SearchDocsAsync`-Tests
bleiben als Pläne (ohne laufende DB). Alternative: vorhandene
`SqlDocumentsStore`-Tests via SQLite/In-Memory (Backlog F-TS-001).

**Doku:** `docs/04` Abschnitt 1 (search_docs-Query) — präziser
LIKE-Semantik-Block: Wildcards werden literal behandelt, Längen-Cap,
Hinweis auf `KnowHowToAi.Search.MaxQueryLength`. `docs/02` Abschnitt 4.D
analog (in Verbindung mit Fix 4). `docs/03` Abschnitt 2 — JSON-Beispiel
um `Search`-Block erweitern.

**Risiko:** Niedrig. Additiv-defensiv: bestehende Queries mit normalen
Strings liefern identische Ergebnisse. Nur Queries mit `%`/`_`/`[`
ändern Verhalten — von "Wildcard-Match" zu "Literal-Match", was die
*richtige* Semantik ist.

---

#### Fix 3 — F-PE-002: `SearchDocsAsync` ohne `TOP`/`LIMIT`

**Problem:** dieselbe Stelle wie Fix 2. Ohne `TOP`-Cap können hunderte bis
tausende Treffer zurückkommen, was das LLM-Token-Budget sprengt. Sortierung
alphabetisch, nicht nach Relevanz — die relevantesten Treffer landen
verstreut.

**Bewusste Entscheidung dokumentiert:** docs/04 Z.48 sagt „Kein Ranking:
Ergebnisse werden alphabetisch sortiert" — das ist die *bewusste*
Grundsatzentscheidung. Die fehlende `TOP`-Begrenzung ist aber ein *Loch*,
keine Entscheidung.

**Fix-Empfehlung:**

```sql
SELECT TOP (@MaxResults) slug AS Slug, title AS Title
FROM dbo.<DocumentsTableName>
WHERE title LIKE @Pattern OR content LIKE @Pattern
   OR tags LIKE @Pattern OR synonyms LIKE @Pattern
ORDER BY
    CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,  -- Title-Treffer zuerst
    title;
```

`MaxResults` aus `KnowHowToAiOptions.Search.MaxResults` (Default 50).
`SearchDocsAsync` nimmt den Wert als zusätzlichen Parameter entgegen
(oder bekommt ihn via Options-Injection in F-AR-002).

**Tests:** Verifikation über `SearchDocsAsync`-Aufrufe (Backlog F-TS-001
für echte DB); hier: Dokumentation im `SearchDocsAsync`-XML-Kommentar
(sofern vorhanden) oder im Code-Selbstkommentar warum `MaxResults`
existiert.

**Doku:** `docs/02` Abschnitt 4.D — Hinweis auf `TOP`-Cap in der
`search_docs`-Tool-Doku (wird mit Fix 4 in die Description übernommen).
`docs/04` Abschnitt 1 — `MaxResults`-Hinweis.

**Risiko:** Niedrig. Cap ist additiv-defensiv. Title-Ranking ist Heuristik,
kann über weitere Optionen verfeinert werden.

---

#### Fix 4 — F-MC-001: Tool-Description-Qualität

**Problem:** Drei Tool-Descriptions in `DocsMcpTools.cs:14, 23, 32` sind
sehr knapp und lassen LLMs über zentrale Edge-Cases im Dunkeln.

**Fix-Empfehlung (Beispiel `list_children`):**

```csharp
[Description("""
    Listet die direkten Kind-Dokumente eines Slugs (oder der Wurzel, wenn
    parentSlug weggelassen oder null ist). Sortierung: alphabetisch nach Slug.

    Edge Cases:
    - parentSlug = null oder weggelassen: listet Root-Dokumente
    - parentSlug = "" (leerer String): wirft ArgumentException, nicht das gleiche wie null
    - parentSlug existiert nicht als Dokument: leere Liste, kein Fehler
    - parentSlug ist kein gültiger Slug (z.B. "Foo Bar"): wird vom Server
      akzeptiert, liefert leere Liste

    Beispiel:
    - list_children() → DocumentSummary[] der Root-Dokumente
    - list_children(parentSlug="it") → DocumentSummary[] der direkten Kinder von "it"

    Es gibt keine Cap; bei sehr breiten Verzeichnissen ggf. >100 Treffer.
    """)]
```

Analog für `search_docs` und `get_doc`. Für `search_docs` insbesondere:
- LIKE-Semantik (`'%query%'`, Substring)
- Wildcard-Literal-Verhalten (kommt mit Fix 2)
- Title-Ranking (kommt mit Fix 3)
- `TOP`-Cap (kommt mit Fix 3, Default 50)
- Keine Ranking-Garantie über Title-Treffer hinaus

Für `get_doc`:
- `null`-Return bei unbekanntem Slug
- Token-Budget-Hinweis (NVARCHAR(MAX))
- Kein YAML-Front-Matter im Content

**Nice-to-Have (kann mit rein):** ein konkretes Beispiel-Output-JSONSnippet
pro Tool. Aufwand < 15 Min, LLM-UX-Mehrwert da.

**Doku:** `docs/02` Abschnitt 4.D als *Quell-Doku* für die Description-
Texte — damit Description und Doku nicht auseinanderlaufen. Eine kurze
Notiz, dass Description aus diesem Abschnitt gespeist wird.

**Risiko:** Niedrig. Reine Text-Änderung in `[Description(...)]`.
Kein Code-Behavior-Change. *Aber*: muss nach Fix 2 + 3 kommen, weil es
deren Verhalten dokumentiert.

---

#### Fix 5 — F-AR-002: Core-Services ohne `ILogger<T>`-Injection

**Problem:** Vier Core-Services nehmen **kein** `ILogger<T>` entgegen.
Konsequenzen: keine Beobachtbarkeit pro Import-/Export-/SQL-/
Validate-Lauf; bei Fehlern in Core nur Top-Level-`catch` loggt ohne
lokalen Kontext.

| Service | Aktueller Konstruktor | Was fehlt |
|---|---|---|
| `SqlDocumentsStore` | `(string, string)` | SQL-Op-Logging |
| `DocsValidator` | `(int)` | Validator-Start/-Ende |
| `ImportService` | `(Func<...>, int)` | Pro-Import-Lauf-Logging |
| `ExportService` | `(Func<...>)` | Pro-Export-Lauf-Logging |

**Fix-Empfehlung:**

**Schritt 5a — NuGet-Ref in `KnowHowToAI.Core.csproj`:**

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
```

**Schritt 5b — Konstruktor-Updates + Log-Calls pro Service:**

```csharp
// SqlDocumentsStore — ctor wird (string, string, ILogger<SqlDocumentsStore>)
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
        // ... bestehender Body ...
        logger.LogInformation(
            "ReplaceAll abgeschlossen: {DocumentCount} Dokumente in {Elapsed}ms",
            documents.Count, sw.ElapsedMilliseconds);
    }
}
```

Analog für `DocsValidator` (Start/Ende + Datei-Count), `ImportService`
(Transaktion-Start/Commit/Ende), `ExportService` (Marker-Check-Entscheidung
+ Export-Count).

**Achtung:** `ImportService` und `ExportService` sind *positional records*
(C# 12+). Neuer Ctor-Parameter zwingt zu Update **aller** Aufrufer:
- `tests/KnowHowToAI.Core.Tests` (mehrere Test-Dateien, anpassen mit
  `NullLogger<T>.Instance`)
- `Program.cs` `RunImport` und `RunExport` (sowie F-AR-002 c)

**Schritt 5c — Composition-Root-Factory in `Program.cs`:**

```csharp
static SqlDocumentsStore BuildStore(KnowHowToAiOptions options, ILogger<SqlDocumentsStore> logger)
    => new(options.ConnectionString, options.DocumentsTableName, logger);

static ImportService BuildImport(KnowHowToAiOptions options, ILogger<ImportService> logger)
    => new(BuildStore(options, /* logger passt hier nicht direkt */ /* s.u. */),
           options.Validation.MaxContentLengthWarning,
           logger);

// Hinweis: Da ImportService ein Func<...>-Delegate statt SqlDocumentsStore
// erwartet, muss die Factory die SqlDocumentsStore-Logger-Verkabelung
// selbst auflösen. Empfehlung: kleine Hilfsfunktion, die Store + Logger
// in das passende Delegate-Format bringt, oder (sauberer) ImportService
// bekommt in einer zukünftigen Iteration einen Store-Parameter statt
// Delegate — wird im Planer-Step konkretisiert, nicht hier.
```

`F-AR-001` (DI-Inkonsistenz) wird *mitgemacht*: die Factory löst die
Inkonsistenz, dass `SqlDocumentsStore` mal mit `new` (in 3 Run-Methoden)
und mal per DI (im Server) konstruiert wird.

**Schritt 5d — Tests anpassen:**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
var service = new ImportService(
    (_, _) => Task.CompletedTask,
    NullLogger<ImportService>.Instance,
    maxContentLengthWarning: 8000);
```

Analog für die anderen drei Services.

**Schritt 5e — `RunServer` in `Program.cs`:**

Im Server-Modus wird `SqlDocumentsStore` bereits per DI gebaut; `ILogger<SqlDocumentsStore>` ist via `AddSerilog(Log.Logger)` als Logger-Provider verfügbar. Per `AddSingleton<ILogger<SqlDocumentsStore>>(sp => new Logger<SqlDocumentsStore>(Log.Logger))` oder durch Anpassung der Service-Registrierung an `AddLogging` muss der Logger explizit aufgelöst werden — Details im Planer-Step.

**Doku:** `docs/03` Abschnitt 1 (Solution-Layout) — Notiz, dass Core-
Services `ILogger<T>` per Konstruktor erwarten. `docs/02` Abschnitt 2
(Tech-Stack-Tabelle) — `Microsoft.Extensions.Logging.Abstractions` als
neue Dep in der Tabelle ergänzen.

**Risiko:** Niedrig. `ILogger<T>` ist additiv. Tests mit `NullLogger<T>.Instance`
sind null-Impact. *Einziger Risiko-Punkt:* die Composition-Root-Factory
in `Program.cs` muss die Store-Logger-Verkabelung sauber auflösen, ohne
dass der Server-Modus anders konstruiert wird als CLI-Modi. Planer-Step
muss das im Detail durchdenken.

---

## Definition of Done / Erfolgskriterien

**Bedingungslos (alle 5 Schritte):**

- [ ] `dotnet build -c Release` — 0 Warnungen, 0 Fehler
- [ ] `dotnet test` — alle bestehenden Tests grün (Baseline 55 vor diesem
      Task) **und** alle in den Fix-Details explizit genannten neuen Tests
      grün
- [ ] `AiNetLinter` (über den `AiNetLinterTests`-Test oder direkter CLI-Lauf)
      — 0 neue Verstöße
- [ ] Conventional Commits, deutscher Imperativ-Titel, `Co-Authored-By: Claude Sonnet 5`
- [ ] 5 Commits, einer pro Finding, in der empfohlenen Reihenfolge
- [ ] Doku-Updates in `docs/02`, `docs/03`, `docs/04` angepasst, **im selben
      Commit** wie der jeweilige Code (per `.agents/rules/05-documentation.mdc`)
- [ ] `appsettings.json` um `KnowHowToAi.Search.{MaxQueryLength,MaxResults}`
      erweitert
- [ ] Roadmap-Checkliste `docs/05-Roadmap.md` bleibt unverändert (Prio-A-
      Fixes sind Architektur-/Security-Verbesserungen, keine neuen
      Roadmap-Punkte) — *keine* Aktualisierung erforderlich

**Bedingt (wenn SQL-Setup-Problem gelöst oder umgangen):**

- [ ] End-to-End-Smoke: `dotnet run --project src/KnowHowToAI.Cli -- validate`
      → `import` gegen `demo-docs` → `server` gestartet →
      `list_children` / `search_docs` (z. B. `query="routing"`) /
      `get_doc(slug="...")` liefern echte Treffer aus den demo-docs

**Bekannter Vorbehalt:** docs/03 Abschnitt 2 dokumentiert ein
SQL-Setup-Problem auf dem Dev-Rechner. Solange das offen ist, wird der
End-to-End-Smoke im `task-summary.md` *dokumentiert und begründet
übersprungen* — das blockiert das Task-Ende **nicht**. Sobald das
Setup-Problem gelöst ist, kann der Smoke nachgeholt werden (ein
zusätzlicher Commit, nicht Teil dieses Tasks).

## Offene Punkte

*Keine.* Alle Klärungspunkte (DoD-Strenge, Schritt-Schnitt für F-AR-002)
sind im Dialog mit dem Nutzer geklärt (2026-07-26).
