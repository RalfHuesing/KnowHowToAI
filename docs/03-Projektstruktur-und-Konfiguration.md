# Projektstruktur & Konfiguration

## 1. Solution-Layout

Root-Namespace/Solution-Name orientiert sich am Repo-Namen: **`KnowHowToAI`**.

```
KnowHowToAI/
├── KnowHowToAI.slnx                   # .NET-10-natives XML-Solution-Format
├── docs/                              # Dieses Konzept (kein Projektinhalt)
├── demo-docs/                         # Kleine Beispiel-Bibliothek für manuelle End-to-End-Tests
│   ├── it.md
│   └── it/netzwerk.md, it/netzwerk/routing.md
├── sql-scripts/                       # Nummerierte DbUp-Skripte, siehe 04 (werden in Core embedded)
│   ├── 0001_create_documents_table.sql
│   └── 0002_create_fulltext_catalog_and_index.sql
├── src/
│   ├── KnowHowToAI.Core/              # Domain, Parser, Validator, Import/Export-Logik
│   │   ├── KnowHowToAI.Core.csproj
│   │   ├── Documents/
│   │   │   ├── Document.cs            # Domain-Objekt (Slug, Title, Content, Tags, Synonyms, ParentSlug)
│   │   │   ├── DocumentSummary.cs     # Slug + Title, Rückgabe von list_children/search_docs
│   │   │   ├── DocumentDetail.cs      # Title + Content, Rückgabe von get_doc
│   │   │   ├── FrontMatterParser.cs   # YAML-Front-Matter <-> Document (Parse + Render)
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
│   │   ├── Logging/
│   │   │   └── SerilogUpgradeLog.cs   # Bindet DbUps IUpgradeLog an Serilog (Console.Error)
│   │   ├── McpTools/
│   │   │   └── DocsMcpTools.cs        # [McpServerTool] list_children/search_docs/get_doc
│   │   └── appsettings.json           # Voll funktionsfähige, committete Konfiguration (siehe Abschnitt 2)
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

`src/KnowHowToAI.Cli/appsettings.json` ist die tatsächlich genutzte, **committete** Konfiguration für dieses lokale Dev-/Demo-Setup (bewusste Abweichung von der ursprünglichen "nie committen"-Regel — siehe unten):

```json
{
  "KnowHowToAi": {
    "DocsRootPath": "C:\\Daten\\Entwicklung\\Ralf\\KnowHowToAI\\demo-docs",
    "ConnectionString": "Server=%COMPUTERNAME%\\MSSQLSERVER2022;Database=DemoDB;User Id=Agent;Password=Agent!;TrustServerCertificate=True;",
    "ExportMarkerFileName": ".knowhowtoai-export-marker.json"
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

* **Override per Umgebungsvariable:** `Microsoft.Extensions.Configuration` erlaubt `KnowHowToAi__ConnectionString` bzw. `KnowHowToAi__DocsRootPath` als Override, ohne die Datei anzufassen (z.B. für CI oder abweichende Rechner).
* **`%COMPUTERNAME%`-Platzhalter:** `Program.LoadOptions` ersetzt den *literalen* Text `%COMPUTERNAME%` in der Connection-String durch `Environment.MachineName` — bewusst **nicht** `Environment.ExpandEnvironmentVariables(...)`. Letzteres würde die Umgebungsvariable `COMPUTERNAME` aus dem Prozess-Environment lesen, die fehlen kann, wenn der MCP-Server von Cursor/Claude Desktop mit einem reduzierten Environment gestartet wird. `Environment.MachineName` fragt den Rechnernamen direkt beim Betriebssystem ab und ist davon unabhängig. Damit funktioniert dieselbe committete `appsettings.json` unverändert auf jedem Rechner, auf dem eine SQL-Server-Instanz mit demselben Instanznamen und denselben Zugangsdaten existiert.
* **Kein generisches Secret-Handling in v1, bewusste Ausnahme für dieses lokale Setup:** Grundsätzlich gilt weiterhin, dass produktive/sensible Connection-Strings nicht ins Repo gehören. Für dieses konkrete lokale Dev-/Demo-Setup (SQL-Login `Agent` auf einer lokalen Instanz, keine echten Geheimnisse) hat der Projektverantwortliche das Committen explizit freigegeben — `appsettings.json` ist daher **nicht** mehr in `.gitignore`, es gibt keine separate `appsettings.example.json` mehr. Bei einem späteren produktiven Einsatz mit echten Secrets ist diese Ausnahme erneut zu bewerten.
* **SQL-Server-Instanzname ≠ Datenbankname:** Der Instanzname (`MSSQLSERVER2022`) muss zu einer tatsächlich registrierten SQL-Server-Instanz auf dem Zielrechner passen (`Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'` zeigt die installierten Instanzen). Er ist unabhängig vom Datenbanknamen (`Database=DemoDB`).
* **Bekannter lokaler Stolperstein (offen, noch nicht durch einen erfolgreichen Lauf verifiziert):** Bei der Implementierung von Schritt 6 zeigte sich per `sqlcmd`, dass die SQL-Server-Instanz auf dem Entwicklungsrechner ihren TCP-Listener nur an `127.0.0.1`/`::1` bindet (nicht an die per Hostname erreichbare Netzwerkadresse) und der Login `Agent` selbst über die Loopback-Adresse mit einer Anmeldefehler-Meldung abgelehnt wurde. Vor dem ersten produktiven `import`/`server`-Lauf prüfen: TCP/IP-Bindung in der SQL Server Configuration Manager (ggf. auf alle Interfaces erweitern) und den Login `Agent` (Passwort, Server-Rolle, ob Server im gemischten Authentifizierungsmodus läuft) separat verifizieren, z.B. per SSMS.

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
