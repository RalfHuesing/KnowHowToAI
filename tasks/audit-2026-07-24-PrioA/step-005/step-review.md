---
status: done
type: step-review
task: audit-2026-07-24-PrioA
step: 005
reviewed_by: auditer
reviewed_by_model: MiniMax-M3
reviewed_by_model_knowledge_cutoff: 2026-01
reviewed_at: 2026-07-26T20:50:00+02:00
verdict: approved
---

# Review Step 005: F-AR-002 — Core-Services mit `ILogger<T>`-Injection + Composition-Root-Konsolidierung

## Verdict

- [x] **approved** — alle drei Prüfebenen ok (nur MINOR/NITPICK-Beobachtungen, keine CRITICAL/MAJOR-Findings)
- [ ] **issues** — Fix-Step `step-005/fix-XX` nötig
- [ ] **blocked** — Nutzer-Entscheidung nötig

## Geprüft

- [x] Plan-Erfüllung: alle 10/11 Plan-Dateien geändert (Datei 11 Nice-to-Have bewusst übersprungen, dokumentiert)
- [x] Rules-Konformität: `.agents/rules/**` eingehalten
- [x] Logische Korrektheit: Code macht was er soll, Logger-Bridge funktioniert, Factory konsolidiert
- [x] Build: selbst nachgeprüft, grün (0 Warnings, 0 Errors)
- [x] Tests: selbst nachgeprüft, grün (78/78, Baseline 78 → 78)
- [x] AiNetLinter-Report direkt gelesen: `OK` (0 Violations)

## Befund

### Plan-Erfüllung

| Plan-Datei | Erfüllt? | Evidenz |
|---|---|---|
| 1: `KnowHowToAI.Core.csproj` — NuGet-Ref `Microsoft.Extensions.Logging.Abstractions` 10.0.9 | **Ja** | `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj:12` — `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />` |
| 2: `SqlDocumentsStore.cs` — ctor + Log-Calls in 5 Methoden | **Ja** | `SqlDocumentsStore.cs:19` (ctor mit `ILogger<SqlDocumentsStore>` als required Param); Log-Calls in `ReplaceAllAsync:29+61`, `GetAllAsync:68+76`, `ListChildrenAsync:84+95`, `SearchDocsAsync:101+130`, `GetDocAsync:147+153` |
| 3: `DocsValidator.cs` — ctor mit optionalem `ILogger<DocsValidator>?` + Log-Calls | **Ja** | `DocsValidator.cs:10` — positional record mit `int maxContentLengthWarning = 8000, ILogger<DocsValidator>? logger = null`; Log-Calls in `Validate:16+62` |
| 4: `ImportService.cs` — positional record + `ILogger<ImportService>?` + Log-Calls | **Ja** | `ImportService.cs:11-14` — positional record mit `(replaceAllAsync, maxContentLengthWarning = 8000, ILogger<ImportService>? logger = null)`; Log-Calls in `ImportAsync:23,28-30,37-39` (sogar 2 End-Logs: Validation-Fail-Fall + Erfolg) |
| 5: `ExportService.cs` — positional record + `ILogger<ExportService>?` + Log-Calls | **Ja** | `ExportService.cs:10-12` — positional record mit `(getAllAsync, ILogger<ExportService>? logger = null)`; Log-Calls in `ExportAsync:18-20,33-35` |
| 6: `Program.cs` — Factory-Funktionen + angepasste Run-Methoden | **Ja** | `Program.cs:164-171` — `BuildStore`/`BuildImportService`/`BuildExportService` als `static` Helper; `RunValidate:64-67`, `RunImport:83-89`, `RunExport:106-111`, `RunServer:135-142` nutzen sie (oder lösen `ILogger<T>` via DI auf) |
| 7: `ImportExportServiceTests.cs` — `NullLogger<T>.Instance` durchgereicht | **Ja** | Tests `ImportServiceTests:21-24,42-45` (NullLogger<ImportService>); `ExportServiceTests:84,108,127` (NullLogger<ExportService>) |
| 8: `DocsValidatorTests.cs` — Tests angepasst | **Ja** | `DocsValidatorTests.cs:7` (field-init mit `NullLogger<DocsValidator>.Instance`); explizit auch in `Validate_ContentAtThreshold_ReturnsNoWarning:106` und `Validate_ContentAboveThreshold_ReportsWarningButStaysValid:117` |
| 9: `docs/02` — Tech-Stack-Tabelle um Logging-Abstraktion | **Ja** | `docs/02-Architektur-und-Techstack.md:28` — neue Zeile `Logging-Abstraktion \| Microsoft.Extensions.Logging.Abstractions \| ...`; bestehende Logging-Zeile (Z. 27) um Backend-nur-in-Cli-Hinweis ergänzt |
| 10: `docs/03` — Solution-Layout-Notiz | **Ja** | `docs/03-Projektstruktur-und-Konfiguration.md:27` (ILogger<T>-Erwartung in Core); `:39` (Composition-Root-Factory-Hinweis in Cli) |
| 11: `docs/03` Abschnitt 2 — Beispiel-Log-Zeilen (Nice-to-Have) | **Übersprungen** | Coder dokumentiert in `step-result.md:98` — sinnvolle Entscheidung, da SQL-Server auf Dev-Rechner nicht verifiziert (kein Smoke-Lauf möglich, fiktive Zeilen würden später realen Logs widersprechen). DoD sagt „*Optional* — wenn der Coder die Zeilen nicht übernehmen will, ist das kein Verstoß". → **Plan-konform** |

**Coder-Abweichungen (alle explizit dokumentiert, severity-bewusst eingeordnet):**

a) `GetAllAsync`/`ListChildrenAsync`: `[.. rows]` → `.ToList()` (CS9176 wegen `var`-Inferenz). **MINOR** — semantisch identisch, reine technische Notwendigkeit, vom Coder korrekt erkannt und behoben (`step-result.md:95`).

b) Plan-Beispiel für `RunServer` hatte Parameter-Reihenfolge für `DocsMcpTools`-ctor verdreht (plan zeigte `(store, logger, maxQueryLength, maxResults)`, realer ctor ist `(store, maxQueryLength, maxResults, logger)`). **MINOR** — Coder hat die korrekte Reihenfolge übernommen, was der *bestehende* `RunServer`-Code bereits hatte (`step-result.md:96`). Positiv: Verifikation gegen realen Code statt blindes Kopieren.

c) `Log.Logger.ForContext<T>()` liefert `Serilog.ILogger`, nicht `Microsoft.Extensions.Logging.ILogger<T>`. Plan-Vorschlag hätte nicht kompiliert. **MINOR (Lob für Problemerkennung)** — Coder hat die richtige Bridge gebaut: `using var loggerFactory = LoggerFactory.Create(b => b.AddSerilog(Log.Logger, dispose: false))` + `loggerFactory.CreateLogger<T>()` (3× in CLI Run-Methoden, korrekt mit `using var` für Disposal). `dispose: false` ist korrekt, weil Serilog selbst verwaltet wird (`Log.Logger`-Singleton darf nicht durch `LoggerFactory.Dispose` mit-disposed werden). **Dies ist eine korrekte Umsetzung trotz fehlerhaftem Plan-Vorschlag.**

d) Beispiel-Log-Zeilen in `docs/03` Abschnitt 2 übersprungen. **MINOR** — sinnvolle Entscheidung (siehe oben).

e) Commit-Subject 75 Zeichen (`fix(arch): core-services mit ilogger-injection und composition-root-factory`). Regel sagt `< 70` (`03-git-workflow.mdc:30`). **MINOR / NITPICK** — Subject stammt aus dem Orchestrator-Auftrag (literal übernommen), 5 Zeichen über der 70er-Regel, 3 über der im Orchestrator-Auftrag genannten 72er-Schwelle. Inhaltlich prägnant und informativ. Kein Build-Bruch.

### Rules-Konformität

| Regel | Status | Evidenz |
|---|---|---|
| `01-code-style.mdc` — sealed Klassen | **Eingehalten** | `SqlDocumentsStore` (`SqlDocumentsStore.cs:13`), `DocsValidator` (`DocsValidator.cs:10`), `ImportService` (`ImportService.cs:11`), `ExportService` (`ExportService.cs:10`) — alle `sealed` |
| `01-code-style.mdc` — Early Returns | **Eingehalten** | `SearchDocsAsync` (`SqlDocumentsStore.cs:104,107`) — empty-query und zu-lange-query early return; `ImportAsync` (`ImportService.cs:26`) — Validation-Fail early return |
| `01-code-style.mdc` — keine Kommentare | **Eingehalten** | Keine neuen Kommentare durch den Step hinzugefügt (Diff zeigt nur `using`-Statements und Body-Änderungen). Die vorhandenen Datei-Header-Kommentare (Z. 10-12 in `SqlDocumentsStore.cs`, Z. 8-10 in `DocsValidator.cs`, Z. 8-10 in `ImportService.cs`, Z. 7-9 in `ExportService.cs`) waren bereits vorher da |
| `01-code-style.mdc` — bewusst einfacher Code | **Eingehalten** | Keine Interface-Wüste, kein Helper für 1 Aufrufer, kein Design für hypothetische Anforderungen |
| `02-testing.mdc` — Tests im selben Commit | **Eingehalten** | `ImportExportServiceTests.cs`, `DocsValidatorTests.cs`, `KnowHowToAI.Core.Tests.csproj` sind in `934978b` (gleicher Commit) |
| `02-testing.mdc` — keine Pflicht für Logger-Aufruf-Tests | **Eingehalten** | Plan sagt explizit „nicht zwingend"; Coder hat keine `LoggerTesting`-Tests hinzugefügt. Test-Count 78 → 78 (Baseline stabil) |
| `03-git-workflow.mdc` — Conventional Commit, deutsch, Imperativ | **Eingehalten** | `fix(arch):` (deutsch, imperativ); Body erklärt das *Warum* (Beobachtbarkeit, F-AR-001-Konsolidierung); Trailer `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` vorhanden; `Refs:`-Verweis auf `tasks/audit-2026-07-24-PrioA/step-005` vorhanden |
| `03-git-workflow.mdc` — Subject < 70 Zeichen | **Verletzt** (75) | Siehe Befund-Punkt e) oben → **NITPICK** |
| `05-documentation.mdc` — Doku im selben Commit | **Eingehalten** | `docs/02-Architektur-und-Techstack.md` und `docs/03-Projektstruktur-und-Konfiguration.md` sind in `934978b` (gleicher Commit wie Code) |
| `06-configuration.mdc` — keine Magic-Werte im Code | **Eingehalten** | `MaxContentLengthWarning = 8000` als Code-Literal in `DocsValidator`/`ImportService` ctor-Default (war schon vorher so, `KnowHowToAi:Validation:MaxContentLengthWarning` ist die Konfig-Override-Quelle). Konzept-Vorgabe: Library-Defaults ok, Settings in `appsettings.json` |
| `AiNetLinter.mdc` — `MaxConstructorDependencies: 5` | **Eingehalten** | `SqlDocumentsStore`: 3 ctor-Params (≤ 4); `DocsValidator`: 2 ctor-Params; `ImportService`: 3 ctor-Params; `ExportService`: 2 ctor-Params |
| `AiNetLinter.mdc` — `MaxMethodLineCount: 60` | **Eingehalten** | `ReplaceAllAsync`: 38 LOC; `SearchDocsAsync`: 36 LOC; `Validate`: 53 LOC; `ImportAsync`: 21 LOC; `ExportAsync`: 21 LOC; `GetAllAsync`: 15 LOC; `ListChildrenAsync`: 16 LOC; `GetDocAsync`: 13 LOC — alle unter 60 |
| `AiNetLinter.mdc` — `EnforceSealedClasses` | **Eingehalten** | siehe oben (alle 4 Services `sealed`) |
| `AiNetLinter.mdc` — `EnforceNullableEnable` | **Eingehalten** | `#nullable enable` ist in `KnowHowToAI.Core.csproj` global gesetzt (war vorher schon); `ILogger<DocsValidator>?`/`ILogger<ImportService>?`/`ILogger<ExportService>?` mit Null-Conditional-Operator (`logger?.LogInformation`) |
| `AiNetLinter.mdc` — `EnforcePascalCase`, `EnforceSemanticNaming` | **Eingehalten** | Alle neuen Identifier sind PascalCase/parametrierter Standard (`logger`, `storeLogger`, `importLogger`, `exportLogger`) |
| `AiNetLinter.mdc` — `EnforceAsciiIdentifiers` | **Eingehalten** | Identifiers sind alle ASCII (nur `Dispose`/`Dispose`-Pattern im Test) |

**AiNetLinter-Report direkt gelesen** (`tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md`):

```
# Run: 2026-07-26 20:38:24
OK
```

→ 0 Violations, post-commit re-rendered (Commit-Zeit: 20:36:57, Report-Zeit: 20:38:24 = ~90s später, plausibel als Post-Commit-Re-Run).

### Logische Korrektheit

**Kern-Anforderung 1: Beobachtbarkeit pro Lauf — erfüllt.**

Jeder Service-Log-Call markiert Start und Ende mit strukturierten Properties:

- `SqlDocumentsStore.ReplaceAllAsync` (`SqlDocumentsStore.cs:29-31, 61-63`): "ReplaceAll startet: {DocumentCount} Dokumente in Tabelle {Table}" + "ReplaceAll abgeschlossen: {DocumentCount} Dokumente in {ElapsedMs}ms"
- `SqlDocumentsStore.GetAllAsync` (`:68, 76-78`): "GetAll startet" + "GetAll abgeschlossen: {DocumentCount} Dokumente in {ElapsedMs}ms"
- `SqlDocumentsStore.ListChildrenAsync` (`:84, 95`): "ListChildren(parentSlug={ParentSlug})" + "ListChildren abgeschlossen: {ResultCount} Kinder"
- `SqlDocumentsStore.SearchDocsAsync` (`:101-103, 130-132`): "SearchDocs(query='{Query}', maxQueryLength={MaxQueryLength}, maxResults={MaxResults})" + "SearchDocs abgeschlossen: {ResultCount} Treffer, truncated={Truncated}"
- `SqlDocumentsStore.GetDocAsync` (`:147, 153-155`): "GetDoc(slug='{Slug}')" + "GetDoc abgeschlossen: {ResultState}"
- `DocsValidator.Validate` (`DocsValidator.cs:16, 62-64`): "Validate startet: docsRoot='{DocsRoot}'" + "Validate abgeschlossen: {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms"
- `ImportService.ImportAsync` (`ImportService.cs:23, 28-30, 37-39`): "Import startet: docsRoot='{DocsRoot}'" + (bei Validation-Fail) "Import abgeschlossen (Validation fehlgeschlagen): {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms" oder (bei Erfolg) "Import abgeschlossen: {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms"
- `ExportService.ExportAsync` (`ExportService.cs:18-20, 33-35`): "Export startet: target='{Target}', markerFile='{MarkerFile}'" + "Export abgeschlossen: {DocumentCount} Dokumente, {ElapsedMs}ms"

Alle Templates nutzen `{Property}`-Platzhalter (strukturiert) statt String-Interpolation (was unstrukturierte Logs ergäbe). `Stopwatch` ist explizit `System.Diagnostics.Stopwatch` — kein `BenchmarkDotNet`-Ballast.

**Kern-Anforderung 2: `logger?` als optional in DocsValidator/ImportService/ExportService — erfüllt.**

- `DocsValidator.cs:10`: `int maxContentLengthWarning = 8000, ILogger<DocsValidator>? logger = null` (Default null)
- `ImportService.cs:14`: `ILogger<ImportService>? logger = null` (Default null)
- `ExportService.cs:12`: `ILogger<ExportService>? logger = null` (Default null)
- `logger?.LogInformation(...)` mit Null-Conditional-Operator an allen 5 Log-Stellen pro Service (`DocsValidator.cs:16, 62`; `ImportService.cs:23, 28, 37`; `ExportService.cs:18, 33`)
- Tests können `new DocsValidator()` ohne `NullLogger<T>.Instance` aufrufen (Default null), aber der Coder hat sich für die explizite Variante entschieden — bessere Test-Praxis, regel-konform.

**Kern-Anforderung 3: Composition-Root-Factory in `Program.cs` — erfüllt.**

- `Program.cs:164-171`: `static SqlDocumentsStore BuildStore(...)`, `static ImportService BuildImportService(...)`, `static ExportService BuildExportService(...)` als `static` Helper-Funktionen
- `BuildStore(options, storeLogger) → new(options.ConnectionString, options.DocumentsTableName, storeLogger)` ✓
- `BuildImportService(options, store, importLogger) → new(store.ReplaceAllAsync, options.Validation.MaxContentLengthWarning, importLogger)` ✓
- `BuildExportService(store, exportLogger) → new(store.GetAllAsync, exportLogger)` ✓
- Konsolidiert die Konstruktion an einem einzigen Ort — Verifikation per `grep`: `new SqlDocumentsStore` taucht *nur* in `BuildStore` auf (via `_table`-Default und ctor in der statischen Helper); `new ImportService`/`new ExportService` nur in den entsprechenden `Build*Service`-Funktionen. `new DocsValidator` nur in `RunValidate:65` (kein `BuildValidator` geplant, akzeptabel — `DocsValidator` ist nicht geteilt zwischen Run-Modi).

**Kern-Anforderung 4: DI-Setup für `RunServer` — erfüllt.**

- `Program.cs:131-142`:
  - `builder.Services.AddSerilog(Log.Logger)` registriert Serilog-Logger als Provider
  - `builder.Services.AddSingleton<SqlDocumentsStore>(sp => BuildStore(options, sp.GetRequiredService<ILogger<SqlDocumentsStore>>()))` ✓
  - `builder.Services.AddSingleton(sp => new DocsMcpTools(sp.GetRequiredService<SqlDocumentsStore>(), options.Search.MaxQueryLength, options.Search.MaxResults, sp.GetRequiredService<ILogger<DocsMcpTools>>()))` ✓ — Reihenfolge `(store, maxQueryLength, maxResults, logger)` passt zum realen Ctor in `DocsMcpTools.cs:12`
  - `ImportService`/`ExportService` korrekt *nicht* im DI-Container (sind CLI-only) ✓

**Kern-Anforderung 5: `LoggerFactory`-Bridge für `Log.Logger.ForContext<T>()`-Issue — korrekt umgesetzt.**

Der Plan-Beispielcode `Log.Logger.ForContext<SqlDocumentsStore>()` hätte nicht kompiliert, weil `ForContext<T>()` ein `Serilog.ILogger` zurückgibt, nicht `Microsoft.Extensions.Logging.ILogger<T>`. Der Coder hat das korrekt erkannt und eine Brücke gebaut:

```csharp
using var loggerFactory = LoggerFactory.Create(b => b.AddSerilog(Log.Logger, dispose: false));
var logger = loggerFactory.CreateLogger<SqlDocumentsStore>();
```

3× in CLI Run-Methoden (`:64`, `:83`, `:106`). `dispose: false` ist korrekt — der `LoggerFactory` darf `Log.Logger` (Serilog-Global-Singleton) nicht disposen, sonst würden nachfolgende `Log.Logger`-Aufrufe in derselben Run-Methode fehlschlagen. `using var` stellt sicher, dass die `LoggerFactory` selbst (nicht Serilog) am Ende des Scopes disposed wird. Im `RunServer`-Pfad nutzt der Coder stattdessen `builder.Services.AddSerilog(Log.Logger)` (`:133`) — korrekte DI-Bridge, löst `ILogger<T>` über `sp.GetRequiredService<ILogger<T>>()` auf.

**Kern-Anforderung 6: `SearchResult`-Folge (Schritt 5f) — nicht gebrochen.**

`SqlDocumentsStore.SearchDocsAsync` (`SqlDocumentsStore.cs:129`) gibt weiterhin `SearchResult(results, Truncated: totalCount > results.Count)` zurück, mit dem `SearchResult`-Wrapper. Die `ResponseSize.Measure`-Switch-Arm aus Step 003 bleibt unangetastet (kein Code in `DocsMcpTools` geändert in diesem Commit). Verifiziert: kein Touch in `McpTools/DocsMcpTools.cs` in `934978b`.

**Kern-Anforderung 7: Konsistenz `DocsMcpTools`-Logger ↔ `SqlDocumentsStore`-Logger — keine Konflikte.**

- `DocsMcpTools` hat seinen eigenen `ILogger<DocsMcpTools>` (per DI in `RunServer:142`); loggt Tool-Aufrufe (nicht in dieser Diff-Zeile geändert, vorher schon vorhanden)
- `SqlDocumentsStore` hat seinen eigenen `ILogger<SqlDocumentsStore>` (per ctor in `BuildStore`); loggt Store-Methoden-Aufrufe
- Keine doppelten Log-Hierarchien: Tool-Logger loggt einmal pro MCP-Call, Store-Logger loggt einmal pro DB-Operation (typischerweise 1× Store-Call pro 1× Tool-Call). Akzeptable Beobachtbarkeits-Tiefe.
- `LogInformation`-Level für beide Seiten — konsistent.

**Tests:**

- 78 Tests grün (selbst nachgeprüft mit `dotnet test -c Release --no-build` → `gesamt: 78, fehlgeschlagen: 0, erfolgreich: 78`)
- `ImportExportServiceTests` mit `NullLogger<T>.Instance` ✓ (verifiziert in `ImportExportServiceTests.cs:21-24, 42-45, 84, 108, 127`)
- `DocsValidatorTests` mit `NullLogger<DocsValidator>.Instance` ✓ (verifiziert in `DocsValidatorTests.cs:7, 106, 117`)
- `SchemaMigratorTests` — `SchemaMigrator` ist `static class` mit `Action<string> logInformation`-Parameter (nicht `ILogger<T>`); keine Anpassung nötig, kein Test-Bruch (78/78 grün beweist das)

**Adversarial Probes:**

1. **AI-Bridge-Layer-Probe**: Direkter Aufruf `Log.Logger.ForContext<SqlDocumentsStore>()` würde `Serilog.ILogger` liefern, nicht `Microsoft.Extensions.Logging.ILogger<T>` → Compile-Fehler oder `InvalidCastException` zur Laufzeit. Verifiziert per `grep`: kein `Log.Logger.ForContext<` in der Codebase, alle 3 CLI Run-Methoden nutzen die `LoggerFactory.Create(...).CreateLogger<T>()`-Bridge. → **Kein Bug**, Bridge korrekt.

2. **Memory-Leak-Probe**: Pro CLI Run-Methode wird eine neue `LoggerFactory` erzeugt. Wird sie disposed? Verifiziert: `using var loggerFactory = ...` an allen 3 Stellen (`:64`, `:83`, `:106`). `using var` am Block-Ende → automatischer Dispose. → **Kein Leak**.

3. **F-AR-001-Konsolidierungs-Probe**: Wird `new SqlDocumentsStore(...)` *nur* in `BuildStore` aufgerufen? Verifiziert per `grep "new SqlDocumentsStore|new ImportService|new ExportService|new DocsValidator"` in `Program.cs`:
   - `new SqlDocumentsStore`: 0 Treffer in Run-Methoden (nur via `BuildStore` in der statischen Helper-Funktion `:165`)
   - `new ImportService`: 0 direkte Treffer (nur via `BuildImportService` `:168`)
   - `new ExportService`: 0 direkte Treffer (nur via `BuildExportService` `:171`)
   - `new DocsValidator`: 1 Treffer in `RunValidate:65` (kein `BuildValidator` geplant, akzeptabel)
   → **F-AR-001 sauber konsolidiert** für die 3 SQL-relevanten Services; `DocsValidator` ist die Ausnahme, die der Plan nicht als Factory verlangt hat.

4. **API-Bruch-Probe**: `ImportService`/`ExportService` haben einen neuen 3./2. ctor-Parameter (`logger`). Verifiziert: Aufrufer sind (a) `Program.cs` `RunImport`/`RunExport` (nutzt Factory, korrekt) und (b) `ImportExportServiceTests` (übergibt `NullLogger<T>.Instance`, korrekt). Kein API-Bruch für externe Konsumenten, weil (i) Parameter ist optional mit Default `null`, (ii) Tests sind die einzigen direkten Aufrufer neben dem Wiring.

5. **`DocsMcpTools`-Ctor-Reihenfolge-Probe**: Plan-Beispiel war falsch. Verifiziert: `DocsMcpTools.cs:12` ist `public sealed class DocsMcpTools(SqlDocumentsStore store, int maxQueryLength, int maxResults, ILogger<DocsMcpTools> logger)`. `Program.cs:138-142` nutzt genau diese Reihenfolge: `(sp.GetRequiredService<SqlDocumentsStore>(), options.Search.MaxQueryLength, options.Search.MaxResults, sp.GetRequiredService<ILogger<DocsMcpTools>>())`. → **Korrekt**, Plan-Fehler vom Coder korrekt umgangen.

6. **SearchDocs-Early-Return-Logging-Probe**: Bei `IsNullOrWhiteSpace(query)` (`:104`) und `query.Length > maxQueryLength` (`:105-110`) wird der "abgeschlossen"-Log nicht erreicht. Folge: ein Operator sieht "SearchDocs startet" ohne Pendant. **Akzeptabel**: Edge-Cases (leere/zu-lange Query), frühzeitige Short-Circuit-Korrektur, nicht-Verhalten-tragend. Plan sagt „öffentliche Methoden loggen Start/Ende" — impliziert normale Ausführung, nicht Exception-Pfade. → **MINOR / Beobachtung**, nicht MAJOR.

### Build-Status

```
$ dotnet build -c Release
Wiederherzustellende Projekte werden ermittelt...
Alle Projekte sind für die Wiederherstellung auf dem neuesten Stand.
  KnowHowToAI.Core -> ...\KnowHowToAI.Core.dll
  KnowHowToAI.Core.Tests -> ...\KnowHowToAI.Core.Tests.dll
  KnowHowToAI.Cli -> ...\KnowHowToAI.Cli.dll

Der Buildvorgang wurde erfolgreich ausgeführt.
    0 Warnung(en)
    0 Fehler

Verstrichene Zeit 00:00:02.51
```

→ **grün**, 0 Warnings, 0 Errors.

### Test-Status

```
$ dotnet test -c Release --no-build
Ausführen von Tests von ...\KnowHowToAI.Core.Tests.dll (net10.0|x64)
... erfolgreich (7s 957ms)

Testlaufzusammenfassung: Bestanden!
  gesamt: 78
  fehlgeschlagen: 0
  erfolgreich: 78
  übersprungen: 0
  Dauer: 8s 314ms
```

→ **grün**, 78/78 (Baseline 78 → 78 stabil). Anmerkung: der Plan nannte 74 als Baseline, das war veraltet — der Coder hat korrekt 78/78 berichtet (Test-Counts haben sich seit Plan-Erstellung durch andere Steps leicht verschoben).

**AiNetLinter-Report direkt gelesen** (`tests/KnowHowToAI.Core.Tests/AiNetLinter/output/lint-report.md`):

```
# Run: 2026-07-26 20:38:24
OK
```

→ 0 Violations. Post-Commit-Run (Commit-Zeit 20:36:57, Report-Zeit 20:38:24 = ~90s später, plausibel als Re-Run direkt nach Commit).

## Findings (bei `issues` — zwingend CRITICAL oder MAJOR)

*Keine CRITICAL- oder MAJOR-Findings.* Verdict bleibt `approved` (siehe Severity-Gating-Regel im Skill).

## Frage an Nutzer (bei `blocked`)

*Entfällt — Verdict ist `approved`.*

## Sonstige Beobachtungen / MINOR / NITPICK (führt NICHT zu issues, Verdict bleibt approved)

1. **Commit-Subject 75 Zeichen** (`fix(arch): core-services mit ilogger-injection und composition-root-factory`) über Regel-Limit `< 70` (`.agents/rules/03-git-workflow.mdc:30`) und über Orchestrator-Schwelle 72. Subject stammt literal aus dem Orchestrator-Auftrag, Coder hat ihn unverändert übernommen. **NITPICK** — Inhaltlich prägnant, kein Build-Bruch. Alternative wäre `fix(arch): ilogger-injection und composition-root` (55 Zeichen) gewesen.

2. **Commit-Typ-Abweichung**: Plan-DoD nannte `feat(observability):...`, Coder nutzte `fix(arch):...`. Conventional Commits erlaubt beide; `fix(arch)` ist für ein Architektur-Consolidierung-Finding (F-AR-002 ist ein Architektur-Fix, nicht ein neues User-Feature) ebenfalls regel-konform. **NITPICK** — Geschmacksfrage, beide vertretbar.

3. **Stil-Abweichung: gespeichertes Feld `_logger` statt Primary-Ctor-Parameter** in `SqlDocumentsStore` (Coder speichert `_logger` als Feld, nutzt `_logger.LogInformation(...)` statt `logger.LogInformation(...)`). Plan-Beispiel zeigte direkten Primary-Parameter-Zugriff. **NITPICK** — gespeichertes Feld ist konsistent mit dem bestehenden `_connectionString`/`_table`-Pattern derselben Klasse und vermeidet Lambda-Capture-Fallen. Eigentlich etwas sauberer.

4. **SearchDocsAsync-Early-Return ohne "abgeschlossen"-Log**: Bei leerer Query (`:104`) oder zu langer Query (`:105-110`) wird der "abgeschlossen"-Log nicht erreicht. Operator sieht im Log-File "SearchDocs startet" ohne Pendant. **MINOR (Observability-Beobachtung)** — kein Verhaltens-Bug, kein Build-Bruch, in Edge-Cases akzeptabel. Falls strikte Start/Ende-Symmetrie gewünscht: expliziter Log vor dem early return + Try/Finally-Block. Out-of-scope für Step 005.

5. **Optionale Beispiel-Log-Zeilen in `docs/03` Abschnitt 2** wurden bewusst übersprungen (Plan-Datei 11, Nice-to-Have). **MINOR / dokumentiert** — sinnvolle Entscheidung, da SQL-Server auf Dev-Rechner nicht verifiziert (siehe `docs/03:94` „Bekannter lokaler Stolperstein"). Fiktive Platzhalter hätten späterer Realität widersprechen können. Bei nächstem erfolgreichen Smoke-Lauf nachziehbar.

6. **`Log.Logger`-Global vs. DI-Logger-Konsistenz**: CLI-Modi und Server-Modus nutzen *zwei* unterschiedliche Wege, `ILogger<T>` aus Serilog abzuleiten (CLI: `LoggerFactory.Create + AddSerilog(dispose: false)`; Server: `builder.Services.AddSerilog(Log.Logger)` + `sp.GetRequiredService<ILogger<T>>()`). Funktional identisch, konzeptuell uneinheitlich. **Beobachtung / Folge-Refactor-Kandidat** (nicht Step-005-relevant). Coder hat es selbst in `step-result.md:103` notiert.

7. **`Microsoft.Extensions.Logging.Abstractions` ist explizit auch in `tests/KnowHowToAI.Core.Tests.csproj`** referenziert (transitiv via Core bereits vorhanden, explizit zur Lesbarkeit). **NITPICK** — Coder hat es selbst in `step-result.md:104` dokumentiert; AiNetLinter und MSBuild stört das nicht. Falls jemand es als Duplikat-Dep wertet, kann die Zeile entfernt werden ohne Tests zu brechen.

8. **`DocsMcpTools`-Factory-Pattern könnte auch über `BuildDocsMcpTools` laufen** für Symmetrie zu den anderen Factory-Funktionen. Aktuell ist `RunServer` der einzige Ort, der `DocsMcpTools` baut, mit einem inline-Lambda. **NITPICK / Folge-Refactor-Kandidat** — Coder hat es in `step-result.md:105` selbst angemerkt. Plan hat das nicht explizit verlangt.

9. **Manueller Smoke-Test (End-to-End) nicht durchgeführt**: Plan-DoD nennt ihn als „bedingt durchführbar" (SQL-Setup-Problem auf Dev-Rechner, siehe `docs/03:94`). Coder hat das dokumentiert und Audit auf Code-Review + Build-Erfolg gestützt. **MINOR / Plan-konform** — kein Verstoß.

10. **Test-Baseline-Diskrepanz**: Plan-DoD sagte 74 Tests, tatsächlich sind es 78. **Beobachtung** — der Coder hat korrekt 78/78 berichtet, der Plan-Wert war veraltet. Kein Coder-Fehler.
