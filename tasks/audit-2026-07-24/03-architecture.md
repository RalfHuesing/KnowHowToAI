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
| [F-AR-002](#f-ar-002) | **High** | `ImportService`/`ExportService` haben **keine** `ILogger`-Injection — `SqlDocumentsStore` und `DocsValidator` ebenfalls nicht | `Sync/ImportService.cs:9`, `Sync/ExportService.cs:8`, `Sync/SqlDocumentsStore.cs:11`, `Validation/DocsValidator.cs:8` |
| [F-AR-005](#f-ar-005) | Medium | Keine zentrale `Constants`-Datei — `FrontMatterParser.delimiter` als Vor-Bote | `Documents/FrontMatterParser.cs:59` |
| [F-AR-006](#f-ar-006) | Low | `Microsoft.Extensions.Logging`-Abhängigkeit nur in `DocsMcpTools`, nicht in Core-Services — Core ist "blind" für strukturiertes Logging | `McpTools/DocsMcpTools.cs:14` |
| [F-AR-008](#f-ar-008) | Info | `sealed` als Class-Lock konsistent angewendet — Cross-Cutting-Stil diszipliniert | (mehrere) |
| [F-AR-009](#f-ar-009) | Info | `partial class DocsValidator` mit `GeneratedRegex` — idiomatisch, AiNetLinter-konform | `Validation/DocsValidator.cs:8` |
| [F-AR-010](#f-ar-010) | Info | Error-Handling ist konsistent: alle vier CLI-Commands fangen `Exception` an der Top-Level und liefern Exit-Code 2 | `Cli/Program.cs:67, 89, 112, 140` |

## Detail-Findings

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

### F-AR-008 / F-AR-009 / F-AR-010 — Info-Findings

Konformität mit `.mdc`-Regeln und idiomatische Patterns. Kein Handlungsbedarf.

---

## Zusammenfassung Dim 3

- **6 Findings** (nach Prio C-Extraktion), davon 1 × High (PrioA), 1 × Medium, 1 × Low, 3 × Info.
- **F-AR-001** (DI-Inkonsistenz, in PrioC) und **F-AR-002** (kein `ILogger` in Core, in PrioA) sind beide extrahiert. Sie lassen sich mit einem ~2-Stunden-Refactor in einem Rutsch lösen, sobald beide Prio-Ordner umgesetzt sind.
- **Architektur-Grundgerüst ist solide:** Layering Core/Cli ist sauber, Delegate-Pattern
  für DB-Isolation ist clever, Tests sind gut isoliert. Die zwei High-Findings sind
  Reibungsverluste, keine Architektur-Krisen.
