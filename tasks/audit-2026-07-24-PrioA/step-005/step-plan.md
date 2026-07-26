---
status: done
type: step-plan
task: audit-2026-07-24-PrioA
step: 005
title: "F-AR-002 — Core-Services mit ILogger<T>-Injection + Composition-Root-Konsolidierung"
estimated_risk: medium
created_by: planer
created_by_model: claude-sonnet-5
created_by_model_knowledge_cutoff: 2026-01
created_at: 2026-07-26T18:00:00+02:00
related_to:
  - "tasks/audit-2026-07-24-PrioA/Konzept.md#fix-5--f-ar-002-core-services-ohne-iloggert-injection"
  - "tasks/audit-2026-07-24/_findings/F-AR-002-core-services-without-logger.md"
  - "tasks/audit-2026-07-24-PrioA/Konzept.md#schritt-5f--anpassung-an-searchresult-shape-folge-von-f-pe-002"
---

# Step 005: F-AR-002 — Core-Services mit `ILogger<T>`-Injection + Composition-Root-Konsolidierung

## Bezug

- **Task:** `audit-2026-07-24-PrioA`
- **Quelle:** `Konzept.md` Sektion „Fix 5 — F-AR-002: Core-Services ohne
  `ILogger<T>`-Injection" + Schritt 5f (Anpassung an `SearchResult`-Shape
  als Folge von F-PE-002)
- **Phase / Priorität:** Kurzfristig (Architektur, High) — am Schluss
  weil umfangreichster Eingriff
- **Abhängigkeiten:** **baut auf Step 002 + Step 003 auf** — die
  `SearchResult`-Folge-Anpassungen (Schritt 5f im Konzept) sind in
  Step 003 schon in den Code eingezogen (`ResponseSize`-Switch-Arm,
  `DocsMcpTools.SearchDocsAsync` reicht `result` durch). Step 005
  konsolidiert die *Composition-Root-Factory* und fügt `ILogger<T>`-
  Injection überall hinzu.
- **F-AR-001-Konsolidierung:** F-AR-001 (DI-Inkonsistenz) wird in
  diesem Step *nebenbei* über die `BuildCoreServices`-Factory in
  `Program.cs` aufgelöst — `SqlDocumentsStore` und `DocsMcpTools`
  werden überall einheitlich via Factory gebaut, nicht mehr
  teils-via-DI (Server) / teils-via-`new` (CLI-Modi). Konzept
  bestätigt: „F-AR-001 wird *mitgemacht*: die Factory löst die
  Inkonsistenz."

## Intention

Vier Core-Services (`SqlDocumentsStore`, `DocsValidator`, `ImportService`,
`ExportService`) haben heute **kein** `ILogger<T>` — Diagnose von
Produktions-Problemen ist entsprechend schwierig, weil weder „was lief"
noch „wie lange" noch „wie viele" geloggt wird. Der MCP-Server hat
einen `ILogger<DocsMcpTools>` bereits (per DI), aber die tieferen
Schichten sind stumm. Nach diesem Step bekommt jeder Service eine
`ILogger<T>`-Injection, jede öffentliche Methode loggt mindestens
Start/Ende (mit Dauer) und ggf. weitere Strukturpunkte (Anzahl
verarbeiteter Dokumente, Stoppuhr für SQL-Operationen, Marker-Datei-
Entscheidung im Export). `Microsoft.Extensions.Logging.Abstractions`
(~30 KB, nur Interfaces) wird in `KnowHowToAI.Core` referenziert —
Core bleibt unabhängig von konkreten Logging-Backends (Serilog bleibt
exklusiv in Cli). Die `Program.cs`-Factory löst gleichzeitig die
F-AR-001-DI-Inkonsistenz auf, sodass `SqlDocumentsStore` und
`DocsMcpTools` an allen Stellen einheitlich via Factory gebaut werden.

## Konkrete Änderungen

### Datei 1: `src/KnowHowToAI.Core/KnowHowToAI.Core.csproj`

- **Was:** Neuen `PackageReference` in den bestehenden `ItemGroup`
  einfügen (Version-Reihenfolge egal, AiNetLinter prüft das nicht):
  ```xml
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
  ```
- **Warum:** Bringt `ILogger<T>`, `ILoggerFactory`, `NullLogger<T>` in
  Core. Nur Interfaces, keine konkrete Logger-Implementierung — Core
  bleibt frei von Serilog/NLog/etc. Konzept-Vorgabe explizit so.
- **Hinweis:** Existierende `Microsoft.Extensions.Configuration.*`-
  Versionierung in Cli.csproj ist 10.0.9 — gleiche Version hier wählen,
  damit `dotnet restore` keine Versions-Mismatch-Warnung wirft.

### Datei 2: `src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs`

- **Was:** Konstruktor um `ILogger<SqlDocumentsStore>` erweitern, an
  öffentlichen Methoden `LogInformation`-Calls hinzufügen.
  ```csharp
  public sealed class SqlDocumentsStore(
      string connectionString,
      string documentsTableName,
      ILogger<SqlDocumentsStore> logger)
  {
      private readonly string _connectionString = connectionString;
      private readonly string _table = $"dbo.{documentsTableName}";
      // ... existing field assignments via ctor ...

      public async Task ReplaceAllAsync(IReadOnlyList<Document> documents, CancellationToken cancellationToken)
      {
          logger.LogInformation(
              "ReplaceAll startet: {DocumentCount} Dokumente in Tabelle {Table}",
              documents.Count, _table);
          var sw = System.Diagnostics.Stopwatch.StartNew();
          // ... existing body ...
          logger.LogInformation(
              "ReplaceAll abgeschlossen: {DocumentCount} Dokumente in {ElapsedMs}ms",
              documents.Count, sw.ElapsedMilliseconds);
      }

      public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken)
      {
          logger.LogInformation("GetAll startet");
          var sw = System.Diagnostics.Stopwatch.StartNew();
          var result = /* existing body */;
          logger.LogInformation("GetAll abgeschlossen: {DocumentCount} Dokumente in {ElapsedMs}ms",
              result.Count, sw.ElapsedMilliseconds);
          return result;
      }

      public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
      {
          logger.LogInformation("ListChildren(parentSlug={ParentSlug})", parentSlug);
          var result = /* existing body */;
          logger.LogInformation("ListChildren abgeschlossen: {ResultCount} Kinder", result.Count);
          return result;
      }

      public async Task<SearchResult> SearchDocsAsync(
          string query, int maxQueryLength, int maxResults, CancellationToken cancellationToken)
      {
          logger.LogInformation(
              "SearchDocs(query='{Query}', maxQueryLength={MaxQueryLength}, maxResults={MaxResults})",
              query, maxQueryLength, maxResults);
          var result = /* existing body */;
          logger.LogInformation(
              "SearchDocs abgeschlossen: {ResultCount} Treffer, truncated={Truncated}",
              result.Results.Count, result.Truncated);
          return result;
      }

      public async Task<DocumentDetail?> GetDocAsync(string slug, CancellationToken cancellationToken)
      {
          logger.LogInformation("GetDoc(slug='{Slug}')", slug);
          var result = /* existing body */;
          logger.LogInformation(
              "GetDoc abgeschlossen: {ResultState}",
              result is null ? "null" : $"content length={result.Content?.Length ?? 0}");
          return result;
      }
  }
  ```
- **Warum:** Strukturiertes Logging mit `LogInformation` (für
  Beobachtbarkeit) statt `LogDebug` (das im Default-`Information`-
  Level nicht durchkommt). Stoppuhr ist explizit ein
  `System.Diagnostics.Stopwatch` — kein `BenchmarkDotNet`-Ballast.
- **Hinweise:**
  - `logger.LogInformation("...", query, ...)` statt
    `$"SearchDocs(query='{query}')"` — strukturierte Properties
    statt String-Interpolation, damit der Logger sie als Felder
    indizieren kann.
  - Keine `try/catch`-Wrapper um die Log-Calls — die Validierungs-
    /Fehlerbehandlungs-Ballast-Regel (`01-code-style.mdc`) gilt
    auch hier. Wenn eine Exception fliegt, ist der „start"-Log
    genug Kontext; der Top-Level-`catch` in `Program.cs RunXxx`
    loggt den Rest.
  - **Niemals `query` direkt in der Log-Message bei sehr großen
    Queries** — aber `MaxQueryLength = 200` cappt das ohnehin.
    Serilog schreibt *nur* in die rotierende Datei (`docs/02`
    Abschn. 2), nicht in `Console.Out` — also kein
    JSON-RPC-Korruptionsrisiko.

### Datei 3: `src/KnowHowToAI.Core/Validation/DocsValidator.cs`

- **Was:** Konstruktor um `ILogger<DocsValidator>` erweitern, an
  `Validate` Logging hinzufügen.
  ```csharp
  public sealed partial class DocsValidator(
      int maxContentLengthWarning = 8000,
      ILogger<DocsValidator>? logger = null)
  {
      // ... existing fields ...

      public ValidationResult Validate(string docsRootPath)
      {
          logger?.LogInformation("Validate startet: docsRoot='{DocsRoot}'", docsRootPath);
          var sw = System.Diagnostics.Stopwatch.StartNew();
          var result = /* existing body */;
          logger?.LogInformation(
              "Validate abgeschlossen: {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms",
              result.Errors.Count, result.Warnings.Count, sw.ElapsedMilliseconds);
          return result;
      }
  }
  ```
- **Warum:** Konsistent mit Step 005 Plan, `logger` ist optional
  (`= null`) für Rückwärts-Kompatibilität mit bestehenden
  `new DocsValidator()`-Aufrufen in Tests (AiNetLinter
  `EnforceNullableEnable` lässt `?`-Parameter zu). Empfehlung
  des Konzepts: *null* statt `NullLogger<T>.Instance` als Default,
  damit die Tests keine `using Microsoft.Extensions.Logging`-Imports
  brauchen, wenn sie den Logger nicht nutzen. Auditer kann das
  hinterfragen — Begründung: einfacher Test-Setup ist Teil der
  „bewusst einfacher Code"-Regel.

### Datei 4: `src/KnowHowToAI.Core/Sync/ImportService.cs`

- **Was:** Positional record um `ILogger<ImportService>` erweitern.
  ```csharp
  public sealed class ImportService(
      Func<IReadOnlyList<Document>, CancellationToken, Task> replaceAllAsync,
      int maxContentLengthWarning = 8000,
      ILogger<ImportService>? logger = null)
  {
      private readonly DocsValidator _validator = new(maxContentLengthWarning);
      // ... existing body ...

      public async Task<ValidationResult> ImportAsync(string docsRootPath, CancellationToken cancellationToken = default)
      {
          logger?.LogInformation("Import startet: docsRoot='{DocsRoot}'", docsRootPath);
          var sw = System.Diagnostics.Stopwatch.StartNew();
          // ... existing body (read+validate+replace) ...
          logger?.LogInformation(
              "Import abgeschlossen: {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms",
              result.Errors.Count, result.Warnings.Count, sw.ElapsedMilliseconds);
          return result;
      }
  }
  ```
- **API-Bruch:** Neuer 3. Konstruktor-Parameter zwingt zu Update
  *aller* Aufrufer (Tests, `Program.cs` `RunImport`).
  `ILogger<ImportService>? logger = null` als optionaler Parameter
  hält Test-Aufrufer rückwärts-kompatibel — bestehende `new
  ImportService(replaceAllAsync)`-Aufrufe kompilieren weiterhin.

### Datei 5: `src/KnowHowToAI.Core/Sync/ExportService.cs`

- **Was:** Analog zu `ImportService` — positional record um
  `ILogger<ExportService>?` erweitern, Logging in `ExportAsync`
  (Start/Ende + Marker-Datei-Entscheidung) und `PrepareTargetDirectory`
  (über Decision-Log).
  ```csharp
  public sealed class ExportService(
      Func<CancellationToken, Task<IReadOnlyList<Document>>> getAllAsync,
      ILogger<ExportService>? logger = null)
  {
      // ... existing body ...

      public async Task ExportAsync(string targetDirectory, string exportMarkerFileName, CancellationToken cancellationToken = default)
      {
          logger?.LogInformation(
              "Export startet: target='{Target}', markerFile='{MarkerFile}'",
              targetDirectory, exportMarkerFileName);
          var sw = System.Diagnostics.Stopwatch.StartNew();
          // ... existing body ...
          logger?.LogInformation(
              "Export abgeschlossen: {DocumentCount} Dokumente, {ElapsedMs}ms",
              /* DocumentCount */, sw.ElapsedMilliseconds);
      }

      private static void PrepareTargetDirectory(string targetDirectory, string exportMarkerFileName)
      {
          // ... existing body, mit Log-Decision ...
          // Falls das Verhalten sich nicht ändert, ist hier kein Log zwingend -
          // die Entscheidung (Marker-Datei gefehlt / neue Marker angelegt) ist
          // bereits im Top-Level-Error-Case sichtbar. Empfehlung: KEIN Log hier.
      }
  }
  ```
- **API-Bruch:** wie `ImportService` — `ILogger<ExportService>?` als
  optionaler Parameter hält Aufrufer rückwärts-kompatibel.

### Datei 6: `src/KnowHowToAI.Cli/Program.cs`

- **Was:** Drei Stellen:
  1. **Composition-Root-Factory** als `static` Helper-Funktionen
     einführen:
     ```csharp
     static SqlDocumentsStore BuildStore(
         KnowHowToAiOptions options,
         ILogger<SqlDocumentsStore> storeLogger) =>
         new(options.ConnectionString, options.DocumentsTableName, storeLogger);

     static ImportService BuildImportService(
         KnowHowToAiOptions options,
         SqlDocumentsStore store,
         ILogger<ImportService> importLogger) =>
         new(store.ReplaceAllAsync, options.Validation.MaxContentLengthWarning, importLogger);

     static ExportService BuildExportService(
         SqlDocumentsStore store,
         ILogger<ExportService> exportLogger) =>
         new(store.GetAllAsync, exportLogger);
     ```
  2. **`RunValidate`**: `new DocsValidator(...)` durch
     `new DocsValidator(maxContentLengthWarning, Log.Logger.ForContext<DocsValidator>())` ersetzen.
     Konkret:
     ```csharp
     var result = new DocsValidator(
         options.Validation.MaxContentLengthWarning,
         Log.Logger.ForContext<DocsValidator>()).Validate(options.DocsRootPath);
     ```
  3. **`RunImport`**: `SqlDocumentsStore`/`ImportService`-Konstruktion
     durch `BuildStore`/`BuildImportService` ersetzen.
  4. **`RunExport`**: gleiche Umstellung.
  5. **`RunServer`**: die Factory-Lambdas aus Step 003 verfeinern —
     `SqlDocumentsStore` und `DocsMcpTools` aus `BuildStore` /
     einem analogen `BuildDocsMcpTools` (in `Program.cs`):
     ```csharp
     builder.Services.AddSingleton<SqlDocumentsStore>(sp => BuildStore(
         options,
         sp.GetRequiredService<ILogger<SqlDocumentsStore>>()));
     builder.Services.AddSingleton(sp => new DocsMcpTools(
         sp.GetRequiredService<SqlDocumentsStore>(),
         sp.GetRequiredService<ILogger<DocsMcpTools>>(),
         options.Search.MaxQueryLength,
         options.Search.MaxResults));
     ```
     `builder.Services.AddSerilog(Log.Logger)` registriert bereits
     `ILogger<T>` für alle `T` via `ILoggerFactory` — `sp.GetRequiredService<ILogger<...>>()`-Aufrufe lösen den
     Serilog-Backend-Logger korrekt auf.
- **Warum:** Konsolidiert F-AR-001 (DI-Inkonsistenz) und F-AR-002
  (Logger-Injection) in einem Schritt. `BuildStore` ist die einzige
  Stelle, die `SqlDocumentsStore` konstruiert — Server-Modus (DI)
  und CLI-Modi (manuell) gehen durch dieselbe Factory.
- **Hinweise zu `Log.Logger.ForContext<T>()`:** Serilog-Syntax, die
  `ILogger<T>` aus dem globalen `Log.Logger` ableitet. Vorteil:
  kein zusätzlicher DI-Container in CLI-Modi nötig. Nachteil:
  Serilog-Coupling — *aber* `Program.cs` ist ohnehin Serilog-
  abhängig (Konfiguration in `ConfigureLogger`). Akzeptabel.
- **`RunServer` Logger-Auflösung:** wenn `AddSerilog(Log.Logger)`
  nicht ausreicht, ist `builder.Services.AddSingleton<ILoggerFactory>(sp =>
  LoggerFactory.Create(b => b.AddSerilog(Log.Logger)))` die
  Fallback-Option. Konzept-Vorgabe nennt das explizit; der Coder
  soll die Variante wählen, die mit der aktuellen `AddSerilog`-
  Version funktioniert.

### Datei 7: `tests/KnowHowToAI.Core.Tests/ImportExportServiceTests.cs`

- **Was:** Bestehende Test-Klassen anpassen — neue Konstruktor-Parameter
  mit `NullLogger<T>.Instance` durchreichen:
  ```csharp
  using Microsoft.Extensions.Logging.Abstractions;

  // ImportServiceTests:
  var service = new ImportService(
      (_, _) => { replaceCallCount++; return Task.CompletedTask; },
      maxContentLengthWarning: 8000,
      logger: NullLogger<ImportService>.Instance);

  // ExportServiceTests:
  var service = new ExportService(
      (_) => Task.FromResult<IReadOnlyList<Document>>([document]),
      NullLogger<ExportService>.Instance);
  ```
- **Warum:** `logger` ist ein optionaler Parameter mit Default
  `null` — die Tests *könnten* also ohne `NullLogger` auskommen.
  Konzept-Vorgabe und bessere Test-Praxis: explizit `NullLogger<T>
  .Instance` durchreichen, damit (a) der Test nicht von der
  impliziten `null`-Akzeptanz abhängt, (b) eine zukünftige
  Refactoring-Änderung am Default-Wert keine Tests bricht.

### Datei 8: `tests/KnowHowToAI.Core.Tests/DocsValidatorTests.cs`

- **Was:** Bestehende `_validator = new DocsValidator();` durch
  `_validator = new DocsValidator(logger: NullLogger<DocsValidator>
  .Instance);` ersetzen, falls die Tests Log-Aufrufe verifizieren
  sollen. Wenn nicht, kann `new DocsValidator()` bleiben — der
  optionale Parameter macht das möglich.
- **Konzept-Vorgabe:** *„Tests anpassen mit `NullLogger<T>.Instance`"*
  — der Coder entscheidet, ob er die Tests explizit mit Logger
  versieht (besser) oder den Default `null` nutzt (einfacher).
  Beides ist regel-konform. *Empfehlung: explizit mit Logger*
  (Konsistenz mit `ImportExportServiceTests`-Pattern).

### Datei 9: `docs/02-Architektur-und-Techstack.md` (Abschnitt 2, Tech-Stack-Tabelle)

- **Was:** Eine neue Zeile in der Tabelle „Tech-Stack & Dependencies":
  ```
  | Logging-Abstraktion | `Microsoft.Extensions.Logging.Abstractions` | Nur Interfaces, in Core referenziert, damit Core-Services `ILogger<T>` per Konstruktor akzeptieren ohne konkrete Backend-Bindung (Serilog bleibt exklusiv in Cli) |
  ```
  Außerdem: in der Tabelle bei `Logging` (Serilog) den Hinweis
  ergänzen: „Konkrete Implementierung *ausschließlich* in Cli (via
  Serilog-Backend), Core nutzt nur die Abstraktion."
- **Warum:** Konsistent mit der Tabelle; explizit macht den
  Architektur-Schnitt klar (Core kennt nur Interfaces).

### Datei 10: `docs/03-Projektstruktur-und-Konfiguration.md` (Abschnitt 1, Solution-Layout)

- **Was:** Im Abschnitt "`KnowHowToAI.Core`" (Z. 25-33) eine
  Aufzählung ergänzen:
  ```
  * Alle öffentlichen Services in Core (`SqlDocumentsStore`,
    `DocsValidator`, `ImportService`, `ExportService`) erwarten
    `ILogger<T>` per Konstruktor. Default ist `null` (kein
    Logging); Production-Code in Cli reicht den Serilog-Backend-
    Logger durch.
  ```
  Im Abschnitt "`KnowHowToAI.Cli`" (Z. 35-39) eine Aufzählung:
  ```
  * `Program.cs` enthält die Composition-Root-Factory
    (`BuildStore`/`BuildImportService`/`BuildExportService`) — alle
    Services werden an einer einzigen Stelle konstruiert (kein
    `new SqlDocumentsStore(...)` verstreut über Run-Methoden).
  ```
- **Warum:** Dokumentationspflicht, neue Dependency in Core +
  Composition-Root-Pattern in Cli. Konsistent mit `04-docs-reference.mdc`.

### Datei 11 (Nice-to-Have, Konzept-Empfehlung): Beispiel-Logs

- **Was:** In `docs/03` Abschnitt 2 (Konfiguration) am Ende des
  `Logging`-Beispielblocks einen „Beispiel-Log"-Eintrag ergänzen:
  ```
  * **Beispiel-Log-Zeilen** (nach `import`-Lauf, aus
    `Logs/knowhowtoai-<Datum>.log`):
    ```
    2026-07-26 18:00:00 INF Import startet: docsRoot='C:\...\demo-docs'
    2026-07-26 18:00:00 INF Validate abgeschlossen: 0 Fehler, 0 Warnungen, 23ms
    2026-07-26 18:00:01 INF ReplaceAll startet: 12 Dokumente in Tabelle dbo.documents
    2026-07-26 18:00:01 INF ReplaceAll abgeschlossen: 12 Dokumente in 412ms
    2026-07-26 18:00:01 INF Import abgeschlossen: 0 Fehler, 0 Warnungen, 1456ms
    ```
  ```
  Diese Zeilen sind *Platzhalter-Illustration* — der Coder passt
  sie an die tatsächlichen Log-Strings an, falls er sie nicht 1:1
  aus dem Code übernehmen will.
- **Warum:** Konzept-Nice-to-Have („Beispiel-Logs im Doku-Update zu
  F-AR-002"), LLM-/Operator-UX-Mehrwert. *Optional* — wenn der
  Coder die Zeilen nicht übernehmen will, ist das kein Verstoß.

## Tests

- [ ] `ImportServiceTests.ImportAsync_InvalidDocs_ReturnsErrorsAndDoesNotReplaceAnything`
      — bestehender Test kompiliert weiter (logger-Parameter neu,
      `NullLogger<ImportService>.Instance` durchgereicht)
- [ ] `ImportServiceTests.ImportAsync_ValidDocs_ReplacesWithParsedDocuments`
      — bestehender Test kompiliert weiter (selbe Anpassung)
- [ ] `ExportServiceTests.ExportAsync_NewTargetDirectory_CreatesMarkerAndWritesDocuments`
      — bestehender Test kompiliert weiter
- [ ] `ExportServiceTests.ExportAsync_ExistingMarker_WipesOldMarkdownBeforeReExport`
      — bestehender Test kompiliert weiter
- [ ] `ExportServiceTests.ExportAsync_ForeignFilesWithoutMarker_ThrowsAndDoesNotCallGetAll`
      — bestehender Test kompiliert weiter
- [ ] `DocsValidatorTests.*` (alle bestehenden Tests) — kompilieren
      weiter, ggf. mit explizitem `NullLogger<DocsValidator>.Instance`

**Optional, vom Coder zu entscheiden:**
- [ ] `ImportServiceTests.ImportAsync_LogsStartAndEnd` — Test mit
      `Microsoft.Extensions.Logging.Abstractions.Testing`-Package,
      das Logger-Output aufzeichnet und assertiert. Nicht zwingend,
      weil das Logging selbst kein Programmlogik-tragender Pfad ist
      (es ist reine Beobachtbarkeit). Empfehlung: *nicht* in diesem
      Step — der Logger-Aufruf ist visuell im Log-File prüfbar
      (siehe manueller Smoke in der DoD).

**Test-Datei (erweitert):** `tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj`
braucht `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions"
Version="10.0.9" />` (selbe Version wie in Core, sonst Mismatch). Diese
Dependency ist nur für die Tests nötig (für `NullLogger<T>.Instance`) —
wird aber in Core ohnehin transitiv mitgezogen, da Core das Paket
jetzt referenziert.

**Bekannte Test-Baseline:** 74 → 74 (keine neuen Tests, nur Anpassungen).

## Definition of Done

- [ ] `Microsoft.Extensions.Logging.Abstractions` Version 10.0.9 in
      `KnowHowToAI.Core.csproj` referenziert
- [ ] `SqlDocumentsStore` ctor: `(connectionString, documentsTableName,
      ILogger<SqlDocumentsStore>)` + Log-Calls in 5 öffentlichen Methoden
- [ ] `DocsValidator` ctor: `(int maxContentLengthWarning = 8000,
      ILogger<DocsValidator>? logger = null)` + Log-Calls in `Validate`
- [ ] `ImportService` ctor (positional record): `(replaceAllAsync,
      int maxContentLengthWarning = 8000, ILogger<ImportService>?
      logger = null)` + Log-Calls in `ImportAsync`
- [ ] `ExportService` ctor (positional record): `(getAllAsync,
      ILogger<ExportService>? logger = null)` + Log-Calls in
      `ExportAsync`
- [ ] `Program.cs` enthält `BuildStore`/`BuildImportService`/
      `BuildExportService` Factory-Funktionen; alle 4 Run-Methoden
      (Validate/Import/Export/Server) nutzen sie (statt verteiltem
      `new`-Aufruf); `RunServer` löst `ILogger<T>` via
      `GetRequiredService<ILogger<T>>()` auf
- [ ] `ImportExportServiceTests.cs`: `NullLogger<T>.Instance` an
      `ImportService`/`ExportService`-Konstruktor durchgereicht
- [ ] `DocsValidatorTests.cs`: ggf. `NullLogger<DocsValidator>
      .Instance` durchgereicht
- [ ] `tests/KnowHowToAI.Core.Tests.csproj` referenziert
      `Microsoft.Extensions.Logging.Abstractions` (für `NullLogger<T>`)
- [ ] `docs/02` Abschnitt 2: Tech-Stack-Tabelle um
      `Logging-Abstraktion`-Zeile erweitert
- [ ] `docs/03` Abschnitt 1: `ILogger<T>`-Erwartung +
      `Composition-Root-Factory`-Hinweis ergänzt
- [ ] Optional: `docs/03` Abschnitt 2: Beispiel-Log-Zeilen
- [ ] `dotnet build -c Release` — 0 Warnings, 0 Errors
- [ ] `dotnet test` — 74 grün (keine neuen Tests, nur Anpassungen)
- [ ] AiNetLinter 0 neue Verstöße
- [ ] Commit mit Subject
      `feat(observability): ilogger-injection in core-services + composition-root-konsolidierung`,
      Body: „Alle vier Core-Services (`SqlDocumentsStore`,
      `DocsValidator`, `ImportService`, `ExportService`) erwarten
      `ILogger<T>` per Konstruktor; öffentliche Methoden loggen
      Start/Ende mit `Stopwatch`-Dauer, relevante Strukturpunkte
      (Dokument-Counts, Truncated-Flag) als strukturierte Properties.
      `Program.cs` enthält jetzt eine einheitliche Composition-Root-
      Factory (`BuildStore`/`BuildImportService`/`BuildExportService`) —
      alle Services werden an einer Stelle konstruiert (löst
      F-AR-001-DI-Inkonsistenz nebenbei mit auf). Serilog bleibt
      exklusiv in Cli; Core referenziert nur
      `Microsoft.Extensions.Logging.Abstractions` (Interfaces).
      Tests nutzen `NullLogger<T>.Instance`."
      Trailer: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`
- [ ] `step-005/step-result.md` geschrieben mit Commit-Hash
- [ ] `status` in `step-plan.md` von `in_progress` auf `done (pending audit)`
      gesetzt

## Rules-Refs

- `.agents/rules/01-code-style.mdc` — Early Returns unverändert,
  keine Kommentare; Service-Konstruktoren behalten ihre Pattern
  (positional records für `ImportService`/`ExportService`,
  ctor-Block für `SqlDocumentsStore`)
- `.agents/rules/02-testing.mdc` — Tests mit `NullLogger<T>.Instance`
  angepasst; keine neuen *Pflicht*-Tests für Log-Aufrufe (reine
  Beobachtbarkeit)
- `.agents/rules/03-git-workflow.mdc` — Conventional Commit, deutsch
- `.agents/rules/05-documentation.mdc` — Doku im selben Commit
- `.agents/rules/06-configuration.mdc` — `Microsoft.Extensions.Logging
  .Abstractions` ist eine Library-Dependency, *kein*
  Schwellenwert — keine `appsettings.json`-Auswirkung
- `.agents/rules/AiNetLinter.mdc` —
  - `MaxConstructorDependencies: 5` für `SqlDocumentsStore` jetzt 3
    Params (bleibt unter Limit)
  - `MaxConstructorDependencies: 5` für `ImportService` jetzt 3 Params
    (bleibt unter Limit)
  - `MaxConstructorDependencies: 5` für `ExportService` jetzt 2 Params
    (bleibt unter Limit)
  - `MaxMethodLineCount: 60` — die Service-Methoden bleiben unter
    Limit (Log-Calls fügen ~3-5 Zeilen hinzu, Methoden wachsen von
    ~15 auf ~20-25 Zeilen)

## Bekannte Ausnahmen

- **Keine Tests für die Logger-Aufrufe selbst** (kein
  `LoggerTesting`-Package). Begründung: Logger-Aufrufe sind reine
  Beobachtbarkeit, kein Programmlogik-tragender Pfad. Der Auditer
  prüft visuell, dass die Log-Strings sinnvoll sind, und ein
  manueller Smoke (Bedingt in DoD) zeigt, dass die Logs in der
  Serilog-Datei landen.
- **Optionale Tests mit `NullLogger<T>.Instance` vs. Default `null`:
  bestehende Tests funktionieren mit beiden Varianten, da der
  Logger-Parameter als optional (`= null`) deklariert ist. Der
  Coder entscheidet, welche Variante sauberer ist.
- **Manueller Smoke (End-to-End):** *bedingt* durch SQL-Setup-Problem.
  Wenn durchführbar: `import`-Lauf gegen DemoDB ausführen, danach
  in `Logs/knowhowtoai-<Datum>.log` kontrollieren, dass die neuen
  Log-Zeilen (Import-Start, ReplaceAll-Start, ReplaceAll-Ende,
  Import-Ende) erscheinen. Wenn nicht durchführbar: dokumentieren
  in `step-005/step-result.md` und `task-summary.md`, Audit-Verdict
  auf Basis Code-Review + Build-Erfolg.

## Code-Skizze

```csharp
// Program.cs - neue Factory-Helper

static SqlDocumentsStore BuildStore(
    KnowHowToAiOptions options,
    ILogger<SqlDocumentsStore> storeLogger) =>
    new(options.ConnectionString, options.DocumentsTableName, storeLogger);

static ImportService BuildImportService(
    KnowHowToAiOptions options,
    SqlDocumentsStore store,
    ILogger<ImportService> importLogger) =>
    new(store.ReplaceAllAsync, options.Validation.MaxContentLengthWarning, importLogger);

static ExportService BuildExportService(
    SqlDocumentsStore store,
    ILogger<ExportService> exportLogger) =>
    new(store.GetAllAsync, exportLogger);

// RunImport - umgebaut
async Task<int> RunImport(ParseResult parseResult, CancellationToken cancellationToken)
{
    try
    {
        var options = LoadOptions(parseResult.GetValue(configOption));
        Log.Logger = ConfigureLogger(options.Logging);

        await SchemaMigrator.MigrateAsync(
            options.ConnectionString, options.DocumentsTableName, message => Log.Logger.Information(message), cancellationToken);

        var store = BuildStore(options, Log.Logger.ForContext<SqlDocumentsStore>());
        var importService = BuildImportService(options, store, Log.Logger.ForContext<ImportService>());
        var result = await importService.ImportAsync(options.DocsRootPath, cancellationToken);
        return PrintValidationResult(result);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
}
```

## Notes

- **Reihenfolge im Loop:** Step 005 ist der letzte Schritt. Begründung:
  umfangreichster Eingriff (5 Service-Dateien + `Program.cs` + Tests
  + 2 Doku-Dateien), außerdem baut die Factory die in Step 003
  begonnenen Factory-Lambdas aus. Wenn der Coder die Reihenfolge
  umdrehen will, wäre das technisch möglich — aber die Konzept-
  Empfehlung ist explizit „am Schluss weil umfangreichster Eingriff".
- **Schritt 5f (SearchResult-Shape-Anpassungen) ist bereits durch
  Step 003 erledigt** — `ResponseSize.Measure` hat den
  `SearchResult`-Switch-Arm, `DocsMcpTools.SearchDocsAsync` reicht
  `result` (Wrapper) durch. Step 005 ändert daran nichts mehr.
- **F-AR-001-Konsolidierung via Factory:** das ist der
  „nebenbei"-Effekt dieses Schritts. Wenn der Coder die F-AR-001-
  Punkte separat im Audit reviewt, sollte er explizit bestätigen,
  dass `new SqlDocumentsStore(...)` nur noch *in* `BuildStore`
  vorkommt — nicht mehr in `RunImport`/`RunExport`/`RunServer`.
- **Logger-Format-Konsistenz:** die Log-Strings verwenden
  durchgängig `LogInformation("Subject Verb: {Property}", value)`-
  Form (Subjekt + Verb + Property-Platzhalter). Das ist
  Serilog-Konvention und konsistent mit den bestehenden
  `Log.Logger.Information(message)`-Aufrufen in `Program.cs`.
- **`Microsoft.Extensions.Logging.Abstractions` Version 10.0.9:** die
  Version 10.0.9 ist die gleiche wie `Microsoft.Extensions.Configuration.*`
  in `KnowHowToAI.Cli.csproj`. Konzept-Vorgabe explizit diese
  Version. Falls `dotnet add package` eine neuere 10.x-Version
  vorschlägt, soll der Coder bei 10.0.9 bleiben (Versions-Mismatch
  vermeiden).
- **Beispiel-Log-Zeilen in Doku:** Konzept-Nice-to-Have. Wenn der
  Coder die Zeilen nicht 1:1 übernehmen will, kann er sie
  weglassen oder an die realen Log-Strings anpassen — das ist
  kein Verstoß gegen die DoD.
- **Nicht im Scope:** `LogResponseSize` (existiert nicht mehr in
  dieser Form — `ResponseSize.Measure` ist die aktuelle Lösung aus
  Commit `d262095`); `Microsoft.Extensions.Logging`-Package
  (*konzrete Implementierung*, nicht die Abstractions) in Core
  referenzieren; `Log.Logger` global durch ein DI-`ILogger<T>`-
  Setup ersetzen (das wäre ein größerer Architektur-Schritt, nicht
  Teil dieses Tasks).
