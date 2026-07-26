---
status: done (pending audit)
type: step-review
task: audit-2026-07-24-PrioA
step: 003
reviewed_by: auditer
reviewed_by_model: MiniMax-M3
reviewed_by_model_knowledge_cutoff: 2026-01
reviewed_at: 2026-07-26T21:30:00+02:00
verdict: approved
---

# Review Step 003: F-PE-002 — `search_docs` mit TOP-Cap, Title-Ranking und Truncation-Marker fürs LLM

## Verdict

- [x] **approved** — alle drei Prüfebenen ok
- [ ] **issues** — Fix-Step `step-003/fix-XX/` angelegt mit Fix-Plan
- [ ] **blocked** — Nutzer-Entscheidung nötig (siehe Frage unten)

**Kern-Anforderung des Fixes erfüllt:** `DocsMcpTools.SearchDocsAsync` reicht `return result;` durch (nicht `result.Results`) — der `truncated`-Marker kommt beim LLM an.

## Geprüft

- [x] Plan-Erfüllung: alle 9 im `step-plan.md` genannten Änderungen erfolgt
- [x] Rules-Konformität: `.agents/rules/**` eingehalten (siehe Details)
- [x] Logische Korrektheit: Code macht was er soll, Tests decken Verhalten ab
- [x] Build: selbst nachgeprüft, grün (0 Warnings, 0 Errors)
- [x] Tests: selbst nachgeprüft, grün (78/78, Baseline 72 + 6 neue)
- [x] Lint: AiNetLinter-Report direkt gelesen, 0 Violations

## Befund

### Plan-Erfüllung

| # | Plan-Punkt | Status | Evidenz |
|---|---|---|---|
| 1 | `SearchResult.cs` (neu) — `sealed record SearchResult(IReadOnlyList<DocumentSummary>, bool Truncated)` | ✅ erfüllt | `src/KnowHowToAI.Core/Documents/SearchResult.cs:3-5` |
| 2 | `SearchDocsAsync` Signatur + SQL + Dapper-Mapping + Result-Konstruktion | ✅ erfüllt | `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:79-110` |
| 3 | `ResponseSize.cs` neuer Switch-Arm `SearchResult search => search.Results.Count` | ✅ erfüllt | `src/KnowHowToAI.Core/Logging/ResponseSize.cs:8` |
| 4 | `DocsMcpTools` Konstruktor + Return-Typ `Task<SearchResult>` + `return result;` | ✅ erfüllt | `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:12,24,27,29` |
| 5 | `Program.cs` Factory-Update für `DocsMcpTools` (jetzt 4 Parameter) | ✅ erfüllt | `src/KnowHowToAI.Cli/Program.cs:131-135` (3 `GetRequiredService`-Aufrufe) |
| 6 | `docs/02` Abschnitt 4.D — search_docs-Block | ✅ erfüllt | `docs/02-Architektur-und-Techstack.md:112-116` (Response-Shape, Title-Ranking, deterministische Sortierung, Verweis auf 04) |
| 7 | `docs/04` Abschnitt 1 — SQL + 4 Erklärpunkte | ✅ erfüllt | `docs/04-Datenmodell-Validierung-Edgecases.md:40-57` (SQL, TOP, COUNT(*) OVER(), Title-Ranking, Response-Shape) |
| 8 | `SearchResultTests` (neu) — 4 Verhaltensweisen | ✅ erfüllt (abweichende Struktur, s.u.) | `tests/KnowHowToAI.Core.Tests/Documents/SearchResultTests.cs:7-30` |
| 9 | `ResponseSizeTests` erweitert — 2 Tests für neuen Switch-Arm | ✅ erfüllt (abweichende Struktur, s.u.) | `tests/KnowHowToAI.Core.Tests/ResponseSizeTests.cs:67-77` |

**Coder-Abweichungen explizit geprüft:**

**(a) `SearchResultTests` als 2 Methoden statt 4 `[Fact]`-Cases:**
- 1 `[Theory]` mit 3 `[InlineData]` deckt ab: `(2,5,true)`, `(3,3,false)`, `(0,0,false)` — also Truncated=true, Truncated=false, Empty-Results.
- 1 `[Fact]` `SearchResult_PositionalRecord_BoolPropertySupportsValueEquality` deckt die Value-Semantics ab.
- Alle 4 geplanten Verhaltensweisen abgedeckt. Methoden-Anzahl 2 (≤ 5, AiNetLinter-Limit für `MiddleManMinMemberCount`). ✅
- Lesson aus fix-01 sauber umgesetzt.

**(b) `ResponseSizeTests` als 1 `[Theory]` mit 2 `[InlineData]`:**
- Coverage identisch zu den 2 geplanten `[Fact]`-Methoden (3 Items + 0 Items).
- Methoden-Anzahl der gesamten Klasse: jetzt 7 (6 Facts + 1 Theory). Der AiNetLinter hat nicht gemeckert (Report OK), weil jeder Test eine andere Beobachtung/anderen Code-Pfad testet — keine `MiddleMan`-Struktur. ✅
- Lesson aus fix-01 sauber umgesetzt.

**(c) Value-Equality-Test angepasst — geteilte Listen-Referenz statt zweier separater Listen:**
- `IReadOnlyList<DocumentSummary>` hat per Default Reference-Equality. Eine `Assert.Equal(first, second)` mit verschiedenen Referenzen würde fehlschlagen.
- Der Plan wollte "Wert-Type-Semantik für die `bool`-Property" — der Test teilt *eine* Listen-Referenz und prüft, dass `SearchResult(gleiche Liste, Truncated: false) == SearchResult(gleiche Liste, Truncated: false)` gilt. Das verifiziert genau das, was der Plan meinte: positional-record-Equality funktioniert auf `bool`-Property-Ebene. ✅
- Aussagekraft bleibt erhalten (Smoke-Test auf positional-record-Verhalten für die einfache Property), auch wenn die Test-Form nicht "zwei strukturgleiche Instanzen" ist.

**(d) Backticks im Commit-Body verloren (PowerShell-Parsing-Issue):**
- `git show c90e4c4` zeigt `` `truncated` `` als `truncated` und `` `SearchResult` `` als `SearchResult` im Body. Im manuell editierten `step-result.md` sind die Backticks erhalten.
- Die Aussage ist ohne Backticks verständlich (Kontext macht klar, dass es Code-Identifier sind). Subject und Trailer korrekt.
- Stilistisch inkonsistent, inhaltlich kein Problem. Repo-Regel "neue Commits statt --amend" spricht gegen --amend-Fix.
- **Wertung:** Beobachtung, kein Issue. Coder hat es transparent dokumentiert.

### Rules-Konformität

| Regel | Status | Evidenz |
|---|---|---|
| `01-code-style.mdc`: `sealed`, Early Returns, keine Kommentare | ✅ | `SearchResult` ist `sealed record` positional, keine Methoden, keine Kommentare. `SearchDocsAsync` Early Returns für leere Query und Längen-Validierung, dann SQL. `ResponseSize` Switch-Expression konsistent. `DocsMcpTools` Konstruktor 4 Parameter (≤ 5 OK) |
| `02-testing.mdc`: Tests im selben Commit wie Code | ✅ | `c90e4c4` enthält `SearchResultTests.cs` (neu) + `ResponseSizeTests.cs` (erweitert) — `git show c90e4c4 --stat` bestätigt |
| `03-git-workflow.mdc`: Conventional Commit, deutsch, Imperativ | ✅ | Subject `fix(perf): search_docs mit top-cap, title-ranking und truncation-marker` (59 Zeichen, ≤ 72). Body erklärt Warum. Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` vorhanden. ⚠️ Backticks im Body durch PowerShell-Parsing verloren (stilistisch, kein Inhaltsproblem) |
| `05-documentation.mdc`: Doku im selben Commit wie Code | ✅ | `docs/02-Architektur-und-Techstack.md` + `docs/04-Datenmodell-Validierung-Edgecases.md` in `c90e4c4` (siehe `--stat`) |
| `06-configuration.mdc`: keine Magic-Werte im Code | ✅ | `MaxResults` aus `KnowHowToAiOptions.Search.MaxResults` via DI injiziert (`Program.cs:134`). Im SQL: `@MaxResults` als Parameter, kein Literal. |
| `AiNetLinter.mdc`: Methoden ≤ 60 LOC, sealed, etc. | ✅ | `SearchResultTests` 2 Methoden (≤ 5). `ResponseSizeTests` 7 Methoden (jede testet anderes Verhalten, AiNetLinter OK). `SqlDocumentsStore.SearchDocsAsync` Z. 79-110 = 32 LOC, ≤ 60. `SearchResult` sealed. `SearchRow` private sealed. |

**AiNetLinter-Lint-Report direkt gelesen** (`tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md`):

```
# Run: 2026-07-26 19:13:37
OK
```

→ **0 Violations** gemeldet. Lint-Test `AiNetLinterTests.LintRun_ReportsNoViolations` ist grün (1/1).

### Logische Korrektheit

**Kern-Anforderung verifiziert — der `truncated`-Marker kommt beim LLM an:**
- `DocsMcpTools.SearchDocsAsync` (`src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:24,29`):
  ```csharp
  public async Task<SearchResult> SearchDocsAsync(...)
  {
      ...
      var result = await store.SearchDocsAsync(query, maxQueryLength, maxResults, cancellationToken);
      ...
      return result;  // ← Wrapper, nicht result.Results
  }
  ```
- Der Rückgabetyp ist `Task<SearchResult>`, nicht `Task<IReadOnlyList<DocumentSummary>>`. MCP-SDK serialisiert `SearchResult` mit beiden Properties (`results` + `truncated`). Das LLM sieht beide. ✅

**SQL-Korrektheit per Hand nachgerechnet:**

```sql
SELECT TOP (@MaxResults) slug AS Slug, title AS Title,
       COUNT(*) OVER() AS TotalCount
FROM dbo.<DocumentsTableName>
WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
ORDER BY
    CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,
    title;
```

- `TOP (@MaxResults)` schützt das Token-Budget ✅
- `COUNT(*) OVER()` Window-Function liefert TotalCount in *einer* Query (kein Race-Condition-Risiko) ✅
- `ORDER BY (CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END), title` — Title-Treffer zuerst (Bucket 0), dann alphabetisch (Bucket 1) ✅
- Alle 4 dokumentierten Spalten in der WHERE-Klausel ✅
- Edge-Case `MaxResults=0`: SQL liefert 0 Rows, `rowList.Count == 0` → `totalCount = 0`, `Truncated = (0 > 0) = false`. Akzeptabel, keine Sonderbehandlung nötig. ✅

**Truncated-Ableitung per Hand verifiziert:**
- Code (`SqlDocumentsStore.cs:105-108`):
  ```csharp
  var rowList = rows.AsList();
  var results = rowList.Select(r => new DocumentSummary(r.Slug, r.Title)).ToList();
  var totalCount = rowList.Count > 0 ? rowList[0].TotalCount : 0;
  return new SearchResult(results, Truncated: totalCount > results.Count);
  ```
- Edge-Case: `rowList.Count == 0` würde `rowList[0].TotalCount` zu `IndexOutOfRangeException` führen — durch `rowList.Count > 0 ? ... : 0` korrekt abgefangen. ✅

**`ResponseSize.Measure<T>` Switch-Reihenfolge verifiziert:**
```csharp
IReadOnlyCollection<DocumentSummary> summaries => summaries.Count,  // (1)
SearchResult search => search.Results.Count,                          // (2)
DocumentDetail detail => detail.Content?.Length ?? 0,                 // (3)
null => 0,                                                            // (4)
_ => 0,                                                               // (5)
```
- Wichtige Frage: würde Arm (1) für eine `SearchResult`-Instanz greifen? Nein — `SearchResult` ist nicht selbst `IReadOnlyCollection<DocumentSummary>`, sondern *hat* eine `Results`-Property, die `IReadOnlyList<DocumentSummary>` ist. Ein Switch-Pattern-Match prüft die *Instanz*, nicht ihre Members. Also greift Arm (2) korrekt. ✅
- Ohne Arm (2) würde Arm (5) `_ => 0` greifen — der `search_docs`-Logeintrag würde `Size=0` zeigen, irreführend. Mit Arm (2) wird `Results.Count` geloggt. ✅

**Tests per Hand nachgerechnet (gegen Plan-Erwartungen):**

| Test | Eingabe | Erwartet | Tatsächlich (verifiziert) |
|---|---|---|---|
| `[InlineData(2, 5, true)]` | results=2, totalCount=5 | Truncated=true | `(5 > 2) = true` ✅ |
| `[InlineData(3, 3, false)]` | results=3, totalCount=3 | Truncated=false | `(3 > 3) = false` ✅ |
| `[InlineData(0, 0, false)]` | results=0, totalCount=0 | Truncated=false | `(0 > 0) = false` ✅ |
| `Measure_SearchResult [InlineData(3)]` | 3 Items | 3 | `search.Results.Count = 3` ✅ |
| `Measure_SearchResult [InlineData(0)]` | 0 Items | 0 | `search.Results.Count = 0` ✅ |
| `SearchResult_PositionalRecord_BoolPropertySupportsValueEquality` | gleiche Liste, Truncated=false | equal | `Assert.Equal(first, second)` ✅ |

**Adversarial Probes:**

1. **`MaxResults=0`-Edge-Case:** SQL `TOP 0` liefert 0 Rows. `rowList.Count == 0` → `totalCount = 0`, `Truncated = (0 > 0) = false`. Konsistent mit der Plan-Notiz. ✅
2. **Leere DB:** SQL liefert 0 Rows, gleiche Logik. ✅
3. **Genau 50 Treffer bei `MaxResults=50`:** SQL liefert 50 Rows, `TotalCount=50`, `Truncated = (50 > 50) = false`. Korrekt. ✅
4. **Versteckte Aufrufer von `SearchDocsAsync`:** Volltextsuche im Code (`grep "SearchDocsAsync"`) findet nur `DocsMcpTools.cs:27` (aktualisiert) und `SqlDocumentsStore.cs:79` (Definition). Keine Tests, keine Service-Delegation. API-Bruch bleibt lokal. ✅
5. **`SearchResult` vs. `IReadOnlyCollection<DocumentSummary>` Switch-Konflikt:** Pattern-Match prüft die Instanz, nicht Members. `SearchResult` ist *kein* `IReadOnlyCollection<DocumentSummary>`. Arm (2) greift korrekt. ✅
6. **Backtick-Verlust im Commit-Body:** `git show c90e4c4` bestätigt: `` `truncated` `` → `truncated`, `` `SearchResult` `` → `SearchResult`. Inhaltlich verständlich, stilistisch inkonsistent. Coder hat es transparent dokumentiert, Repo-Regel "neue Commits statt --amend" spricht gegen Fix.

### Build-Status

```
dotnet build -c Release
→ KnowHowToAI.Core → bin/Release/net10.0/KnowHowToAI.Core.dll
→ KnowHowToAI.Core.Tests → bin/Release/net10.0/KnowHowToAI.Core.Tests.dll
→ KnowHowToAI.Cli → bin/Release/net10.0/KnowHowToAI.Cli.dll
Der Buildvorgang wurde erfolgreich ausgeführt.
    0 Warnung(en)
    0 Fehler
```

### Test-Status

```
dotnet test -c Release --no-build
→ Testlaufzusammenfassung: Bestanden!
  gesamt: 78
  fehlgeschlagen: 0
  erfolgreich: 78
  übersprungen: 0
  Dauer: 10s 991ms

AiNetLinter-Test (gefiltert):
→ gesamt: 1, fehlgeschlagen: 0, erfolgreich: 1
```

**AiNetLinter-Report direkt gelesen** (nicht nur Test-Exit-Code vertraut — Lesson aus fix-01):

```
$ Get-Content tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md
# Run: 2026-07-26 19:13:37
OK
```

→ **0 Violations** gemeldet.

## Findings (bei `issues`)

Keine.

## Frage an Nutzer (bei `blocked`)

Keine.

## Sonstige Beobachtungen (nicht als Issues zu werten)

1. **Backticks im Commit-Body verloren** (PowerShell-Parsing-Issue): `git show c90e4c4` zeigt `truncated` statt `` `truncated` `` und `SearchResult` statt `` `SearchResult` ``. Im manuell editierten `step-result.md` sind die Backticks erhalten. Subject und Trailer korrekt. Coder hat es transparent dokumentiert, Repo-Regel "neue Commits statt --amend" spricht gegen --amend-Fix. Stilistisch inkonsistent, inhaltlich kein Problem. Kann in einem separaten `chore`-Commit korrigiert werden, falls gewünscht.

2. **`ResponseSizeTests`-Klasse hat jetzt 7 Methoden** (6 Facts + 1 Theory). Die `MiddleManMinMemberCount: 5`-Grenze aus AiNetLinter bezieht sich auf reine Forwarding-Klassen ohne eigene Logik. Hier testet jede Methode einen anderen Code-Pfad im Switch-Expression — AiNetLinter hat nicht gemeckert (Report OK). Strukturell sauber.

3. **Value-Equality-Test mit geteilter Listen-Referenz:** Test verifiziert positional-record-Equality auf der `bool`-Property-Ebene. Da `IReadOnlyList<>` Default-Reference-Equality hat, ist die einzige korrekte Test-Form die geteilte Referenz. Alternativ wäre `Assert.Equal(first.Truncated, second.Truncated)` plus separate Referenz-Identity-Prüfung — der gewählte Ansatz ist kompakter und testet das gleiche Verhalten.

4. **Plan-Test-Baseline vs. Coder-Baseline:** Plan sagt "68 → 74 grün", Coder-Result sagt "72 → 78 grün" (Baseline 72 statt 68). Differenz: 4 zusätzliche Tests aus dem vorangegangenen Step 002-Build, die in der Plan-Erwartung nicht eingerechnet waren. Kein Konflikt mit dem Plan — der Plan schätzt die Baseline konservativ. ✅

5. **`SearchResult` mit `IReadOnlyList<DocumentSummary>`-Property:** AiNetLinter `EnforceSealedClasses` greift, positional record ist sauber. Keine Warnung. Wert-Gleichheit funktioniert nur bei gleicher Listen-Referenz — semantisch okay, weil `SearchResult` als Antwort-Container nie für Equality-Vergleiche zwischen logisch-gleichen aber referenz-verschiedenen Instanzen benutzt wird.
