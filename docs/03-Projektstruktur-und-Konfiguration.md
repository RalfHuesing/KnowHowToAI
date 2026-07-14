# Projektstruktur & Konfiguration

## 1. Solution-Layout

Root-Namespace/Solution-Name orientiert sich am Repo-Namen: **`KnowHowToAI`**.

```
KnowHowToAI/
├── KnowHowToAI.slnx                   # .NET-10-natives XML-Solution-Format
├── docs/                              # Dieses Konzept (kein Projektinhalt)
├── sql-scripts/                       # Nummerierte DbUp-Skripte, siehe 04 (werden in Core embedded)
│   ├── 0001_create_documents_table.sql
│   └── 0002_create_fulltext_catalog_and_index.sql
├── src/
│   ├── KnowHowToAI.Core/              # Domain, Parser, Validator, Import/Export-Logik
│   │   ├── KnowHowToAI.Core.csproj
│   │   ├── Documents/
│   │   │   ├── Document.cs            # Domain-Objekt (Slug, Title, Content, Tags, Synonyms, ParentSlug)
│   │   │   ├── FrontMatterParser.cs   # YAML-Front-Matter -> Document
│   │   │   └── SlugRules.cs           # Regex + Validierung für Slugs
│   │   ├── Validation/
│   │   │   ├── DocsValidator.cs       # Orphan-Check, Slug-Check, YAML-Check
│   │   │   └── ValidationResult.cs
│   │   ├── Migrations/
│   │   │   └── SchemaMigrator.cs      # DbUp gegen embedded sql-scripts/*.sql, IUpgradeLog-Parameter
│   │   ├── Sync/
│   │   │   ├── ImportService.cs       # Validate + Wipe-and-Dump; SQL-Zugriff kommt als Delegate von außen
│   │   │   ├── ExportService.cs       # Marker-Datei-Logik, MD-Generierung; SQL-Zugriff ebenfalls als Delegate
│   │   │   └── SqlDocumentsStore.cs   # Einziger Ort mit echtem SqlConnection/Dapper-Zugriff (Wipe-and-Dump + Read)
│   │   └── Configuration/
│   │       └── KnowHowToAiOptions.cs  # DocsRootPath, ConnectionString, ExportMarkerFileName
│   ├── KnowHowToAI.Cli/               # Entry Point
│   │   ├── KnowHowToAI.Cli.csproj
│   │   ├── Program.cs                 # System.CommandLine-Wiring (validate/import/export/server)
│   │   ├── McpTools/
│   │   │   └── DocsMcpTools.cs        # [McpServerTool] list_children/search_docs/get_doc
│   │   └── appsettings.example.json   # Beispiel-Konfiguration, echte appsettings.json nie committen
│   └── ... (weitere Projekte nur bei Bedarf, siehe 05-Roadmap Backlog)
└── tests/
    └── KnowHowToAI.Core.Tests/
        ├── KnowHowToAI.Core.Tests.csproj
        ├── FrontMatterParserTests.cs
        ├── SlugRulesTests.cs
        ├── SchemaMigratorTests.cs
        ├── DocsValidatorTests.cs
        ├── ImportExportServiceTests.cs
        ├── AiNetLinterTests.cs         # führt AiNetLinter als externen Prozess aus, siehe Abschnitt 4
        └── AiNetLinter/
            ├── docs/                   # versionierte Tool-Doku (readme/agent-api/configuration)
            ├── rules/KnowHowToAI.rules.json
            └── output/                 # Lint-Reports, gitignored
```

**Warum genau diese 3 Projekte?**
- `KnowHowToAI.Core` enthält die gesamte Logik ohne IO-Framework-Abhängigkeiten (kein `System.CommandLine`, kein MCP-SDK) → einfach und schnell testbar mit xUnit v3.
- `KnowHowToAI.Cli` ist der einzige Ort, der CLI-Parsing und MCP-Hosting kennt. Bleibt dünn (nur Wiring).
- `KnowHowToAI.Core.Tests` testet ausschließlich `Core` — keine Integrationstests gegen einen echten SQL Server in v1 (siehe [05-Roadmap.md](05-Roadmap.md)).

**Wie `ImportService`/`ExportService` ohne SQL Server testbar bleiben:** Beide nehmen den SQL-Zugriff als **Delegate** entgegen (`Func<IReadOnlyList<Document>, CancellationToken, Task>` bzw. `Func<CancellationToken, Task<IReadOnlyList<Document>>>`), nicht als Interface. Ein `IDocumentsRepository`-Interface für eine einzige Implementierung widerspräche [01-code-style.mdc](../.agents/rules/01-code-style.mdc) (das genau dieses Beispiel als verbotene Interface-Wüste nennt); ein Delegate erreicht dieselbe Testbarkeit ohne die zusätzliche Abstraktionsebene — deckt sich mit der in [02-testing.mdc](../.agents/rules/02-testing.mdc) explizit genannten Option "Interface **oder** Delegate". `SqlDocumentsStore` ist die einzige Klasse mit echtem `SqlConnection`/Dapper-Zugriff und wird selbst nicht separat unit-getestet (dünner DB-Adapter, analog zu `SchemaMigrator.Migrate`).

**Schema-Migration ist kein Teil von `ImportService` mehr.** `SchemaMigrator.Migrate(...)` läuft in der Cli-Schicht (Schritt 5) **vor** dem Aufruf von `ImportService.ImportAsync(...)`, nicht innerhalb davon — Migration erfordert zwingend eine echte DB-Verbindung und würde sonst die Testbarkeit von `ImportService` wieder zunichtemachen. Für den Endnutzer ändert sich am dokumentierten Ablauf aus [01-Konzept-und-Workflow.md](01-Konzept-und-Workflow.md#phase-4-synchronisation-wipe-and-dump) nichts: `KnowHowToAI.Cli import` führt weiterhin beide Schritte in der beschriebenen Reihenfolge aus, nur eben als zwei Aufrufe innerhalb desselben Kommandos statt als ein Aufruf.

## 4. AiNetLinter (Code-Qualitäts-Gate)

Zusätzlich zu Build und Unittests läuft [`AiNetLinter`](https://github.com/RalfHuesing/AiNetLinter) als Roslyn-basierter Linter gegen KI-taugliche Codestruktur (Komplexität, Sealed Classes, Phantom-Dependencies, Namespace-Pfad-Abgleich). Integriert als einzelner Test (`AiNetLinterTests.LintRun_ReportsNoViolations`) in `KnowHowToAI.Core.Tests` — kein eigenes Projekt.

* **Tool-Standort:** extern, außerhalb des Repos (z. B. `C:\Daten\AiNetLinter-win-x64\AiNetLinter.exe`), Pfad überschreibbar per Umgebungsvariable `AINETLINTER_EXE`. Ist die `.exe` nicht vorhanden, wird der Test übersprungen (kein CI-Hard-Requirement) — das Tool ist ein optionales lokales Entwickler-Werkzeug.
* **Konfiguration:** `tests/KnowHowToAI.Core.Tests/AiNetLinter/rules/KnowHowToAI.rules.json`, abgeleitet von den Tool-Defaults mit drei projektspezifischen Anpassungen:
  * `Web.IsEnabled: false` — kein CSS/JS/Razor in diesem Projekt (reine Console-/MCP-Anwendung).
  * `RuleMetadata.StaticTestSentinel.Severity: "error"` — macht die ohnehin geltende Testpflicht aus [02-testing.mdc](../.agents/rules/02-testing.mdc) zum automatisierten, build-blockierenden Gate für alles in `KnowHowToAI.Core`.
  * `ProjectOverrides."*.Cli".Global.EnableTestSentinel: false` — spiegelt die in [02-testing.mdc](../.agents/rules/02-testing.mdc) dokumentierte Ausnahme (reines Cli-Wiring braucht keine eigenen Tests).
* **Kein Baseline-File in v1:** Das Projekt ist aktuell verstoßfrei (verifiziert per Erstlauf), es gibt nichts einzufrieren. Eine `--baseline`-Datei wird nachgezogen, sobald ein erster dokumentierter Altlast-Verstoß bewusst in Kauf genommen wird.
* **Regel-Sync:** Der Test ruft `--sync-cursor-rules --cursor-rules-path .agents/rules` mit auf (ab AiNetLinter 1.0.75) und hält `.agents/rules/AiNetLinter.mdc` (generierte Grenzwert-Übersicht) automatisch aktuell — bewusst dort statt in `.cursor/rules/`, da `.agents/rules/` die eine Quelle der Wahrheit für Agenten-Regeln ist (siehe [00-Overview.md](00-Overview.md), Grundsatzentscheidung 1 in [04-docs-reference.mdc](../.agents/rules/04-docs-reference.mdc)). Cursor liest die Datei trotzdem, da `.cursor/rules/00-see-agents-rules.mdc` dorthin verweist. Diese Datei ist automatisch generiert — nicht manuell bearbeiten, Anpassungen gehören in `KnowHowToAI.rules.json`.
* Tool-Dokumentation ist unter `tests/KnowHowToAI.Core.Tests/AiNetLinter/docs/*.md` versioniert (Stand des Tools zum Zeitpunkt der Integration, ohne Netzzugriff nutzbar).

---

## 2. Konfiguration: `appsettings.json`

**Ein Konfigurationsort pro Einsatzort/Projekt.** Enthält Docs-Root-Pfad *und* Connection-String gemeinsam. Liegt entweder neben der `.exe` (Default) oder wird explizit per `--config <path>` referenziert — so kann dieselbe gebaute `.exe` für mehrere unabhängige Projekte/Docs-Bibliotheken verwendet werden, indem man pro Projekt eine eigene Config-Datei anlegt und in der jeweiligen MCP-Launch-Config referenziert.

```json
{
  "KnowHowToAi": {
    "DocsRootPath": "C:\\Projekte\\MeinProjekt\\wissensdatenbank",
    "ConnectionString": "Server=localhost;Database=KnowHowToAiDocs;Trusted_Connection=True;TrustServerCertificate=True;",
    "ExportMarkerFileName": ".knowhowtoai-export-marker.json"
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

* **Override per Umgebungsvariable:** `Microsoft.Extensions.Configuration` erlaubt `KnowHowToAi__ConnectionString` bzw. `KnowHowToAi__DocsRootPath` als Override, ohne die Datei anzufassen (z.B. für CI oder abweichende Rechner).
* **Kein Secret-Handling in v1:** Es wird davon ausgegangen, dass lokale/Netzwerk-SQL-Server mit Windows-Auth (`Trusted_Connection`) oder unkritischen Zugangsdaten genutzt werden. Falls SQL-Auth mit Passwort nötig wird, gehört der Connection-String **nicht** ins Git-Repo (`appsettings.json` mit echten Zugangsdaten in `.gitignore` aufnehmen, nur eine `appsettings.example.json` committen).

### MCP-Launch-Konfiguration (Beispiel für Claude Desktop/Cursor)

```json
{
  "mcpServers": {
    "knowhowtoai-mein-projekt": {
      "command": "C:\\Pfad\\zu\\KnowHowToAI.Cli.exe",
      "args": ["server", "--config", "C:\\Projekte\\MeinProjekt\\knowhowtoai.appsettings.json"]
    }
  }
}
```

Mehrere Projekte = mehrere Einträge mit unterschiedlichen `--config`-Pfaden, alle gegen dieselbe `.exe`.

---

## 3. CLI-Kommandos (Übersicht)

| Kommando | Optionen | Zweck |
| --- | --- | --- |
| `validate` | `--config <path>` | Prüft Docs-Root, kein DB-Zugriff nötig |
| `import` | `--config <path>` | DbUp-Migration + validate + Wipe-and-Dump in SQL Server |
| `export` | `--config <path>` `--target <dir>` | Schreibt DB-Inhalt als `.md`-Dateien (Marker-geschützt) |
| `server` | `--config <path>` | Startet MCP-stdio-Server (Default-Modus ohne Argument ebenfalls denkbar) |

`--config` ist optional, wenn eine `appsettings.json` direkt neben der `.exe` liegt (Default-Konvention von `Microsoft.Extensions.Configuration`).
