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
| [F-AR-009](#f-ar-009) | Info | `partial class DocsValidator` mit `GeneratedRegex` — idiomatisch, AiNetLinter-konform | `Validation/DocsValidator.cs:8` |
| [F-AR-010](#f-ar-010) | Info | Error-Handling ist konsistent: alle vier CLI-Commands fangen `Exception` an der Top-Level und liefern Exit-Code 2 | `Cli/Program.cs:67, 89, 112, 140` |

## Detail-Findings

---
## Zusammenfassung Dim 3

- **0 Findings** (Dim 3 sauber nach Prio J-Extraktion).
- **F-AR-001** (DI-Inkonsistenz, in PrioC) und **F-AR-002** (kein `ILogger` in Core, in PrioA) sind beide extrahiert. Sie lassen sich mit einem ~2-Stunden-Refactor in einem Rutsch lösen, sobald beide Prio-Ordner umgesetzt sind.
- **Architektur-Grundgerüst ist solide:** Layering Core/Cli ist sauber, Delegate-Pattern
  für DB-Isolation ist clever, Tests sind gut isoliert. Die zwei High-Findings sind
  Reibungsverluste, keine Architektur-Krisen.
