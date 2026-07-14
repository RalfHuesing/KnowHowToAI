# Projektstruktur & Konfiguration

## 1. Solution-Layout

Root-Namespace/Solution-Name orientiert sich am Repo-Namen: **`KnowHowToAI`**.

```
KnowHowToAI/
├── KnowHowToAI.slnx    # .NET-10-natives XML-Solution-Format
├── docs/               # Dieses Konzept (kein Projektinhalt)
├── demo-docs/          # Kleine Beispiel-Bibliothek für manuelle End-to-End-Tests
├── sql-scripts/        # Nummerierte DbUp-Skripte, siehe 04 (werden in Core embedded)
├── scripts/            # Hilfsskripte, z.B. publish.ps1 (siehe Abschnitt 5)
├── publish/            # Ausgabe von scripts/publish.ps1 (gitignored, nicht committen)
├── src/
│   ├── KnowHowToAI.Core/   # Domain-Logik, kein IO-Framework — siehe Tabelle unten
│   └── KnowHowToAI.Cli/    # Entry Point: CLI-Wiring + MCP-Hosting — siehe Tabelle unten
└── tests/
    └── KnowHowToAI.Core.Tests/
        └── AiNetLinter/    # Config/Docs/Output für den Linter-Test, siehe Abschnitt 4
```

> **Diese Seite ist ein Schema, keine vollständige Dateiliste.** Neue Klassen kommen in den Ordner, der zu ihrer Verantwortung passt (ein Typ pro Datei, Dateiname = Typname) — das wird hier nicht bei jeder neuen Datei nachgetragen. Siehe [05-documentation.mdc](../.agents/rules/05-documentation.mdc): Projektstruktur-Doku beschreibt Konventionen, keine Inhaltsverzeichnisse.

**`KnowHowToAI.Core`** — enthält die gesamte Logik ohne IO-Framework-Abhängigkeiten (kein `System.CommandLine`, kein MCP-SDK) → einfach und schnell testbar mit xUnit v3.

| Ordner | Zuständigkeit |
| --- | --- |
| `Documents/` | Domain-Objekte und alles rund ums Parsen/Rendern eines Dokuments (`Document`, `DocumentSummary`, `DocumentDetail`, `FrontMatterParser`, `SlugRules`) |
| `Validation/` | `DocsValidator`, `ValidationResult` |
| `Migrations/` | `SchemaMigrator` (DbUp gegen embedded `sql-scripts/*.sql`) |
| `Sync/` | `ImportService`, `ExportService` (SQL-Zugriff als Delegate von außen, siehe unten), `SqlDocumentsStore` (einziger Ort mit echtem `SqlConnection`/Dapper-Zugriff) |
| `Configuration/` | `KnowHowToAiOptions` |

**`KnowHowToAI.Cli`** — der einzige Ort, der CLI-Parsing und MCP-Hosting kennt. Bleibt dünn (nur Wiring).

| Ordner | Zuständigkeit |
| --- | --- |
| *(Root)* | `Program.cs` — `System.CommandLine`-Wiring für `validate`/`import`/`export`/`server`, `appsettings.json` |
| `Logging/` | Adapter zwischen Drittanbieter-Logging-Interfaces (z.B. DbUps `IUpgradeLog`) und Serilog |
| `McpTools/` | `[McpServerToolType]`-Klassen |

**`KnowHowToAI.Core.Tests`** — testet ausschließlich `Core`, ein Testfile pro getesteter Klasse, gleicher Namensmuster (`{Klasse}Tests.cs`). Keine Integrationstests gegen einen echten SQL Server in v1 (siehe [05-Roadmap.md](05-Roadmap.md)). Zusätzlich `AiNetLinterTests.cs` (siehe Abschnitt 4).

**Wie `ImportService`/`ExportService` ohne SQL Server testbar bleiben:** Beide nehmen den SQL-Zugriff als **Delegate** entgegen (`Func<IReadOnlyList<Document>, CancellationToken, Task>` bzw. `Func<CancellationToken, Task<IReadOnlyList<Document>>>`), nicht als Interface. Ein `IDocumentsRepository`-Interface für eine einzige Implementierung widerspräche [01-code-style.mdc](../.agents/rules/01-code-style.mdc) (das genau dieses Beispiel als verbotene Interface-Wüste nennt); ein Delegate erreicht dieselbe Testbarkeit ohne die zusätzliche Abstraktionsebene — deckt sich mit der in [02-testing.mdc](../.agents/rules/02-testing.mdc) explizit genannten Option "Interface **oder** Delegate". `SqlDocumentsStore` ist die einzige Klasse mit echtem `SqlConnection`/Dapper-Zugriff und wird selbst nicht separat unit-getestet (dünner DB-Adapter, analog zu `SchemaMigrator.Migrate`).

**Schema-Migration ist kein Teil von `ImportService` mehr.** `SchemaMigrator.Migrate(...)` läuft in der Cli-Schicht (Schritt 5) **vor** dem Aufruf von `ImportService.ImportAsync(...)`, nicht innerhalb davon — Migration erfordert zwingend eine echte DB-Verbindung und würde sonst die Testbarkeit von `ImportService` wieder zunichtemachen. Für den Endnutzer ändert sich am dokumentierten Ablauf aus [01-Konzept-und-Workflow.md](01-Konzept-und-Workflow.md#phase-4-synchronisation-wipe-and-dump) nichts: `KnowHowToAI.Cli import` führt weiterhin beide Schritte in der beschriebenen Reihenfolge aus, nur eben als zwei Aufrufe innerhalb desselben Kommandos statt als ein Aufruf.

---

## 2. Konfiguration: `appsettings.json`

**Ein Konfigurationsort pro Einsatzort/Projekt.** Enthält Docs-Root-Pfad *und* Connection-String gemeinsam. Liegt entweder neben der `.exe` (Default) oder wird explizit per `--config <path>` referenziert — so kann dieselbe gebaute `.exe` für mehrere unabhängige Projekte/Docs-Bibliotheken verwendet werden, indem man pro Projekt eine eigene Config-Datei anlegt und in der jeweiligen MCP-Launch-Config referenziert.

`src/KnowHowToAI.Cli/appsettings.json` ist die tatsächlich genutzte, **committete** Konfiguration für dieses lokale Dev-/Demo-Setup (bewusste Abweichung von der ursprünglichen "nie committen"-Regel — siehe unten):

```json
{
  "KnowHowToAi": {
    "DocsRootPath": "C:\\Daten\\Entwicklung\\Ralf\\KnowHowToAI\\demo-docs",
    "ConnectionString": "Server=%COMPUTERNAME%\\MSSQLSERVER2022;Database=DemoDB;User Id=Agent;Password=Agent!;TrustServerCertificate=True;",
    "ExportMarkerFileName": ".knowhowtoai-export-marker.json",
    "Logging": {
      "MinimumLevel": "Information",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 14
    }
  }
}
```

* **`Logging`** (`KnowHowToAiLoggingOptions`): steuert die Serilog-Datei-Rotation — `MinimumLevel` (`Serilog.Events.LogEventLevel`-Name), `RollingInterval` (`Serilog.RollingInterval`-Name, z.B. `Day`), `RetainedFileCountLimit` (Anzahl aufbewahrter Dateien). Bewusst konfigurierbar statt hartcodiert, siehe [06-configuration.mdc](../.agents/rules/06-configuration.mdc). `Program.ConfigureLogger` baut damit den Serilog-Logger direkt nach `LoadOptions` neu auf (ein kurzlebiger Bootstrap-Logger mit denselben Defaults deckt die Zeitspanne davor ab, z.B. falls `LoadOptions` selbst fehlschlägt).

* **Override per Umgebungsvariable:** `Microsoft.Extensions.Configuration` erlaubt `KnowHowToAi__ConnectionString` bzw. `KnowHowToAi__DocsRootPath` als Override, ohne die Datei anzufassen (z.B. für CI oder abweichende Rechner).
* **`%COMPUTERNAME%`-Platzhalter:** `Program.LoadOptions` ersetzt den *literalen* Text `%COMPUTERNAME%` in der Connection-String durch `Environment.MachineName` — bewusst **nicht** `Environment.ExpandEnvironmentVariables(...)`. Letzteres würde die Umgebungsvariable `COMPUTERNAME` aus dem Prozess-Environment lesen, die fehlen kann, wenn der MCP-Server von Cursor/Claude Desktop mit einem reduzierten Environment gestartet wird. `Environment.MachineName` fragt den Rechnernamen direkt beim Betriebssystem ab und ist davon unabhängig. Damit funktioniert dieselbe committete `appsettings.json` unverändert auf jedem Rechner, auf dem eine SQL-Server-Instanz mit demselben Instanznamen und denselben Zugangsdaten existiert.
* **Kein generisches Secret-Handling in v1, bewusste Ausnahme für dieses lokale Setup:** Grundsätzlich gilt weiterhin, dass produktive/sensible Connection-Strings nicht ins Repo gehören. Für dieses konkrete lokale Dev-/Demo-Setup (SQL-Login `Agent` auf einer lokalen Instanz, keine echten Geheimnisse) hat der Projektverantwortliche das Committen explizit freigegeben — `appsettings.json` ist daher **nicht** mehr in `.gitignore`, es gibt keine separate `appsettings.example.json` mehr. Bei einem späteren produktiven Einsatz mit echten Secrets ist diese Ausnahme erneut zu bewerten.
* **SQL-Server-Instanzname ≠ Datenbankname:** Der Instanzname (`MSSQLSERVER2022`) muss zu einer tatsächlich registrierten SQL-Server-Instanz auf dem Zielrechner passen (`Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'` zeigt die installierten Instanzen). Er ist unabhängig vom Datenbanknamen (`Database=DemoDB`).
* **`Logs/`-Ordner neben der `.exe`:** Serilog legt beim Start jedes Kommandos automatisch `Logs/` relativ zu `AppContext.BaseDirectory` an (Ordnername/-Präfix sind Konvention, nicht konfigurierbar; Rotation dagegen schon, siehe `Logging` oben und [02](02-Architektur-und-Techstack.md#2-tech-stack--dependencies)). Gitignored über die generische `[Ll]ogs/`-Regel.
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

---

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

## 5. Deployment (Single-File-Publish)

```powershell
scripts\publish.ps1
```

Erzeugt via `dotnet publish` eine **self-contained Single-File-.exe** unter `publish\KnowHowToAI.Cli.exe` (Parameter `-Runtime`, `-Configuration`, `-OutputDir` überschreibbar, Default: `win-x64` / `Release` / `publish`).

* **Self-contained statt framework-dependent:** Die .NET-Runtime ist eingebettet (~85 MB statt ~1-5 MB). Bewusst gewählt, damit die `.exe` unabhängig davon läuft, ob Cursor/Claude Desktop den MCP-Server-Prozess mit einer .NET-Runtime im `PATH` startet — derselbe Grund wie bei der `%COMPUTERNAME%`-Auflösung (Abschnitt 2): der MCP-Host-Prozess-Kontext ist nicht garantiert identisch mit einer normalen interaktiven Shell.
* `-p:IncludeNativeLibrariesForSelfExtract=true` bündelt auch native Abhängigkeiten in die eine `.exe`, statt sie beim ersten Start in einen Temp-Ordner zu extrahieren.
* `appsettings.json` liegt nach dem Publish weiterhin als separate Datei neben der `.exe` (bewusst nicht in die Single-File eingebettet — muss editierbar bleiben, siehe Abschnitt 2).
* `publish/` ist gitignored; jeder baut sich die `.exe` lokal selbst, es wird kein Build-Artefakt versioniert.
