# Roadmap

## v1 (MVP) — Kern-Loop zuerst

Ziel: Der komplette Doku-Loop (`validate` → `import` → `server`, plus `export`) funktioniert Ende-zu-Ende gegen einen echten MS SQL Server, mit den drei MCP-Tools nutzbar in Cursor/Claude Desktop/Claude Code.

### Implementierungs-Reihenfolge (für den frischen Umsetzungs-Chat)

Jeder Schritt ist ein eigener Commit (siehe [03-git-workflow.mdc](../.agents/rules/03-git-workflow.mdc)); Tests entstehen im selben Commit wie das Feature, nicht nachträglich.

- [x] **1. Solution & Projekt-Setup**
  - [x] `KnowHowToAI.slnx` mit `src/KnowHowToAI.Core`, `src/KnowHowToAI.Cli`, `tests/KnowHowToAI.Core.Tests`
  - [x] NuGet-Pakete: `Dapper`, `Microsoft.Data.SqlClient`, `dbup-sqlserver`, `System.CommandLine`, `ModelContextProtocol`, `Serilog` (+ `Serilog.Sinks.Console`), `YamlDotNet`
  - [x] xUnit-v3-Testprojekt verdrahtet

- [x] **2. SQL-Schema & DbUp-Integration**
  - [x] `sql-scripts/0001_create_documents_table.sql` (siehe [04, Abschnitt 1](04-Datenmodell-Validierung-Edgecases.md#1-sql-skripte-sql-scripts-dbup-verwaltet))
  - [x] `SchemaMigrator` (Core/Migrations) führt sie via DbUp aus, Skripte als Embedded Resource, Logging über `IUpgradeLog`-Parameter statt harter Serilog-Abhängigkeit
  - [x] Tests: Skript-Discovery ohne echten SQL Server

- [x] **3. Domain-Model & Front-Matter-Parser/Validator**
  - [x] `Document`, `SlugRules` (Regex + Orphan-Hilfsfunktionen)
  - [x] `FrontMatterParser`: YAML ↔ Markdown, inkl. `Render` für den Export (Rundtrip-fähig)
  - [x] `DocsValidator`: Slug-Check, YAML-Check, mehrstufiger Orphan-Check — sammelt alle Fehler in einem Durchlauf (siehe [04, Abschnitt 3](04-Datenmodell-Validierung-Edgecases.md#3-validierungsregeln-validate))
  - [x] Tests: valide/invalide Front-Matter, Slug-Regeln, Orphan-Szenarien, leeres Docs-Root

- [x] **4. Import/Export-Engine**
  - [x] `ImportService`: Validate-Gate + Wipe-and-Dump, SQL-Zugriff als Delegate (nicht Interface, siehe [03, Abschnitt 1](03-Projektstruktur-und-Konfiguration.md#1-solution-layout))
  - [x] `ExportService`: Marker-Datei-Logik (siehe [04, Abschnitt 4.4](04-Datenmodell-Validierung-Edgecases.md#44-export-marker-datei)) + MD-Generierung
  - [x] `SqlDocumentsStore`: einziger Ort mit echtem SQL-Zugriff, Insert-Reihenfolge nach Slug-Tiefe (FK-sicher)
  - [x] Schema-Migration bewusst aus `ImportService` herausgezogen — läuft ab Schritt 5 vorgelagert in der Cli
  - [x] Tests: Validierungs-Gate, Happy Path, alle drei Marker-Datei-Szenarien, Front-Matter-Rundtrip

- [x] **5. CLI-Wiring**
  - [x] `KnowHowToAiOptions`-Bindung aus `appsettings.json` (+ Env-Var-Override) pro `--config`-Pfad, klare Fehlermeldung bei fehlender Datei (kein stiller Fallback, siehe [04, Abschnitt 4.7](04-Datenmodell-Validierung-Edgecases.md#4-edge-cases--wie-sie-behandelt-werden))
  - [x] `validate`-Kommando: `DocsValidator` aufrufen, Fehlerliste ausgeben, Exit-Code ≠ 0 bei Fehlern
  - [x] `import`-Kommando: zuerst `SchemaMigrator.Migrate(...)`, dann `ImportService.ImportAsync(...)` (siehe [03, Abschnitt 1](03-Projektstruktur-und-Konfiguration.md#1-solution-layout))
  - [x] `export`-Kommando: `ExportService.ExportAsync(...)` mit `--target`
  - [x] `server`-Kommando: Generic-Host-Grundgerüst mit MCP-Hosting (Tool-Inhalt folgt in Schritt 6)
  - [x] Serilog konfiguriert, Sink zwingend rotierende Datei unter `Logs/` relativ zur `.exe` (kein Konsolen-Sink); Rotation (Intervall, Aufbewahrungsdauer, Minimum-Level) konfigurierbar über `KnowHowToAiOptions.Logging`, siehe [03, Abschnitt 2](03-Projektstruktur-und-Konfiguration.md#2-konfiguration-appsettingsjson)
  - [x] `Console.OutputEncoding` auf BOM-loses UTF-8 gesetzt (Umlaute in Fehlermeldungen, ohne den stdout-JSON-RPC-Stream zu korrumpieren — siehe [02](02-Architektur-und-Techstack.md))
  - [x] Alle vier Kommandos fangen Fehler an der CLI-Grenze ab (`catch (Exception)`, klare Meldung + Exit-Code ≠ 0) statt roher .NET-Stacktraces
  - [x] Manuell smoke-getestet: `validate` (Erfolg + Fehlerfall), `import`/`export` gegen nicht erreichbaren SQL Server (klare Fehlermeldung, Exit 2), `server`-Start (stdout bleibt leer, Logging landet in `Logs/`)

- [x] **6. MCP-Stdio-Server**
  - [x] `SqlDocumentsStore.ListChildrenAsync` (SQL gegen `parent_slug`, NULL-sicher für Root)
  - [x] `SqlDocumentsStore.SearchDocsAsync` (`LIKE '%...%'`, kein Full-Text Search — siehe [04](04-Datenmodell-Validierung-Edgecases.md#search_docs-query-umgesetzt-in-sqldocumentsstoresearchdocsasync))
  - [x] `SqlDocumentsStore.GetDocAsync` (`DocumentDetail?`, `null` wenn Slug unbekannt)
  - [x] `DocsMcpTools` delegiert dünn an `SqlDocumentsStore` (per DI injiziert), gibt strukturierte Typen zurück statt JSON-Strings
  - [x] Jeder Tool-Aufruf loggt Parameter + Antwortgröße in Bytes (nicht den Inhalt), siehe [02, Abschnitt 4.D](02-Architektur-und-Techstack.md#d-knowhowtoaicli-server---config-path)
  - [x] `server`-Kommando hostet die drei Tools über stdio (`ModelContextProtocol`-SDK), `SqlDocumentsStore` als Singleton registriert
  - [x] `demo-docs/` als kleine Beispiel-Bibliothek angelegt, `appsettings.json` zeigt standardmäßig darauf
  - [ ] **Offen:** End-to-End-Verifikation gegen eine befüllte DB (list_children/search_docs/get_doc mit echten Ergebnissen) — blockiert durch ein SQL-Server-Setup-Problem auf dem Entwicklungsrechner (siehe [03, Abschnitt 2](03-Projektstruktur-und-Konfiguration.md#2-konfiguration-appsettingsjson), "Bekannter lokaler Stolperstein"), wird vom Projektverantwortlichen selbst geprüft

- [x] **7. Tests** — kein separater Schritt mehr nötig: Tests entstehen laut Git-Workflow-Regel im selben Commit wie das jeweilige Feature (siehe Schritte 2–4). Was in Schritt 5/6 an echter Logik entsteht, bekommt dort seine Tests; reines CLI-Wiring ist laut [02-testing.mdc](../.agents/rules/02-testing.mdc) davon ausgenommen.

- [x] **8. Setup-Dokumentation & Beispielkonfiguration**
  - [x] `appsettings.json` existiert bereits, voll funktionsfähig und committet (aus Schritt 1/6, siehe [03, Abschnitt 2](03-Projektstruktur-und-Konfiguration.md#2-konfiguration-appsettingsjson))
  - [x] `README.md` im Root (kurz, verweist auf `docs/` statt Inhalte zu duplizieren)
  - [x] `scripts/publish.ps1`: Single-File-Self-Contained-Build nach `publish/` (gitignored), siehe [03, Abschnitt 5](03-Projektstruktur-und-Konfiguration.md#5-deployment-single-file-publish)

- [x] **9. MCP-Resource `docs://authoring-guide` + Server-Instructions**
  - [x] `DocsMcpResources` (`KnowHowToAI.Cli/McpTools/`): statische Resource mit Front-Matter-Template, Slug-Regeln, Hierarchie-Regel als kompakter Markdown-Text — löst den Kaltstart-Fall (leeres/fremdes docs-root, Claude hat keine Beispiel-Dateien und kennt dieses Repo nicht), siehe [01, Phase 2](01-Konzept-und-Workflow.md#phase-2-doku-erweitern-oder-umstrukturieren-schreib-modus)
  - [x] `ServerInstructions` im `server`-Kommando gesetzt (kurzer Pointer auf die drei Tools + die Resource, kommt automatisch bei jeder Verbindung an)
  - [x] Reines Wiring ohne Verzweigungslogik → laut [02-testing.mdc](../.agents/rules/02-testing.mdc) keine separaten Unittests nötig, analog zu `DocsMcpTools`
  - [x] Manuell smoke-getestet gegen den echten stdio-Server (`initialize` liefert `instructions`, `resources/list` zeigt die Resource, `resources/read` liefert den vollständigen Guide-Text; stdout bleibt reines JSON-RPC)

### Definition of Done (v1)

- [x] `validate` erkennt alle in [04](04-Datenmodell-Validierung-Edgecases.md) beschriebenen Fehlerfälle korrekt und sammelt sie. Verifiziert per Unittests und manuellem CLI-Lauf (Erfolgs- und Fehlerfall).
- [ ] `import` legt Schema per DbUp an/aktualisiert es und befüllt die Tabelle transaktional neu. *Wiring steht und der Fehlerfall (SQL Server nicht erreichbar → klare Meldung, Exit 2) ist manuell verifiziert; der Erfolgspfad gegen einen echten, erreichbaren SQL Server steht noch aus (in dieser Umgebung kein SQL Server verfügbar).*
- [x] `export` respektiert die Marker-Datei-Logik strikt (kein Wipe ohne Marker). Verifiziert per Unittests (alle drei Szenarien aus [04, Abschnitt 4.4](04-Datenmodell-Validierung-Edgecases.md#44-export-marker-datei)) und manuellem CLI-Lauf.
- [ ] `server` beantwortet alle drei MCP-Tools korrekt gegen eine befüllte DB, inkl. leerer/Fehlerfälle ohne Absturz. *Implementiert und Host-Start manuell verifiziert (stdout bleibt leer); die drei Tools selbst sind noch nicht gegen eine befüllte DB durchgespielt — blockiert durch dasselbe SQL-Setup-Problem wie beim `import`-DoD-Punkt.*
- [ ] `search_docs` liefert korrekte Treffer für einen realistischen Testdatensatz (`LIKE`, siehe [04](04-Datenmodell-Validierung-Edgecases.md#search_docs-query-umgesetzt-in-sqldocumentsstoresearchdocsasync)). *Implementiert, aber noch nicht gegen reale Daten getestet — blockiert durch dasselbe SQL-Setup-Problem.*
- [x] Alle Unittests grün, Core-Projekt ohne Abhängigkeit zu CLI/MCP-SDK.

---

## v2+ (Backlog, nicht Teil des MVP)

Diese Punkte bewusst **nicht** in v1, um den Kern-Loop nicht zu verzögern:

* **Watch-Modus** (`docu-cli watch`): automatisches `validate`+`import` bei Dateiänderungen (FileSystemWatcher + Debouncing).
* **Multi-Library-Support**: mehrere unabhängige Docs-Bibliotheken/DBs gleichzeitig ansprechbar aus einem Server-Prozess (aktuell: eine Config = eine Bibliothek, mehrere Configs = mehrere MCP-Server-Einträge — das reicht für v1 völlig aus).
* **Schreib-Tools via MCP** (z.B. `create_doc`, `update_doc` direkt aus dem LLM heraus, ohne Umweg über Filesystem-Export). Bewusst zurückgestellt, da der Validierungs-Gate-Mechanismus (Abschnitt "Wipe and Dump") dafür erst durchdacht werden müsste (Race Conditions, wenn Claude parallel schreibt während `import` läuft).
* **Packaging als globales .NET-Tool** (`dotnet tool install --global`) statt reinem Build-Artefakt.
* **CI-Pipeline** (GitHub Actions: Build + Test bei jedem Push/PR).
* **Integrationstests gegen echten SQL Server** (z.B. via Testcontainers), zusätzlich zu den reinen Unittests aus v1.
* **Content-Chunking/Trunkierung** für sehr große Einzeldokumente in `get_doc`, falls Token-Verbrauch zum Problem wird.
* **Web-UI/Dashboard** zum Browsen der Bibliothek ohne LLM.
