# Roadmap

## v1 (MVP) — Kern-Loop zuerst

Ziel: Der komplette Doku-Loop (`validate` → `import` → `server`, plus `export`) funktioniert Ende-zu-Ende gegen einen echten MS SQL Server, mit den drei MCP-Tools nutzbar in Cursor/Claude Desktop/Claude Code.

### Implementierungs-Reihenfolge (für den frischen Umsetzungs-Chat)

1. **Solution & Projekt-Setup** ✅
   Solution `KnowHowToAI.sln` mit `src/KnowHowToAI.Core`, `src/KnowHowToAI.Cli`, `tests/KnowHowToAI.Core.Tests` anlegen (siehe [03-Projektstruktur-und-Konfiguration.md](03-Projektstruktur-und-Konfiguration.md)). NuGet-Pakete einbinden: `Dapper`, `Microsoft.Data.SqlClient`, `dbup-sqlserver`, `System.CommandLine`, `ModelContextProtocol`, `Serilog` (+ `Serilog.Sinks.Console`), `YamlDotNet`. xUnit-v3-Testprojekt verdrahten.

2. **SQL-Schema & DbUp-Integration** ✅
   `sql-scripts/0001_create_documents_table.sql` und `0002_create_fulltext_catalog_and_index.sql` anlegen (siehe [04](04-Datenmodell-Validierung-Edgecases.md#1-sql-skripte-sql-scripts-dbup-verwaltet)). DbUp-Runner-Code in `Core` oder `Cli`, der beim `import`-Kommando vor dem eigentlichen Sync läuft.

3. **Domain-Model & Front-Matter-Parser/Validator** ✅
   `Document`-Klasse, `FrontMatterParser` (YamlDotNet), `SlugRules` (Regex-Validierung), `DocsValidator` (YAML-Check, Slug-Check, Orphan-Check — alle Fehler sammeln, siehe [04](04-Datenmodell-Validierung-Edgecases.md#3-validierungsregeln-validate)). Unittests für valide/invalide Dateien, Slug-Regeln, Orphan-Szenarien.

4. **Import/Export-Engine** ✅
   `ImportService`: Validate + Wipe-and-Dump, SQL-Zugriff als Delegate (nicht als Interface, siehe [03, Abschnitt 1](03-Projektstruktur-und-Konfiguration.md#1-solution-layout)). `ExportService`: Marker-Datei-Logik (siehe [04, Abschnitt 4.5](04-Datenmodell-Validierung-Edgecases.md#45-export-marker-datei)) + MD-Dateien inkl. YAML-Front-Matter-Generierung schreiben. `SqlDocumentsStore`: einziger Ort mit echtem SQL-Zugriff via Dapper.

5. **CLI-Wiring**
   `Program.cs` mit `System.CommandLine`-Subcommands `validate`, `import`, `export`, `server`, jeweils mit `--config`-Option (siehe [03](03-Projektstruktur-und-Konfiguration.md#3-cli-kommandos-übersicht)). Konfigurationsbindung (`KnowHowToAiOptions`) aus `appsettings.json` + Env-Var-Override. **`import` ruft hier zuerst `SchemaMigrator.Migrate(...)` auf und erst danach `ImportService.ImportAsync(...)`** (Migration ist bewusst nicht Teil von `ImportService` selbst, siehe [03, Abschnitt 1](03-Projektstruktur-und-Konfiguration.md#1-solution-layout)).

6. **MCP-Stdio-Server**
   `DocsMcpTools` mit `[McpServerTool]`-Methoden `list_children`, `search_docs`, `get_doc`, gemappt auf Dapper-Queries (Full-Text-Query für `search_docs`, siehe [04](04-Datenmodell-Validierung-Edgecases.md#search_docs-query-beispiel)). Serilog zwingend auf `Console.Error`.

7. **Tests**
   xUnit v3 für Parser, Validator, Slug-Regeln, Import/Export-Logik (Core, ohne echten SQL Server — DB-Zugriff hinter einem schmalen Interface/Delegate isolieren, damit Core-Tests ohne Datenbank laufen).

8. **Setup-Dokumentation & Beispielkonfiguration**
   `appsettings.example.json`, README mit Setup-Schritten (SQL-Server-Voraussetzungen inkl. Full-Text-Feature, `dotnet publish`, MCP-Launch-Config-Beispiel — siehe [03](03-Projektstruktur-und-Konfiguration.md#mcp-launch-konfiguration-beispiel-für-claude-desktopcursor)).

### Definition of Done (v1)

- [ ] `validate` erkennt alle in [04](04-Datenmodell-Validierung-Edgecases.md) beschriebenen Fehlerfälle korrekt und sammelt sie.
- [ ] `import` legt Schema per DbUp an/aktualisiert es und befüllt die Tabelle transaktional neu.
- [ ] `export` respektiert die Marker-Datei-Logik strikt (kein Wipe ohne Marker).
- [ ] `server` beantwortet alle drei MCP-Tools korrekt gegen eine befüllte DB, inkl. leerer/Fehlerfälle ohne Absturz.
- [ ] Full-Text-Suche liefert sinnvoll gerankte Treffer für einen realistischen Testdatensatz.
- [ ] Alle Unittests grün, Core-Projekt ohne Abhängigkeit zu CLI/MCP-SDK.

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
