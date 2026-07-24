# Dimension 4 — Test-Coverage & -Qualität

> **Vergleichsbasis:** `.agents/rules/02-testing.mdc` (Testpflicht Core, xUnit-v3-Stil,
> `SlugRulesTests` als Stilvorlage), AiNetLinter `EnableTestSentinel`-Regel (Tests als
> Gate für Core-Klassen), sowie die in `docs/04-Datenmodell-Validierung-Edgecases.md`
> dokumentierten Edge Cases.
> **Methodik:** Stichprobe aller 7 Test-Dateien, Klassifikation pro `[Fact]`/`[Theory]`
> gegen den dokumentierten Edge-Case-Katalog, Identifikation von Lücken.
> **Baseline:** `dotnet run --project tests/KnowHowToAI.Core.Tests -c Release` →
> **49 Tests, 0 Failed, 10,6s** (siehe [`_meta.md`](_meta.md)).

## Test-Inventar

| Test-Datei | Test-Methode | Was geprüft wird | Edge-Case-Abdeckung |
| --- | --- | --- | --- |
| `SlugRulesTests` | `IsValidSlug_AcceptsCompliantSlugs` (3 Inlines) | gültige Slugs | ✅ |
| | `IsValidSlug_RejectsNonCompliantSlugs` (6 Inlines) | ungültige Slugs | ✅ |
| | `GetParentSlug_ReturnsNullForRootSlug` | Root-Edge | ✅ |
| | `GetParentSlug_ReturnsParentForNestedSlug` | Nested-Edge | ✅ |
| `FrontMatterParserTests` | `Parse_ValidFile_ReturnsDocumentWithParsedFields` | Happy Path | ✅ |
| | `Parse_RootSlug_HasNoParentSlug` | Root-Edge | ✅ |
| | `Parse_WithoutTagsAndSynonyms_DefaultsToEmptyLists` | Defaults | ✅ |
| | `Parse_MissingTitle_Throws` | Missing-Title | ✅ |
| | `Parse_MissingOpeningDelimiter_Throws` | No-Front-Matter | ✅ |
| | `Parse_UnclosedFrontMatter_Throws` | Unclosed-Delimiter | ✅ |
| | `Parse_InvalidYaml_Throws` | Invalid-YAML | ✅ |
| `DocsValidatorTests` | `Validate_EmptyDirectory_ReturnsNoErrors` | Empty-Root | ✅ |
| | `Validate_ValidHierarchy_ReturnsNoErrors` | Happy-Hierarchy | ✅ |
| | `Validate_InvalidSlugSegment_ReportsError` | Invalid-Slug | ✅ |
| | `Validate_MissingTitle_ReportsError` | Missing-Title | ✅ |
| | `Validate_MissingParentDocuments_ReportsOrphanErrorForEachAncestor` | Orphan-Check (Multi-Level) | ✅ |
| | `Validate_CollectsErrorsAcrossMultipleFiles` | Multi-Error-Collect | ✅ |
| | `Validate_ContentContainsFileOrMarkdownLink_ReportsError` (4 Inlines) | File/Markdown-Link (4 Varianten) | ✅ |
| | `Validate_ContentContainsSlugAndHttpLinks_ReturnsNoErrors` | Negative-Case (Slug/HTTP) | ✅ |
| | `Validate_ContentAtThreshold_ReturnsNoWarning` | Length-Threshold-Boundary | ✅ |
| | `Validate_ContentAboveThreshold_ReportsWarningButStaysValid` | Length-Warning | ✅ |
| `ImportServiceTests` | `ImportAsync_InvalidDocs_ReturnsErrorsAndDoesNotReplaceAnything` | Validation-Fail-Path | ✅ |
| | `ImportAsync_ValidDocs_ReplacesWithParsedDocuments` | Happy-Path | ✅ |
| `ExportServiceTests` | `ExportAsync_NewTargetDirectory_CreatesMarkerAndWritesDocuments` | Empty-Target | ✅ |
| | `ExportAsync_ExistingMarker_WipesOldMarkdownBeforeReExport` | Re-Export | ✅ |
| | `ExportAsync_ForeignFilesWithoutMarker_ThrowsAndDoesNotCallGetAll` | Foreign-Without-Marker | ✅ |
| `SchemaMigratorTests` | `DiscoverScripts_FindsEmbeddedScript` | Discovery | ✅ |
| | `DiscoverScripts_SubstitutesConfiguredTableName` | Platzhalter-Substitution | ✅ |
| `SqlIdentifierValidatorTests` | `EnsureValid_AcceptsValidIdentifiers` (4 Inlines) | Valid-IDs | ✅ |
| | `EnsureValid_RejectsInvalidIdentifiers` (6 Inlines) | Invalid-IDs | ✅ |
| `AiNetLinterTests` | `LintRun_ReportsNoViolations` | Lint-Smoke | ✅ |

**Insgesamt: 49 Tests, davon 21 mit `[Theory]`/`[InlineData]`-Parametrisierung (entspricht ~105 effektiven Test-Cases).**

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-TS-001](#f-ts-001) | **High** | `SqlDocumentsStore` hat **null** Unit-Tests (dokumentiert, aber: Edge-Cases via Dapper sind testbar mit SQLite/InMemory) | n/a (fehlt) |
| [F-TS-002](#f-ts-002) | Medium | `SchemaMigrator.MigrateAsync` ist nicht getestet (nur `DiscoverScripts`) | n/a (fehlt) |
| [F-TS-003](#f-ts-003) | Medium | `DocsValidator` deckt `content`-Länge nur in 2 Boundary-Tests ab — kein Test für `content.Length = 0` | `DocsValidatorTests.cs:103-125` |
| [F-TS-004](#f-ts-004) | Medium | `ImportService` deckt `replaceAllAsync`-Throws nicht ab | `ImportServiceTests.cs:12-48` |
| [F-TS-005](#f-ts-005) | Medium | `ImportService` deckt nicht-leere docs-root mit fehlgeschlagenen Files ab (z.B. fehlender Parent) | `ImportServiceTests.cs:30-48` |
| [F-TS-006](#f-ts-006) | Low | `FrontMatterParser`: kein Test für nur-whitespace `title` (z.B. `title: "   "`) | `FrontMatterParserTests.cs:65-76` |
| [F-TS-007](#f-ts-007) | Low | `FrontMatterParser`: kein Test für BOM am Dateianfang (Umlaut-Encoding-Edge) | (fehlt) |
| [F-TS-008](#f-ts-008) | Low | `ExportService`: kein Test für `getAllAsync`-Throws | `ExportServiceTests.cs` |
| [F-TS-009](#f-ts-009) | Low | `ExportService`: kein Test für leeres Resultat von `getAllAsync` | `ExportServiceTests.cs` |
| [F-TS-010](#f-ts-010) | Low | `SlugRules.FromFilePath` ist nicht getestet | `SlugRulesTests.cs` |
| [F-TS-011](#f-ts-011) | Low | `DocsValidator`: kein Test für nicht-`.md`-Dateien im Root | `DocsValidatorTests.cs` |
| [F-TS-012](#f-ts-012) | Info | `AiNetLinterTests` ist die richtige Strategie (Lint als Test), aber: Tool-Standort ist hartcodiert | `AiNetLinterTests.cs:8` |

## Detail-Findings

### F-TS-001 — `SqlDocumentsStore` hat keine Unit-Tests

**Schweregrad:** High (per `02-testing.mdc` ausgenommen, aber Risiko bei komplexer Logik)

**Beobachtung:**
`docs/03-Projektstruktur-und-Konfiguration.md` Zeile 44 sagt:
> "`SqlDocumentsStore` ist die einzige Klasse mit echtem `SqlConnection`/Dapper-Zugriff und
> wird selbst nicht separat unit-getestet (dünner DB-Adapter, analog zu
> `SchemaMigrator.Migrate`)."

Diese Ausnahme ist in `02-testing.mdc` *nicht* explizit dokumentiert — sie ergibt sich aus
der Praxis. **Aber:** Der Store enthält Logik, die *sehr wohl* Edge-Cases hat:

| Methode | Edge-Case | Aktuell getestet? |
| --- | --- | --- |
| `BuildLikePattern` | Wildcard-Injection (Dim 2 F-SE-001) | ❌ |
| `ToDocument` (private) | Ungültiges JSON in `Tags`/`Synonyms` (Dim 1 F-CQ-003) | ❌ |
| `ReplaceAllAsync` | FK-Constraint-Verletzung bei falscher Slug-Reihenfolge | ❌ |
| `ListChildrenAsync` | Root-Edge (`parentSlug = null`) | ❌ |
| `SearchDocsAsync` | Leere `query` → `%%` → kompletter Match | ❌ |
| `GetDocAsync` | Slug nicht gefunden → `null` | ❌ |

**Mitigation-Idee (NICHT für v1 — Integrationstests sind explizit Backlog):**
- Die `02-testing.mdc` Zeile 20 sagt: "Echte Integrationstests gegen SQL Server sind bewusst
  Backlog." — also bewusst raus.
- Alternative: **SQLite-In-Memory für Unit-Tests?** Dapper ist DB-agnostisch. SQLite
  könnte für die meisten SELECT-Queries funktionieren, aber `LIKE '%...%'` und
  `NVARCHAR(MAX)`-JSON-Operationen verhalten sich leicht anders. Aufwand ~2-4 Stunden
  für die Infrastruktur, dann pro Test-Methode ~10 Minuten.
- **Empfehlung:** Erst wenn ein Finding in Dim 2 oder Dim 8 einen Test-Setup erfordert
  (z.B. für F-SE-001). Aktuell: dokumentiert, akzeptiert, Backlog-Item wert
  (ist es auch — `docs/05-Roadmap.md` Zeile 94 erwähnt es explizit).

**Aufwand:** ~4 Stunden für SQLite-Test-Infrastruktur + 1-2 Stunden pro Methode.

---

### F-TS-002 — `SchemaMigrator.MigrateAsync` nicht getestet

**Schweregrad:** Medium (Risiko, dass `MigrateAsync` z.B. Connection-String falsch handhabt)

**Beobachtung:**
`SchemaMigratorTests` testet nur `DiscoverScripts` (2 Tests). `MigrateAsync` (Zeile 16 in
`SchemaMigrator.cs`) ist ungetestet. Würde echten SQL-Server brauchen, was der Grund für
die Lücke ist.

**Edge-Cases in `MigrateAsync`:**
- Connection-String ist null/leer → SqlException
- `documentsTableName` ist ungültig → wird vor Open abgefangen via `SqlIdentifierValidator`
- Erste Migration, Tabelle existiert nicht → `IF NOT EXISTS`-Guard erstellt
- Zweite Migration, Tabelle existiert → Guard tut nichts (idempotent)
- Skript-Wiederholung nach Fehler → ???
- SQL-Server antwortet nicht → Timeout

**Mitigation:** Auch hier: Integrationstests sind Backlog. Dokumentiert.

**Aufwand:** wie F-TS-001.

---

### F-TS-003 — `DocsValidator` deckt Content-Länge nur an Boundary ab

**Schweregrad:** Medium (kleine Lücke)

**Beobachtung:**
`DocsValidatorTests` Zeile 103-125 testet `content.Length == 10` (Threshold, no warning)
und `content.Length == 11` (above threshold, warning). Was fehlt:
- `content.Length == 0` (leerer Content — gültig? Warning?)
- `content.Length == 1` (deutlich unter Threshold)
- Test mit `maxContentLengthWarning = 0` (Edge-Case: Warning-Schwelle 0 → Warning für jeden Inhalt?)

**Fix:** Drei zusätzliche Tests, ~5 Minuten.

---

### F-TS-004 — `ImportService.replaceAllAsync`-Throws nicht getestet

**Schweregrad:** Medium (Edge-Case im Happy Path)

**Beobachtung:**
`ImportAsync_ValidDocs_ReplacesWithParsedDocuments` testet den Happy Path. Was, wenn
`replaceAllAsync` (die SQL-Operation) wirft? Aktuell:
- Validierung ist OK
- `replaceAllAsync` wird aufgerufen
- Wirft → Exception propagiert nach oben
- `RunImport` fängt mit `catch (Exception ex)` und gibt Exit 2
- **Aber:** Partielle Inserts sind möglich! Wenn `DELETE` klappt, aber ein `INSERT` in
  der Mitte wirft, ist die DB in einem halbleeren Zustand. `SqlDocumentsStore.ReplaceAllAsync`
  ist zwar in einer Transaction (siehe Zeile 27 + 52), aber wenn der `using` block
  vorzeitig abbricht (z.B. `OperationCanceledException`), wird der Rollback implizit
  via `await using` ausgelöst.

**Test, der fehlt:**
```csharp
[Fact]
public async Task ImportAsync_ReplaceThrows_NoPartialSideEffects()
{
    // Arrange: replace wirft
    // Act + Assert: Exception propagiert, aber... (siehe Empfehlung)
}
```

Eigentlich ist der Test nicht so wichtig, weil `SqlDocumentsStore.ReplaceAllAsync` die
Transaktion managed. Aber: das `ImportService` selbst hat keine Transaction-Verantwortung
— wenn der `await replaceAllAsync(documents, cancellationToken)` durch Cancellation
abgebrochen wird, läuft das Cancellation in `SqlDocumentsStore` (das ist OK), aber: der
`ImportService` hat keine Cleanup-Logik. Was, wenn der Caller den CancellationToken
gesetzt hat und das Cancellation *vor* `replaceAllAsync` passiert? Aktuell: nichts
Schlimmes — `replaceAllAsync` wird nicht aufgerufen.

**Aufwand:** ~15 Minuten für den Test.

---

### F-TS-005 — `ImportService` und fehlgeschlagene Files

**Schweregrad:** Medium

**Beobachtung:** `ImportAsync_InvalidDocs_ReturnsErrorsAndDoesNotReplaceAnything` testet
einen ungültigen Slug. Was, wenn:
- Eine Datei hat gültigen Slug, aber fehlenden Parent (Orphan) → wird das im `ImportService`
  auch abgefangen? Ja, weil `DocsValidator.Validate` das vorab prüft.
- Eine Datei ist nicht lesbar (z.B. Lock durch anderen Prozess) → `File.ReadAllText`
  wirft `IOException`. Wird der Fehler gesammelt oder propagiert er?

Aktuell propagiert er (`yield return` reicht die Exception nicht als Validation-Error
durch). Das ist ein Bug oder eine Design-Entscheidung — `validate` würde das gleiche
Problem haben (gleiche `Directory.EnumerateFiles` + `File.ReadAllText` Pattern).

**Fix-Empfehlung:** Tests für nicht-lesbare Dateien hinzufügen ODER `File.ReadAllText` in
`yield return` durch defensive Variante ersetzen (try/catch IOException, in Errors-Liste
sammeln statt werfen).

**Aufwand:** ~20 Minuten.

---

### F-TS-006 — `FrontMatterParser` kein Test für nur-whitespace `title`

**Schweregrad:** Low

**Beobachtung:** `Parse_MissingTitle_Throws` testet `tags: [a]` ohne `title:`-Feld.
Aber: `title: "   "` (drei Leerzeichen) — `string.IsNullOrWhiteSpace` (FrontMatterParser
Zeile 28) fängt das ab. Test fehlt.

**Aufwand:** ~5 Minuten.

---

### F-TS-007 — `FrontMatterParser` kein BOM-Test

**Schweregrad:** Low (Edge-Case, der in Windows-Bearbeitung manchmal auftritt)

**Beobachtung:** Datei mit UTF-8-BOM am Anfang → `SplitFrontMatter` würde die BOM
vom `---` abtrennen? Nein — `String.StartsWith` mit `StringComparison.Ordinal` ist
byte-genau, BOM wäre also 3 Bytes vor dem `---`. `StartsWith("---")` würde fehlschlagen
→ "Datei beginnt nicht mit YAML Front Matter" Exception.

Das ist die richtige Semantik (BOM ist nicht erlaubt in Markdown-Dateien), aber nicht
explizit getestet. Wenn ein User die Datei mit Notepad speichert ("Mit Codierung →
UTF-8 mit BOM"), passiert das häufig.

**Aufwand:** ~5 Minuten.

---

### F-TS-008 / F-TS-009 — `ExportService` Edge-Cases fehlen

**Schweregrad:** Low (zwei Lücken)

**Beobachtung:**
- Kein Test: `getAllAsync` wirft → `ExportAsync` propagiert oder fängt?
- Kein Test: `getAllAsync` gibt leere Liste zurück → Marker-Datei wird geschrieben,
  aber keine `.md`-Dateien. Korrekt? Aktueller Code: ja (Schleife über leere Liste
  macht nichts). Test fehlt.

**Aufwand:** ~10 Minuten.

---

### F-TS-010 — `SlugRules.FromFilePath` nicht getestet

**Schweregrad:** Low (Pure-Function, leicht zu testen)

**Beobachtung:** `FromFilePath` wird nirgends getestet. Edge-Cases:
- `docsRootPath` und `filePath` identisch → was wird zurückgegeben?
- `filePath` ist außerhalb von `docsRootPath` (Path-Traversal-Edge, F-SE-006)
- Datei mit mehreren Extensions (`foo.md.bak`) → `Path.GetExtension` liefert `.bak`
  → Slug wäre `foo.md`. Sollte das ein Fehler sein?

**Aufwand:** ~15 Minuten für 3-4 Tests.

---

### F-TS-011 — `DocsValidator` und Nicht-MD-Dateien

**Schweregrad:** Low (Defense-in-Depth-Test)

**Beobachtung:** Wenn jemand eine `.txt`-Datei in `docs-root` legt, wird sie ignoriert
(`Directory.EnumerateFiles(docsRootPath, "*.md", SearchOption.AllDirectories)`).
Aktuell kein Test dafür.

**Aufwand:** ~5 Minuten.

---

### F-TS-012 — `AiNetLinterTests` Tool-Pfad hartcodiert (Info)

**Beobachtung:** `AiNetLinterTests.cs:8`:
```csharp
private const string DefaultExePath = @"C:\Daten\AiNetLinter-win-x64\AiNetLinter.exe";
```

Lokal-pfadhartcodiert, mit `Environment.GetEnvironmentVariable("AINETLINTER_EXE")` als
Override. Das ist OK für Solo-Entwicklung, aber:
- Auf anderen Rechnern schlägt der Test fehl (richtig, mit Skip)
- Auf CI (siehe Backlog-Item "CI-Pipeline") müsste der Pfad pro Build-Agent konfiguriert
  werden

**Fix-Empfehlung:** `AINETLINTER_EXE` zur "no-skip" Variable machen: wenn nicht gesetzt
und Default-Pfad nicht da, explizit warnen statt überspringen. Aber: das ist eine
Verhaltens-Änderung. Aktuell: dokumentiert, akzeptiert.

---

## Test-Coverage-Bewertung

| Code-Klasse | Pflicht (per `02-testing.mdc`) | Tatsächlich | Status |
| --- | --- | --- | --- |
| `Document` (Modell) | nein (Modell) | keine Tests | OK |
| `DocumentSummary` (Modell) | nein | keine Tests | OK |
| `DocumentDetail` (Modell) | nein | keine Tests | OK |
| `SlugRules` | ja | 4 Tests, 9 Inlines | ✅ Vollständig |
| `FrontMatterParser` | ja | 7 Tests | ✅ Solide |
| `DocsValidator` | ja | 10 Tests, 4+2 Inlines | ✅ Solide (kleine Lücken F-TS-003) |
| `ImportService` | ja | 2 Tests | ⚠️ Minimal (F-TS-004, F-TS-005) |
| `ExportService` | ja | 3 Tests | ✅ Solide (kleine Lücken F-TS-008/009) |
| `SchemaMigrator` (nur Discovery) | ja (per Regel) | 2 Tests | ✅ Adequat (`MigrateAsync` ausgenommen, Backlog) |
| `SqlIdentifierValidator` | ja | 10 Inlines | ✅ Vollständig |
| `SqlDocumentsStore` | ausgenommen (per Doku) | 0 Tests | ⚠️ Dokumentiert, aber Lücke |
| `Configuration/*` | nein (Datenklassen) | 0 Tests | OK |
| `Program.cs` (CLI) | nein (per Regel) | 0 Tests | OK |
| `DocsMcpTools` (MCP) | nein (per Regel) | 0 Tests | OK (reine Delegation) |
| `DocsMcpResources` (MCP) | nein (per Regel) | 0 Tests | OK |

## Test-Stil-Bewertung

- ✅ `[Fact]`/`[Theory]`/`[InlineData]` korrekt verwendet
- ✅ `TestContext.Current.CancellationToken` statt veraltetes `CancellationToken.None`
- ✅ `IDisposable` für Temp-Directory-Cleanup (`DocsValidatorTests`, `ImportServiceTests`,
  `ExportServiceTests`)
- ✅ Strikte AAA-Struktur (Arrange/Act/Assert)
- ✅ `Assert.Collection`, `Assert.Single`, `Assert.Contains` idiomatisch
- ✅ Test-Methoden-Namen: `MethodName_Scenario_ExpectedBehavior` (z.B.
  `Validate_ContentContainsFileOrMarkdownLink_ReportsError`) — sehr gut lesbar
- ❌ `SlugRulesTests` ist die Stilvorlage, aber `ImportServiceTests`/`ExportServiceTests`
  haben zwei Test-Klassen in *einer* Datei (`ImportServiceTests` + `ExportServiceTests`).
  Inkonsistent. Sollte aufgespalten werden in `ImportServiceTests.cs` und
  `ExportServiceTests.cs`.

## Zusammenfassung Dim 4

- **12 Findings**, davon 1 × High (akzeptiert per Doku), 4 × Medium, 6 × Low, 1 × Info.
- **Test-Qualität ist insgesamt hoch.** Die Stichprobe deckt das Wesentliche ab, Stile sind
  konsistent, AAA-Disziplin ist sauber.
- **Zwei nennenswerte Lücken:**
  1. `SqlDocumentsStore` (F-TS-001) — bewusst ausgenommen, dokumentiert, Backlog. Würde
     bei Implementierung der Wildcard-Escapes (F-SE-001) SQLite-Tests erfordern.
  2. `ImportService` Edge-Cases (F-TS-004, F-TS-005) — kleinere Lücken, ~30 Min Aufwand.
- **Empfehlung:** F-TS-006/007/008/009/010/011 als Quick-Wins in einem separaten Commit
  angehen (~45 Min total).
