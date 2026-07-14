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
│   │   │   ├── ImportService.cs       # Wipe-and-Dump, Transaktion, ruft SchemaMigrator zuerst auf
│   │   │   └── ExportService.cs       # Marker-Datei-Logik, MD-Generierung
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
        └── ImportExportServiceTests.cs
```

**Warum genau diese 3 Projekte?**
- `KnowHowToAI.Core` enthält die gesamte Logik ohne IO-Framework-Abhängigkeiten (kein `System.CommandLine`, kein MCP-SDK) → einfach und schnell testbar mit xUnit v3.
- `KnowHowToAI.Cli` ist der einzige Ort, der CLI-Parsing und MCP-Hosting kennt. Bleibt dünn (nur Wiring).
- `KnowHowToAI.Core.Tests` testet ausschließlich `Core` — keine Integrationstests gegen einen echten SQL Server in v1 (siehe [05-Roadmap.md](05-Roadmap.md)).

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
