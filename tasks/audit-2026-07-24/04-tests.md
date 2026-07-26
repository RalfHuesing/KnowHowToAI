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
| [F-TS-012](#f-ts-012) | Info | `AiNetLinterTests` ist die richtige Strategie (Lint als Test), aber: Tool-Standort ist hartcodiert | `AiNetLinterTests.cs:8` |

## Detail-Findings

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

- **1 Finding** (nach Prio E-Extraktion), 1 × Info. F-TS-001 bis F-TS-011 sind in PrioE extrahiert (siehe `tasks/audit-2026-07-24-PrioE/Konzept.md`).
- **Test-Qualität ist insgesamt hoch.** Die Stichprobe deckt das Wesentliche ab, Stile sind
  konsistent, AAA-Disziplin ist sauber.
