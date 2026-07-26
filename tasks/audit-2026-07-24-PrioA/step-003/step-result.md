---
status: done
type: step-result
task: audit-2026-07-24-PrioA
step: 003
coded_by: coder
coded_by_model: MiniMax-M3
coded_by_model_knowledge_cutoff: 2026-01
coded_at: 2026-07-26T19:15:00+02:00
code_commit_hash: c90e4c4f77566d74d054613d1f9e5780cd62901b
# Hinweis: den Commit, der DIESE Datei enthält (Coder-Skill Schritt 7),
# kann diese Datei denknotwendig nicht selbst zitieren — bei Bedarf per
# `git log --follow -- <Pfad-dieser-Datei>` nachschlagen.
status_after: done
---

# Result Step 003: F-PE-002 — `search_docs` mit TOP-Cap, Title-Ranking und Truncation-Marker fürs LLM

## Zusammenfassung

`SqlDocumentsStore.SearchDocsAsync` nutzt jetzt `TOP (@MaxResults)` +
`COUNT(*) OVER() AS TotalCount` + Title-Ranking via
`ORDER BY (CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END), title`. Der
neue Core-Record `SearchResult(IReadOnlyList<DocumentSummary>, bool Truncated)`
ist der neue Rückgabetyp; `Truncated` wird in derselben Query aus
`TotalCount > Results.Count` abgeleitet (kein Race-Condition-Risiko zwischen
zwei Round-Trips). `ResponseSize.Measure` erkennt `SearchResult` und misst
`Results.Count`. `DocsMcpTools.SearchDocsAsync` reicht `result` (den Wrapper)
als Tool-Antwort durch, **nicht** `result.Results` — der `truncated`-Marker
kommt damit beim LLM an. Konstruktor um `int maxResults` erweitert (4
Parameter, AiNetLinter-Limit 5), `Program.cs RunServer`-Factory entsprechend
angepasst. Produktdoku in `docs/02` Abschnitt 4.D und `docs/04` Abschnitt 1
mit den neuen Query- und Shape-Details aktualisiert.

## Geänderte Dateien

- `src/KnowHowToAI.Core/Documents/SearchResult.cs` (neu) — `sealed record SearchResult(IReadOnlyList<DocumentSummary> Results, bool Truncated)` als neuer Core-Domain-Record.
- `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs` — `SearchDocsAsync` um `int maxResults` erweitert, Rückgabetyp `Task<SearchResult>`, SQL auf neuen Shape umgestellt (`TOP (@MaxResults)`, `COUNT(*) OVER() AS TotalCount`, Title-Ranking), neuer privater `SearchRow`-Record für Dapper-Mapping, `Truncated` aus `totalCount > results.Count` abgeleitet.
- `src/KnowHowToAI.Core/Logging/ResponseSize.cs` — neuer Switch-Arm `SearchResult search => search.Results.Count` vor `_ => 0` eingefügt, damit der `search_docs`-Logeintrag nicht irreführend `Size=0` zeigt.
- `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs` — Konstruktor-Primary um `int maxResults` erweitert (jetzt 4 Parameter: `store`, `maxQueryLength`, `maxResults`, `logger`), `search_docs`-Tool-Rückgabetyp auf `Task<SearchResult>`, `return result;` statt `return result.Results;`.
- `src/KnowHowToAI.Cli/Program.cs` — `RunServer`-Factory für `DocsMcpTools` um `options.Search.MaxResults` als dritten Argument erweitert (notwendig, sonst kompiliert der Build nicht).
- `docs/02-Architektur-und-Techstack.md` — `search_docs`-Block in Abschnitt 4.D auf neuen Response-Shape `{ results, truncated }` aktualisiert, Hinweis auf Title-Ranking und deterministische Sortierung ergänzt, Verweis auf `04` Abschnitt 1.
- `docs/04-Datenmodell-Validierung-Edgecases.md` — `search_docs`-SQL-Listing in Abschnitt 1 komplett auf neuen Shape umgestellt, Erklärungen zu `TOP (@MaxResults)`, `COUNT(*) OVER()`, Title-Ranking und `Truncated`-Ableitung ergänzt, Verweis auf `02` Abschnitt 4.D.
- `tests/KnowHowToAI.Core.Tests/Documents/SearchResultTests.cs` (neu) — 2 Testmethoden: `[Theory]` mit 3 `[InlineData]`-Cases für die `Truncated`-Ableitung aus `(results.Count, totalCount)`, plus 1 `[Fact]` für positional-record-Wert-Gleichheit der `bool`-Property. Lesson aus fix-01: `[Theory]+[InlineData]` statt vieler `[Fact]`.
- `tests/KnowHowToAI.Core.Tests/ResponseSizeTests.cs` — 1 `[Theory]` mit 2 `[InlineData]`-Cases für `Measure(SearchResult)` (3 Items bzw. leer) hinzugefügt. Lesson aus fix-01: `[Theory]+[InlineData]` für 2 Varianten desselben Switch-Arms.

## Commit

- **Code-Commit-Hash:** `c90e4c4f77566d74d054613d1f9e5780cd62901b`
- **Message:**
  ```
  fix(perf): search_docs mit top-cap, title-ranking und truncation-marker

  Verhindert Token-Budget-Sprengung bei breiten Suchen via
  TOP(@MaxResults) und gibt dem LLM via `truncated`-Marker in der
  Antwort die Möglichkeit, eine gekappte Trefferliste zu erkennen
  und die Suche zu verfeinern. Title-Ranking verbessert die
  Treffer-Reihenfolge. `SearchResult` ist der neue Response-Shape,
  `ResponseSize.Measure` erkennt ihn. API-Bruch am
  `SearchDocsAsync`-Rückgabetyp bewusst akzeptiert (kein
  Migrationspfad - v1.0.2-Tool, LLMs lesen Description neu).

  Refs: tasks/audit-2026-07-24-PrioA/step-003
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  ```
- **Branch:** main
- **Push:** nein (lokal)
- **Doku-Commit:** separater, zweiter Commit für diese Datei +
  `step-plan.md`-Status (siehe Coder-Skill Schritt 7) — dessen Hash steht
  nicht hier drin (Selbstbezug), sondern in `git log`.

## Build-Output

```
dotnet build -c Release
→ Ergebnis: grün — 0 Warnungen, 0 Fehler
```

## Test-Output

```
dotnet test -c Release
→ Ergebnis: grün — 78 Tests, 0 fehlgeschlagen, 0 übersprungen
→ Anzahl Tests: 78 (Baseline 72 + 6 neue: 4 in SearchResultTests, 2 in ResponseSizeTests)
```

AiNetLinter-Test (`AiNetLinterTests.LintRun_ReportsNoViolations`) explizit
ausgeführt — Exit 0, Report unter
`tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md` enthält
nur `OK` (0 Violations). **Nicht nur Test-Exit-Code vertraut** (Lesson aus
fix-01).

## Abweichungen vom Plan

- **`SearchResultTests` mit 2 Methoden statt 4 `[Fact]`-Methoden:** Plan
  listet 4 `[Fact]`-Cases (3 Truncated + 1 Value-Semantics). Lektion aus
  fix-01 war, `[Theory]+[InlineData]` zu bevorzugen. Umgesetzt: 1
  `[Theory]` mit 3 `[InlineData]` für die 3 Truncated-Varianten + 1
  `[Fact]` für Value-Semantics (anderes Verhalten, nicht in dieselbe
  Theory passend). Alle 4 geplanten Verhaltensweisen bleiben abgedeckt.
- **`ResponseSizeTests`-Erweiterung als `[Theory]+[InlineData]` statt 2
  `[Fact]`:** Plan listet 2 `[Fact]`-Cases. Lektion aus fix-01: für
  Varianten desselben Switch-Arms bevorzugt `[Theory]+[InlineData]`.
  Umgesetzt: 1 `[Theory]` mit 2 `[InlineData]` (3 Items bzw. leer). Gleiche
  Coverage, weniger Test-Methoden.
- **Value-Equality-Test angepasst:** Erst-Draft testete zwei
  `SearchResult`-Instanzen mit *verschiedenen* `IReadOnlyList`-Referenzen
  und scheiterte, weil `IReadOnlyList<DocumentSummary>` Default-Reference-
  Equality hat. Korrigiert: Test teilt jetzt *eine* Listen-Referenz und
  prüft die positional-record-Wert-Gleichheit für die `bool`-Property —
  semantisch das, was der Plan unter „Wert-Type-Semantik für `bool`-Property"
  meinte. Name des Tests angepasst von
  `IsSealedRecord_ValueSemanticsForBoolProperty` auf
  `SearchResult_PositionalRecord_BoolPropertySupportsValueEquality`, um
  den engen Scope klarzustellen.

## Beobachtungen

- **`SearchRow` als Dapper-Mapping-Type:** `private sealed record SearchRow(string Slug, string Title, int TotalCount)` neben dem bestehenden `DocumentRow` — sauberes Pattern, hält den Row-Type in der Klasse
  gekapselt (kein Core-Leak).
- **AiNetLinter-Limit für `DocsMcpTools`-Konstruktor:** jetzt 4 Parameter,
  Limit ist 5. Step 005 plant die Factory-Konsolidierung, in der ein
  `ILogger<>`-Wiring dazukommen könnte — dann wären es 5 und exakt am
  Limit. Falls ein weiterer Parameter dazukommt, müsste ein
  Parameter-`record` her (laut AiNetLinter.mdc `MaxMethodParameterCount: 4`
  — gilt das auch für Konstruktoren? Diese Frage ist im aktuellen Scope
  nicht zu beantworten, AiNetLinter hat nicht gemeckert).
- **`MaxResults=0`-Edge-Case:** Wenn `MaxResults=0` in `appsettings.json`
  gesetzt wird, liefert die Query 0 Rows → leere Results-Liste, `Truncated
  = (0 > 0) = false`. Akzeptables Verhalten, keine Sonderbehandlung nötig
  (Plan-Notiz).
- **API-Bruch `Task<IReadOnlyList<DocumentSummary>>` → `Task<SearchResult>`:**
  Es gibt aktuell keine direkten Aufrufer außer `DocsMcpTools`, also
  bleibt der Bruch lokal. Tests, die die alte Signatur erwartet hätten,
  existieren nicht (DB-abhängig, Backlog F-TS-001).
- **`COUNT(*) OVER()` mit leerem Result-Set:** Edge-Case, der im
  Plan-Code-Sketch korrekt gehandhabt wird (`rowList.Count > 0 ?
  rowList[0].TotalCount : 0`). Bei `MaxResults=0` oder keinem Match liefert
  SQL keine Rows, also kein `TotalCount` in der Antwort; Code greift
  sauber auf `0` zurück → `Truncated = false`. Konsistent.
- **`SearchResult` als positional `sealed record` mit
  `IReadOnlyList<DocumentSummary>`:** AiNetLinter hat keine Warnung
  produziert. `EnforceSealedClasses` greift. Wert-Gleichheit zwischen
  Records funktioniert nur, wenn die `Results`-Liste dieselbe Referenz
  ist — semantisch okay, weil `SearchResult` als reiner Antwort-Container
  nie für Equality-Vergleiche zwischen logisch-gleichen aber
  referenz-verschiedenen Instanzen benutzt wird.

## Bekannte Unschärfen

- **`SqlDocumentsStore.SearchDocsAsync` direkte Tests:** in diesem Step
  *nicht* möglich (DB-abhängig, Backlog F-TS-001). Die `Truncated`-
  Ableitungs-Logik ist über `SearchResultTests` indirekt abgesichert; die
  `SearchRow`-Mapping- und `TOP`/`COUNT(*) OVER()`-Pfade sind
  ausschließlich manuell verifizierbar (Smoke-Test gegen echten
  SQL-Server). Auditer sollte die SQL gegen eine Test-Instanz
  nachvollziehen — kein isolierter Test vorhanden.
- **Backticks im Commit-Body:** PowerShell-Parsing hat die `` ` ``-Ticks
  im Multiline-Commit-Body rausgefiltert (`truncated` statt `` `truncated` ``,
  `SearchResult` statt `` `SearchResult` ``). Inhaltlich unverständlich,
  aber stilistisch inkonsistent zur Repo-Konvention (Code-Identifiers
  sollten in Backticks stehen). Commit-Subject und Trailer korrekt; nur
  im Body fehlen die Ticks an 2 Stellen. **Hinweis für Auditer:** Falls
  das stört, kann das in einem separaten `chore`-Commit korrigiert
  werden, ohne den Code-Commit umzuschreiben (Repo-Regel
  „neue Commits statt --amend").
