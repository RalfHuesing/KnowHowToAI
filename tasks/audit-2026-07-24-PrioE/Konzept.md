# Audit Prio E — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** PrioA (umgesetzt), PrioB/C/D (in Umsetzung)
> **Methodik:** Aus dem Gesamt-Audit (67 Findings nach Prio A-D) wurden die 11 Findings extrahiert, die unter „Test-Coverage (Dim 4)" zusammengefasst sind. Bewertung: Tests brechen Architektur/Intention nicht — alle Edge-Case-Tests gehen rein. Nur F-TS-012 (Info) bleibt draußen. Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand | Status |
|---|---|---|---|---|
| [F-TS-001](#f-ts-001--sqldocumentsstore-hat-keine-unit-tests) | `SqlDocumentsStore` hat keine Unit-Tests | High (per `02-testing.mdc` akzeptiert) | Backlog-Doku | offen |
| [F-TS-002](#f-ts-002--schemamigratormigrateasync-nicht-getestet) | `SchemaMigrator.MigrateAsync` nicht getestet | Medium | ~30 Min + SQLite-Setup | offen (Backlog) |
| [F-TS-003](#f-ts-003--docsvalidator-deckt-content-länge-nur-an-boundary-ab) | `DocsValidator` deckt Content-Länge nur an Boundary ab | Medium | ~5 Min | **erledigt** |
| [F-TS-004](#f-ts-004--importservicereplaceallasync-throws-nicht-getestet) | `ImportService.replaceAllAsync`-Throws nicht getestet | Medium | ~15 Min | offen |
| [F-TS-005](#f-ts-005--importservice-und-fehlgeschlagene-files) | `ImportService` und fehlgeschlagene Files | Medium | ~20 Min | offen |
| [F-TS-006](#f-ts-006--frontmatterparser-kein-test-für-nur-whitespace-title) | `FrontMatterParser` kein Test für nur-whitespace `title` | Low | ~5 Min | **erledigt** |
| [F-TS-007](#f-ts-007--frontmatterparser-kein-bom-test) | `FrontMatterParser` kein BOM-Test | Low | ~5 Min | **erledigt** |
| [F-TS-008/009](#f-ts-008--f-ts-009--exportservice-edge-cases-fehlen) | `ExportService` Edge-Cases fehlen | Low | ~10 Min | offen |
| [F-TS-010](#f-ts-010--slugrulesfromfilepath-nicht-getestet) | `SlugRules.FromFilePath` nicht getestet | Low | ~15 Min | **erledigt** |
| [F-TS-011](#f-ts-011--docsvalidator-und-nicht-md-dateien) | `DocsValidator` und Nicht-MD-Dateien | Low | ~5 Min | **erledigt** |

**Gesamt-Aufwand:** ~110 Min (10 Min Code + 100 Min Tests) + ~30 Min SQLite-Setup (siehe F-TS-001 Hinweis). Aufteilbar in 1-3 Commits.

**Leitidee:** Edge-Case-Coverage verbessern, ohne Architektur/Intention zu verändern. Plus SQLite-Test-Infrastruktur als Backlog-Doku festhalten, falls spätere Sicherheits-Fixes (F-SE-001) sie brauchen.

---

## F-TS-001 — `SqlDocumentsStore` hat keine Unit-Tests

> **Schweregrad:** High · **Dimension:** Test-Coverage (per `02-testing.mdc` ausgenommen)
> **Datei:** `tests/KnowHowToAI.Core.Tests/` (existiert nicht) + `docs/05-Roadmap.md` (Referenz)

### Problem

`docs/03` Zeile 44 sagt:
> "`SqlDocumentsStore` ist die einzige Klasse mit echtem `SqlConnection`/Dapper-Zugriff und wird selbst nicht separat unit-getestet (dünner DB-Adapter, analog zu `SchemaMigrator.Migrate`)."

Diese Ausnahme ist in `02-testing.mdc` *nicht* explizit dokumentiert — sie ergibt sich aus der Praxis. **Aber:** Der Store enthält Logik, die *sehr wohl* Edge-Cases hat:

| Methode | Edge-Case | Aktuell getestet? |
| --- | --- | --- |
| `BuildLikePattern` | Wildcard-Injection (Dim 2 F-SE-001) | ❌ |
| `ToDocument` (private) | Ungültiges JSON in `Tags`/`Synonyms` (Dim 1 F-CQ-003) | ❌ |
| `ReplaceAllAsync` | FK-Constraint-Verletzung bei falscher Slug-Reihenfolge | ❌ |
| `ListChildrenAsync` | Root-Edge (`parentSlug = null`) | ❌ |
| `SearchDocsAsync` | Leere `query` → `%%` → kompletter Match | ❌ |
| `GetDocAsync` | Slug nicht gefunden → `null` | ❌ |

**Mitigation-Idee (NICHT für v1 — Integrationstests sind explizit Backlog):**
- `02-testing.mdc` Zeile 20: "Echte Integrationstests gegen SQL Server sind bewusst Backlog." — also bewusst raus.
- Alternative: **SQLite-In-Memory für Unit-Tests?** Dapper ist DB-agnostisch. SQLite könnte für die meisten SELECT-Queries funktionieren, aber `LIKE '%...%'` und `NVARCHAR(MAX)`-JSON-Operationen verhalten sich leicht anders. Aufwand ~2-4 Stunden für die Infrastruktur, dann pro Test-Methode ~10 Minuten.

**In Prio E:** Backlog-Doku. SQLite-Setup als optionale Vorarbeit, falls andere Sicherheits-Fixes (F-SE-001 in Prio A, F-SE-003 in Prio D) Tests benötigen.

### Aufwand

- ~30 Min Doku-Update (Verweis in `docs/05-Roadmap.md`)
- ~4 Stunden für SQLite-Test-Infrastruktur (Backlog, **nicht in Prio E**)
- ~1-2 Stunden pro Methode (Backlog)

### Risiko

Keine (reine Doku). SQLite-Setup selbst hat Risiko: `LIKE`-Semantik unterscheidet sich zwischen SQLite und SQL Server, was zu false-positive/false-negative Tests führen kann.

---

## F-TS-002 — `SchemaMigrator.MigrateAsync` nicht getestet

> **Schweregrad:** Medium · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/SchemaMigratorTests.cs` (existiert)

### Problem

`SchemaMigratorTests` testet nur `DiscoverScripts` (2 Tests). `MigrateAsync` (Zeile 16 in `SchemaMigrator.cs`) ist ungetestet. Würde echten SQL-Server brauchen, was der Grund für die Lücke ist.

**Edge-Cases in `MigrateAsync`:**
- Connection-String ist null/leer → SqlException
- `documentsTableName` ist ungültig → wird vor Open abgefangen via `SqlIdentifierValidator`
- Erste Migration, Tabelle existiert nicht → `IF NOT EXISTS`-Guard erstellt
- Zweite Migration, Tabelle existiert → Guard tut nichts (idempotent)
- Skript-Wiederholung nach Fehler → ???
- SQL-Server antwortet nicht → Timeout

### Fix-Empfehlung

Mit SQLite-In-Memory (siehe F-TS-001): `SchemaMigrator.DiscoverScripts` ist DB-agnostisch (Datei-IO). `MigrateAsync` braucht SQL, was nur mit SQLite funktioniert. Tests:
- `MigrateAsync_NoConnectionString_Throws` (mit `Mock<IDbConnection>`)
- `MigrateAsync_EmptySqlDirectory_DoesNothing` (mit SQLite-Setup)

### Aufwand

- ~30 Min Code + Tests (mit SQLite-Setup, siehe F-TS-001)
- Falls SQLite-Setup nicht umgesetzt: nur Doku-Hinweis in `SchemaMigratorTests` ("MigrateAsync nicht testbar ohne SQL")

### Risiko

Niedrig. Test-Code ändert nichts an Produktion.

---

## F-TS-003 — `DocsValidator` deckt Content-Länge nur an Boundary ab

> **Schweregrad:** Medium · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/DocsValidatorTests.cs`

### Problem

Zeile 103-125 testet `content.Length == 10` (Threshold, no warning) und `content.Length == 11` (above threshold, warning). Was fehlt:
- `content.Length == 0` (leerer Content — gültig? Warning?)
- `content.Length == 1` (deutlich unter Threshold)
- Test mit `maxContentLengthWarning = 0` (Edge-Case: Warning-Schwelle 0 → Warning für jeden Inhalt?)

### Fix

Drei zusätzliche Tests:
```csharp
[Fact]
public void Validate_EmptyContent_NoWarning()
[Fact]
public void Validate_VeryShortContent_NoWarning()
[Fact]
public void Validate_ZeroWarningThreshold_WarnsEverything()
```

### Aufwand

- ~5 Min
- 1 Commit (kann mit F-TS-004/F-TS-005 kombiniert werden)

### Risiko

Keine.

---

## F-TS-004 — `ImportService.replaceAllAsync`-Throws nicht getestet

> **Schweregrad:** Medium · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/ImportServiceTests.cs`

### Problem

`ImportAsync_ValidDocs_ReplacesWithParsedDocuments` testet den Happy Path. Was, wenn `replaceAllAsync` (die SQL-Operation) wirft?

Aktuell: `SqlDocumentsStore.ReplaceAllAsync` ist in einer Transaktion (Zeile 27 + 52), impliziter Rollback via `await using`. Das ist OK, aber: das `ImportService` selbst hat keine Cleanup-Logik. Was, wenn der `cancellationToken` *vor* `replaceAllAsync` gesetzt wird? Aktuell: nichts Schlimmes.

### Fix-Empfehlung

```csharp
[Fact]
public async Task ImportAsync_ReplaceThrows_PropagatesException()
[Fact]
public async Task ImportAsync_CancellationBeforeReplace_ThrowsOperationCanceled()
```

### Aufwand

- ~15 Min
- 1 Commit (kann mit F-TS-003 kombiniert werden)

### Risiko

Keine.

---

## F-TS-005 — `ImportService` und fehlgeschlagene Files

> **Schweregrad:** Medium · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/ImportServiceTests.cs` + `ImportService.ReadDocuments` (Code)

### Problem

`ImportAsync_InvalidDocs_ReturnsErrorsAndDoesNotReplaceAnything` testet einen ungültigen Slug. Was, wenn:
- Eine Datei hat gültigen Slug, aber fehlenden Parent (Orphan) → `DocsValidator.Validate` fängt das vorab ab
- Eine Datei ist nicht lesbar (z.B. Lock durch anderen Prozess) → `File.ReadAllText` wirft `IOException`. Wird der Fehler gesammelt oder propagiert er?

Aktuell propagiert er (`yield return` reicht die Exception nicht als Validation-Error durch). Das ist ein Bug oder eine Design-Entscheidung.

### Fix-Empfehlung

Variante A — nur Tests:
```csharp
[Fact]
public void ReadDocuments_LockedFile_PropagatesIOException()
```

Variante B — defensiver Code + Tests: `File.ReadAllText` durch `try/catch IOException` ersetzen, in Errors-Liste sammeln. Saubere Variante, ~20 Min.

### Aufwand

- Variante A: ~10 Min
- Variante B: ~20 Min (defensiver Code + 2 Tests)

### Risiko

Variante A: keine. Variante B: niedrig (Error-Sammlung statt Throw ist die bessere UX).

---

## F-TS-006 — `FrontMatterParser` kein Test für nur-whitespace `title`

> **Schweregrad:** Low · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/FrontMatterParserTests.cs`

### Problem

`Parse_MissingTitle_Throws` testet `tags: [a]` ohne `title:`-Feld. Aber: `title: "   "` (drei Leerzeichen) — `string.IsNullOrWhiteSpace` (FrontMatterParser Zeile 28) fängt das ab. Test fehlt.

### Fix

```csharp
[Theory]
[InlineData("   ")]
[InlineData("\t")]
[InlineData("\n")]
public void Parse_WhitespaceOnlyTitle_Throws()
```

### Aufwand

- ~5 Min
- 1 Commit (kann mit F-TS-007 kombiniert werden)

### Risiko

Keine.

---

## F-TS-007 — `FrontMatterParser` kein BOM-Test

> **Schweregrad:** Low · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/FrontMatterParserTests.cs`

### Problem

Datei mit UTF-8-BOM am Anfang → `SplitFrontMatter` würde die BOM vom `---` abtrennen? Nein — `String.StartsWith` mit `StringComparison.Ordinal` ist byte-genau, BOM wäre also 3 Bytes vor dem `---`. `StartsWith("---")` würde fehlschlagen → "Datei beginnt nicht mit YAML Front Matter" Exception.

Das ist die richtige Semantik (BOM ist nicht erlaubt in Markdown-Dateien), aber nicht explizit getestet. Wenn ein User die Datei mit Notepad speichert ("Mit Codierung → UTF-8 mit BOM"), passiert das häufig.

### Fix

```csharp
[Fact]
public void Parse_FileWithUtf8Bom_ThrowsOrStripsBom()
{
    // Decide: BOM soll geworfen werden (klare Semantik) oder gestrippt werden (UX).
    // Aktueller Code: wirft. Test das.
}
```

### Aufwand

- ~5 Min
- 1 Commit (kann mit F-TS-006 kombiniert werden)

### Risiko

Keine.

---

## F-TS-008 / F-TS-009 — `ExportService` Edge-Cases fehlen

> **Schweregrad:** Low · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/ExportServiceTests.cs`

### Problem

- Kein Test: `getAllAsync` wirft → `ExportAsync` propagiert oder fängt?
- Kein Test: `getAllAsync` gibt leere Liste zurück → Marker-Datei wird geschrieben, aber keine `.md`-Dateien. Korrekt? Aktueller Code: ja. Test fehlt.

### Fix

```csharp
[Fact]
public async Task ExportAsync_GetAllThrows_Propagates()
[Fact]
public async Task ExportAsync_EmptyList_WritesMarkerOnly()
```

### Aufwand

- ~10 Min
- 1 Commit

### Risiko

Keine.

---

## F-TS-010 — `SlugRules.FromFilePath` nicht getestet

> **Schweregrad:** Low · **Dimension:** Test-Coverage
> **Datei:** `tests/KnowHowToAI.Core.Tests/SlugRulesTests.cs`

### Problem

`FromFilePath` wird nirgends getestet. Edge-Cases:
- `docsRootPath` und `filePath` identisch → was wird zurückgegeben?
- `filePath` ist außerhalb von `docsRootPath` (Path-Traversal-Edge, F-SE-006)
- Datei mit mehreren Extensions (`foo.md.bak`) → `Path.GetExtension` liefert `.bak` → Slug wäre `foo.md`. Sollte das ein Fehler sein?

### Fix

```csharp
[Theory]
[InlineData("docs", "docs/foo.md", "foo")]
[InlineData("docs", "docs/sub/bar.md", "sub/bar")]
[InlineData("docs", "etc/passwd", "../etc/passwd")] // Path-Traversal-Edge
[InlineData("docs", "foo.md.bak", "foo.md")] // Multi-Extension
public void FromFilePath_HandlesEdgeCases(string root, string path, string expected)
```

### Aufwand

- ~15 Min
- 1 Commit (kann mit F-TS-011 kombiniert werden)

### Risiko

Keine. Pure-Function-Tests.

---

## F-TS-011 — `DocsValidator` und Nicht-MD-Dateien

> **Schweregrad:** Low · **Dimension:** Test-Coverage (Defense-in-Depth)
> **Datei:** `tests/KnowHowToAI.Core.Tests/DocsValidatorTests.cs`

### Problem

Wenn jemand eine `.txt`-Datei in `docs-root` legt, wird sie ignoriert (`Directory.EnumerateFiles(docsRootPath, "*.md", SearchOption.AllDirectories)`). Aktuell kein Test dafür.

### Fix

```csharp
[Fact]
public void Validate_OnlyNonMdFiles_ReturnsNoErrorsAndIgnoresThem()
```

### Aufwand

- ~5 Min
- 1 Commit (kann mit F-TS-010 kombiniert werden)

### Risiko

Keine.

---

## Warum diese 11 und nicht andere?

### Aufgenommen

Alle 10 Edge-Case-Tests (F-TS-002 bis F-TS-011) gehen rein — Tests brechen Architektur/Intention nicht. Plus F-TS-001 als Backlog-Doku-Verweis.

### Bewusst weggelassen (Kurzbegründung)

- **F-TS-012 (AiNetLinterTests Tool-Pfad):** Info-Finding, kein Handlungsbedarf. Hartcodierter Pfad mit Env-Var-Override ist OK für Solo-Entwicklung.

Alle übrigen Findings (56) gehören thematisch in andere Brocken (F: Performance-Polish, G: Config-Deploy, H: Code-Quality-Rest, I: Doku-Rest, J: Architektur-Rest, K: Dependencies-Rest, L: Sicherheits-Rest, M: Prio-A-Findings-Raus, plus die Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-TS-001** (Doku-Update) — 5 Min, kann mit anderen Doku-Edits kombiniert werden
2. **F-TS-003** + **F-TS-004** + **F-TS-005** — ~40 Min, ein Commit
3. **F-TS-006** + **F-TS-007** — ~10 Min, ein Commit
4. **F-TS-008/009** + **F-TS-010** + **F-TS-011** — ~30 Min, ein Commit
5. **F-TS-002** (optional, braucht SQLite-Setup) — ~30 Min, separater Commit

**Gesamt-Aufwand in dieser Reihenfolge:** ~110 Min ohne SQLite, ~140 Min mit.

**Commit-Clustering-Vorschlag:**
- Commit 1: F-TS-003 + F-TS-004 + F-TS-005 (Medium-Severity-Tests)
- Commit 2: F-TS-006 + F-TS-007 (FrontMatterParser-Edge-Cases)
- Commit 3: F-TS-008/009 + F-TS-010 + F-TS-011 (ExportService + SlugRules + DocsValidator)
- Optional Commit 4: F-TS-002 (mit SQLite-Setup, falls umgesetzt)

## Querverweise zu anderen Brocken

- **F-SE-001 in PrioA** — `BuildLikePattern` ist `private static` in `SqlDocumentsStore`. Tests brauchen `InternalsVisibleTo` oder Reflection. F-TS-001 (SQLite-Setup) würde das vereinfachen.
- **F-SE-003 in PrioD** — Längen-Validierung; Tests könnten in `DocsMcpToolsTests` ergänzt werden, was aktuell nicht testpflichtig ist.
- **F-DK-004 in PrioB** — `SchemaMigrator`-Transaktion; F-TS-002 würde das Verhalten testen.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).
