# Dimension 3 — Architektur & Patterns

> **Vergleichsbasis:** `.agents/rules/01-code-style.mdc` (bewusst einfacher Code, keine
> Interface-Wüsten, Early Returns), `.agents/rules/04-docs-reference.mdc` (docs als Source
> of Truth), sowie Clean-Architecture-Grundprinzipien (Layering, Composition Root, DI).
> **Methodik:** Statische Analyse der Schicht-Trennung, Service-Konstruktion, Logging-Verdrahtung
> und Error-Handling-Konsistenz. Vergleich der vier CLI-Commands untereinander.

## Architektur-Snapshot

```
+------------------------------------------------------------------+
| KnowHowToAI.Cli                                                  |
|  Program.cs  ─────>  System.CommandLine (4 Sub-Commands)         |
|       │                                                         |
|       ├── RunValidate()   ──>  new DocsValidator(...)            |
|       ├── RunImport()     ──>  SchemaMigrator.MigrateAsync      |
|       │                  ──>  new SqlDocumentsStore(...)         |
|       │                  ──>  new ImportService(store.ReplaceAll) |
|       ├── RunExport()     ──>  new SqlDocumentsStore(...)         |
|       │                  ──>  new ExportService(store.GetAll)    |
|       └── RunServer()     ──>  Host.CreateApplicationBuilder()   |
|                              ──>  AddSingleton<SqlDocumentsStore> |
|                              ──>  AddMcpServer().WithToolsFromAssembly() |
+------------------------------------------------------------------+
              │
              │ Project-Ref
              ▼
+------------------------------------------------------------------+
| KnowHowToAI.Core                                                |
|  Documents/  Document, DocumentSummary, DocumentDetail,          |
|              FrontMatterParser, SlugRules                        |
|  Validation/ DocsValidator, ValidationResult, ValidationError   |
|  Migrations/ SchemaMigrator, SqlScript (record)                 |
|  Sync/       ImportService, ExportService, SqlDocumentsStore,   |
|              SqlIdentifierValidator                             |
|  Configuration/ KnowHowToAiOptions, ...LoggingOptions, ...ValidationOptions |
+------------------------------------------------------------------+
              │
              │ Project-Ref
              ▼
+------------------------------------------------------------------+
| KnowHowToAI.Core.Tests  (xUnit v3 + MTP)                        |
|  SlugRulesTests, FrontMatterParserTests, DocsValidatorTests,     |
|  ImportExportServiceTests (Import + Export),                    |
|  SchemaMigratorTests, SqlIdentifierValidatorTests,               |
|  AiNetLinterTests                                                |
+------------------------------------------------------------------+
```

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-AR-001](#f-ar-001) | **High** | DI-Inkonsistenz: `RunValidate`/`RunImport`/`RunExport` nutzen `new`, `RunServer` nutzt den DI-Container | `Cli/Program.cs:64, 84-86, 105-107, 130` |
| [F-AR-002](#f-ar-002) | **High** | `ImportService`/`ExportService` haben **keine** `ILogger`-Injection — `SqlDocumentsStore` und `DocsValidator` ebenfalls nicht | `Sync/ImportService.cs:9`, `Sync/ExportService.cs:8`, `Sync/SqlDocumentsStore.cs:11`, `Validation/DocsValidator.cs:8` |
| [F-AR-003](#f-ar-003) | Medium | `LogResponseSize` (Tool-Response-Logging) liegt in `DocsMcpTools`, nicht im Store — Verantwortung an der falschen Schicht | `McpTools/DocsMcpTools.cs:43-44` |
| [F-AR-004](#f-ar-004) | Medium | `SqlDocumentsStore` ist Singleton, aber Thread-Safety nicht dokumentiert (konsistent zur `docs/04` Edge-Case-4.3-Beschreibung) | `Sync/SqlDocumentsStore.cs:11` |
| [F-AR-005](#f-ar-005) | Medium | Keine zentrale `Constants`-Datei — `FrontMatterParser.delimiter` als Vor-Bote | `Documents/FrontMatterParser.cs:59` |
| [F-AR-006](#f-ar-006) | Low | `Microsoft.Extensions.Logging`-Abhängigkeit nur in `DocsMcpTools`, nicht in Core-Services — Core ist "blind" für strukturiertes Logging | `McpTools/DocsMcpTools.cs:14` |
| [F-AR-007](#f-ar-007) | Low | Service-Lifetimes (Singleton vs. Transient) sind nicht explizit dokumentiert in `docs/03` | (kein Code-Fix, Doku-Fix) |
| [F-AR-008](#f-ar-008) | Info | `sealed` als Class-Lock konsistent angewendet — Cross-Cutting-Stil diszipliniert | (mehrere) |
| [F-AR-009](#f-ar-009) | Info | `partial class DocsValidator` mit `GeneratedRegex` — idiomatisch, AiNetLinter-konform | `Validation/DocsValidator.cs:8` |
| [F-AR-010](#f-ar-010) | Info | Error-Handling ist konsistent: alle vier CLI-Commands fangen `Exception` an der Top-Level und liefern Exit-Code 2 | `Cli/Program.cs:67, 89, 112, 140` |

## Detail-Findings

### F-AR-001 — DI-Inkonsistenz zwischen CLI-Commands

**Schweregrad:** High (Cross-Cutting-Inkonsistenz, mittelfristige Wartungs-Falle)

**Beobachtung:**
Innerhalb *einer* Datei (`Program.cs`) werden Services je nach Command-Pfad auf zwei
völlig unterschiedliche Weisen konstruiert:

| Command | Service-Konstruktion |
| --- | --- |
| `RunValidate` | `new DocsValidator(options.Validation.MaxContentLengthWarning)` |
| `RunImport` | `new SqlDocumentsStore(...)` + `new ImportService(store.ReplaceAllAsync, ...)` |
| `RunExport` | `new SqlDocumentsStore(...)` + `new ExportService(store.GetAllAsync)` |
| `RunServer` | `Host.CreateApplicationBuilder()` + `AddSingleton<SqlDocumentsStore>` + `AddMcpServer(...).WithToolsFromAssembly()` |

**Konsequenzen:**
1. **Doppelte ConnectionString-Validierung:** `SqlDocumentsStore`-Konstruktor ruft
   `SqlIdentifierValidator.EnsureValid` auf (Zeile 18). In `RunImport`/`RunExport` passiert
   das einmal pro Command-Lauf. In `RunServer` einmal beim App-Build. Inkonsistent.
2. **Inkonsistente Test-Coverage:** `ImportService` ist getestet (über Delegate-Injection),
   `SqlDocumentsStore` ist *nicht* getestet (siehe Dim 4). Aber: in `RunImport` werden
   beide ohne Schutzwall zwischen Test und Produktion verkettet.
3. **Doppelter ConnectionString-Pool:** `Microsoft.Data.SqlClient` macht automatisch
   Connection-Pooling basierend auf dem ConnectionString. Zwei `new SqlDocumentsStore(...)`
   mit demselben ConnectionString teilen sich denselben Pool → OK. Aber: Wenn der Pool
   jemals konfigurierbar wird (z.B. pro DB-Connection eigene Settings), wäre das schwer
   zu refaktorieren.
4. **Schwerer zu refaktorisieren:** Wenn morgen z.B. ein `CachingDocumentsStore`-Decorator
   eingeführt wird, müsste das an *drei* Stellen in `Program.cs` plus in `RunServer`
   passieren, statt an *einer* (Composition Root für `SqlDocumentsStore`).

**Fix-Empfehlung:**
1. Composition-Root-Pattern einführen: Eine zentrale Factory-Funktion
   `BuildCoreServices(KnowHowToAiOptions options)`, die `DocsValidator`,
   `SqlDocumentsStore`, `ImportService`, `ExportService` konstruiert und als
   `(DocsValidator validator, SqlDocumentsStore store, ImportService import,
   ExportService export)` Tupel zurückgibt.
2. Im `server`-Pfad: Diese Factory via `IServiceCollection.AddSingleton<SqlDocumentsStore>(sp => ...)`
   registrieren, sodass der gleiche Code in beiden Pfaden läuft.
3. Optional: `ImportService`/`ExportService` auch per DI registrieren (als Transient oder
   Scoped) — wird im `server`-Pfad nicht gebraucht, aber im `import`/`export`-Pfad.

**Beispiel-Refactor:**
```csharp
static (DocsValidator validator, SqlDocumentsStore store, ImportService import, ExportService export)
    BuildCoreServices(KnowHowToAiOptions options)
{
    var store = new SqlDocumentsStore(options.ConnectionString, options.DocumentsTableName);
    var validator = new DocsValidator(options.Validation.MaxContentLengthWarning);
    var import = new ImportService(store.ReplaceAllAsync, options.Validation.MaxContentLengthWarning);
    var export = new ExportService(store.GetAllAsync);
    return (validator, store, import, export);
}

// RunImport:
var (_, store, import, _) = BuildCoreServices(options);
await SchemaMigrator.MigrateAsync(...);
var result = await import.ImportAsync(options.DocsRootPath, cancellationToken);

// RunServer:
builder.Services.AddSingleton(sp => BuildCoreServices(options).store);
```

**Aufwand:** ~30 Minuten + Test-Run.

---

### F-AR-002 — Core-Services ohne `ILogger`-Injection

**Schweregrad:** High (Beobachtbarkeit, mittelfristige Wartungs-Falle)

**Beobachtung:**
Vier Core-Services haben **keine** `ILogger<T>`-Injection:
- `ImportService` (Zeile 9: nimmt nur `Func<...>` und `int maxContentLengthWarning`)
- `ExportService` (Zeile 8: nur `Func<...>`)
- `SqlDocumentsStore` (Zeile 11: nur `string` × 2)
- `DocsValidator` (Zeile 8: nur `int`)

Konsequenzen:
- `SqlDocumentsStore.ReplaceAllAsync` weiß nicht, in welcher Bibliothek es gerade läuft
  (kann nicht loggen "Import für 'sage100'-Bibliothek gestartet").
- `DocsValidator.Validate` kann nicht loggen "validate gestartet für 142 Dateien in 'C:\...'",
  sondern muss vom Aufrufer mit-protokolliert werden.
- `ImportService.ImportAsync` kann nicht loggen "Transaktion gestartet, 142 Dokumente
  eingefügt, Transaktion committed" — der `validate`-Output geht auf `Console`, der
  eigentliche Import passiert "still".
- Bei Fehlern in Core (z.B. `ReplaceAllAsync` wirft SQL-Exception): nur der Top-Level-
  `catch (Exception ex)` in `Program.cs` loggt. Kein lokaler Kontext.

Die `01-code-style.mdc` Zeile 19 sagt: "Kein Validierungs-/Fehlerbehandlungs-Ballast für
Fälle, die nicht eintreten können." Aber: Logging ist *kein* Ballast, sondern Sichtbarkeit.
Die Regel erlaubt es trotzdem — Ballast wäre z.B. "alle 5 Sekunden prüfen, ob der Pfad
existiert". Logging ist Standard.

**Fix-Empfehlung:**
`Microsoft.Extensions.Logging.Abstractions` ist ein leichtgewichtiges NuGet
(nur ~30 KB), das nur Interfaces liefert, ohne konkrete Logger-Implementierung. Core
nimmt `ILogger<SqlDocumentsStore>`, `ILogger<ImportService>` etc. per Constructor.
`Microsoft.Extensions.Logging.Abstractions` ist bereits transient über
`Microsoft.Extensions.Hosting` in der Cli-Referenz — Core referenziert es explizit.

```csharp
public sealed class SqlDocumentsStore(
    string connectionString,
    string documentsTableName,
    ILogger<SqlDocumentsStore> logger)
{
    public async Task ReplaceAllAsync(...) {
        logger.LogInformation("ReplaceAll startet: {DocumentCount} Dokumente", documents.Count);
        // ...
    }
}
```

**Aufwand:** ~1 Stunde + Test-Update (Logger-Stub hinzufügen, wahrscheinlich via
`NullLogger<T>`).

---

### F-AR-003 — `LogResponseSize` in falscher Schicht

**Schweregrad:** Medium (siehe Dim 8 für Performance-Auswirkung; hier: Architektur)

**Beobachtung:**
`src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:43-44`:
```csharp
private void LogResponseSize<T>(string toolName, T response) =>
    logger.LogInformation("{ToolName} response: {ByteCount} bytes", toolName, JsonSerializer.SerializeToUtf8Bytes(response).Length);
```

Dieses Helper ist im **Cli-McpTools-Layer**, nicht im Core-Store. Es serialisiert die
Response zu JSON-Bytes *nur um die Länge zu messen* — und das nach dem MCP-SDK bereits
die Serialisierung für den Output macht. Doppelt serialisiert.

**Architektur-Smell:** Die Logik "wie groß ist die Response?" gehört zur MCP-Schicht
(genauer: zur Tool-Response-Beobachtung), nicht zum Store. ABER: die *konkrete* Messung
(Bytes vs. Chars, JSON-Serialisierung vs. Approximation) ist eine Implementierungs-Detail.
Sauber wäre:

```csharp
// In DocsMcpTools:
logger.LogInformation("{ToolName} response: {ItemCount} items", toolName, result.Count);
```

Oder bei `get_doc`:
```csharp
logger.LogInformation("{ToolName} response: content {ContentLength} chars", toolName, result?.Content?.Length ?? 0);
```

Das ist:
- Schneller (kein JSON-Serialize der gesamten Response)
- Präziser (Items-Anzahl statt Bytes ist für LLM-UX relevanter)
- Kein Doppel-Serialisieren

Querverweis zu Dim 8 (F-PE-001) für die Performance-Auswirkung.

**Aufwand:** ~15 Minuten.

---

### F-AR-004 — `SqlDocumentsStore` Thread-Safety undokumentiert

**Schweregrad:** Medium (Doku-Lücke, nicht-Bug; Risiko bei zukünftigen Refactorings)

**Beobachtung:**
`SqlDocumentsStore` ist `sealed`, hat nur private Felder (Connection-String + Table),
und alle Methoden erstellen ihre eigene `SqlConnection`. Daraus folgt: read-Methoden
(`ListChildrenAsync`, `SearchDocsAsync`, `GetDocAsync`, `GetAllAsync`) sind thread-safe
(Connection wird pro Aufruf geöffnet, kein Shared State).

`ReplaceAllAsync` ist **nicht** thread-safe — wenn zwei Imports parallel laufen, race
condition auf `DELETE FROM` und `INSERT`s. Aktuell nicht möglich (CLI ruft `import`
nicht parallel auf, Server ruft `ReplaceAllAsync` nicht auf), aber: ein zukünftiger
Refactor, der Import via MCP-Tool exposed (Backlog-Item in `docs/05`: "Schreib-Tools
via MCP"), würde das brechen.

`docs/04-Datenmodell-Validierung-Edgecases.md`, Edge-Case 4.3 erwähnt das implizit:
> "Ein parallel laufender MCP-Server sieht dadurch nie einen halb-geleerten Zustand"

Aber: kein expliziter Hinweis "ReplaceAllAsync darf nicht parallel aufgerufen werden"
im Code selbst.

**Fix-Empfehlung:**
1. Code-Kommentar in `ReplaceAllAsync`:
   ```csharp
   // Not thread-safe. Do not call concurrently with other instances or with
   // read methods that need snapshot consistency.
   ```
2. Optional: `SemaphoreSlim` als private Field, der `ReplaceAllAsync` serialisiert.
   Kostet ~5 Zeilen Code, gibt Garantie.
3. docs/04 oder docs/03: expliziter Hinweis, dass `import` Single-Process ist.

**Aufwand:** ~5 Minuten (nur Kommentar), ~20 Minuten (mit Semaphore).

---

### F-AR-005 — Keine zentrale `Constants`-Datei

**Schweregrad:** Medium (Vor-Bote, Regel sagt "erst ab 2. Fall", aber 1 Fall steht bevor)

**Beobachtung:**
Die `.mdc`-Regel `06-configuration.mdc` Zeile 17 sagt:
> "Sobald ein zweiter Fall zu `FrontMatterParser.delimiter` hinzukommt, wird sie unter
> `KnowHowToAI.Core/Constants.cs` (oder passender benannt) angelegt."

Aktuell: nur 1 Fall (`FrontMatterParser.delimiter` in Zeile 59).

**Aktuelle "nahezu"-Konstanten, die in `Constants.cs` gehören würden, sobald ein 2. Fall entsteht:**

| Wert | Datei | Warum noch kein Refactor |
| --- | --- | --- |
| `"---"` (YAML-Delimiter) | `FrontMatterParser.cs:59` | Nur 1 Stelle |
| `"%.md"` / `"%.markdown"` (Markdown-Extension-Check) | `DocsValidator.cs:72-73` | 1 Validator-Stelle |
| `"file://"` (Schema-Präfix) | `DocsValidator.cs:71` | 1 Validator-Stelle |
| `"%COMPUTERNAME%"` (Env-Var-Literal) | `Program.cs:167-168` | 1 Stelle im Loader |

**Empfehlung:** Beobachten und bei 2. Fall handeln. Aktuell nicht zwingend.

---

### F-AR-006 — `Microsoft.Extensions.Logging` nicht in Core (Low)

**Schweregrad:** Low (Folge von F-AR-002)

**Beobachtung:** Core referenziert `Microsoft.Extensions.Logging.Abstractions` *nicht*
explizit, obwohl die Cli-Schicht `Microsoft.Extensions.Logging` (transient) referenziert.
F-AR-002 löst das mit.

---

### F-AR-007 — Service-Lifetimes undokumentiert

**Schweregrad:** Low (Doku-Lücke, kein Bug)

**Beobachtung:**
- `SqlDocumentsStore` ist Singleton (`RunServer`-Pfad, `AddSingleton`).
- Andere Services sind implizit Transient (per `new` in `RunImport`/`RunExport`).
- Doku (`docs/03`) zeigt *wie* der Server verdrahtet wird, aber nicht *warum* diese
  Lifetime gewählt wurde.

**Fix-Empfehlung:** Ein-Satz-Erklärung in `docs/03` zu Singleton-Lifetime
("SqlDocumentsStore ist zustandslos auf Instance-Ebene und teilt sich den DB-Connection-
Pool, daher Singleton.").

**Aufwand:** ~5 Minuten.

---

### F-AR-008 / F-AR-009 / F-AR-010 — Info-Findings

Konformität mit `.mdc`-Regeln und idiomatische Patterns. Kein Handlungsbedarf.

---

## Zusammenfassung Dim 3

- **10 Findings**, davon 2 × High, 3 × Medium, 2 × Low, 3 × Info.
- **Zwei High-Findings hängen zusammen:** F-AR-001 (DI-Inkonsistenz) und F-AR-002
  (kein `ILogger` in Core). Beide lassen sich mit einem ~2-Stunden-Refactor in einem
  Rutsch lösen: Composition-Root-Pattern einführen + Core-Services `ILogger<T>`
  akzeptieren lassen.
- **Architektur-Grundgerüst ist solide:** Layering Core/Cli ist sauber, Delegate-Pattern
  für DB-Isolation ist clever, Tests sind gut isoliert. Die zwei High-Findings sind
  Reibungsverluste, keine Architektur-Krisen.
