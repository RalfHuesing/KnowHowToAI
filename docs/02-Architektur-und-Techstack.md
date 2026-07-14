# Architektur & Tech-Stack

## 1. Kernphilosophie & Code-Guidelines

* **Pragmatismus über Abstraktion:** Keine Interface-Wüsten (`IDocumentService`, `IDocumentRepository` etc. sind verboten, wenn es nur eine Implementierung gibt). Direkte, testbare Klassen.
* **Keep it Simple:** Early Returns nutzen, tiefe Verschachtelungen (`if-else`-Kaskaden) vermeiden, zyklomatische Komplexität minimal halten.
* **Source of Truth:** Die Wahrheit liegt im lokalen Dateisystem als Markdown-Dateien mit YAML Front Matter. MS SQL Server dient als performanter, relationaler Lese- und Suchcache für den MCP-Server.
* **Wipe and Dump:** Der Import-Prozess löscht die bestehenden Zeilen der `documents`-Tabelle komplett und baut sie aus den validierten Markdown-Dateien deterministisch neu auf — in einer Transaktion.
* **Kein ORM-Ballast:** Kein EF Core. Dapper für Queries, DbUp für Schema-Verwaltung.

---

## 2. Tech-Stack & Dependencies

| Bereich | Wahl | Begründung |
| --- | --- | --- |
| Runtime | .NET 10 (Console Application) | Aktuelle LTS-Version |
| Protokoll | MCP via stdio | Standard für Cursor/Claude Desktop/Claude Code |
| MCP-SDK | [`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) (offizielles C#-SDK) | Attribute-basierte Tool-Registrierung (`[McpServerTool]`), fertiges stdio-Hosting über `Microsoft.Extensions.Hosting` |
| CLI-Parsing | `System.CommandLine` | Subcommands, Optionen, Auto-Help, offizielle .NET-Library |
| Datenbank | **MS SQL Server** (lokal oder im Netzwerk) | Vorgabe: kein anderer SQL-Dialekt vorgesehen |
| DB-Zugriff | **Dapper** + `Microsoft.Data.SqlClient` | Schlanke, schnelle SQL-Queries ohne EF-Core-Ballast |
| Schema-Verwaltung | **DbUp** + nummerierte Skripte in `sql-scripts/` | Idempotente, versionierte Schema-Migration ohne ORM |
| Suche | **`LIKE '%...%'`** über `title`/`content`/`tags`/`synonyms` | Kein Full-Text-Search-Feature vorausgesetzt (nicht auf jeder Ziel-Instanz installiert), kein RAG-Overkill |
| Front-Matter-Parsing | `YamlDotNet` | Etablierter, schlanker YAML-Parser für .NET |
| Logging | **Serilog**, Sink ausschließlich auf eine rotierende Datei unter `Logs/` relativ zur `.exe` | `Console.Out` ist exklusiv für das MCP-JSON-RPC-Protokoll reserviert, `Console.Error` wäre bei einem von Cursor/Claude Desktop gestarteten Hintergrundprozess ohnehin nicht einsehbar und nicht persistent |
| Testing | **xUnit v3** | Fokus auf Unit-Tests für Parser, Validator, Import/Export-Logik |
| Konfiguration | `Microsoft.Extensions.Configuration` (`appsettings.json` + Umgebungsvariablen-Override) | Ein Konfigurationsort pro Einsatzort, siehe [03](03-Projektstruktur-und-Konfiguration.md) |
| Linting | **AiNetLinter** (externes CLI-Tool, als Test im Testprojekt eingebunden) | Roslyn-basierte Qualitätsprüfung (Komplexität, Sealed Classes, Phantom-Dependencies) zusätzlich zu Build+Tests; Details siehe [03, Abschnitt 4](03-Projektstruktur-und-Konfiguration.md#4-ainetlinter-code-qualitäts-gate) |

> **Kritischer Hinweis für die Implementierung:** Beim MCP-Server darf **absolut nichts** auf `Console.Out`/`Console.Write` loggen, da dies das JSON-RPC-Protokoll korrumpiert. Serilog schreibt deshalb ausschließlich in eine rotierende Datei (`Logs/knowhowtoai-<Datum>.log`, täglich rollend, 14 Tage aufbewahrt, `AppContext.BaseDirectory`-relativ) — kein Konsolen-Sink für keines der vier Kommandos, siehe [Program.cs](../src/KnowHowToAI.Cli/Program.cs).
>
> **Console-Encoding:** `Program.cs` setzt `Console.OutputEncoding` auf `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)`, nicht auf `Encoding.UTF8`. Deutsche Fehlermeldungen (Umlaute) würden auf der Windows-Konsole sonst falsch dargestellt — `Encoding.UTF8` selbst erzeugt aber eine BOM-Präambel beim ersten Schreibzugriff, die im `server`-Modus die ersten Bytes des stdout-JSON-RPC-Streams korrumpieren würde.

---

## 3. Datenstruktur & Datei-Layout

### Die Markdown-Dateien (Filesystem, Docs-Root)

Die Hierarchie wird rein über die Ordnerstruktur und den relativen Pfad (den **Slug**) definiert. Jede Datei enthält YAML Front Matter. Slug-Regeln siehe [04-Datenmodell-Validierung-Edgecases.md](04-Datenmodell-Validierung-Edgecases.md#slug-regeln).

**Beispiel-Datei:** `it/netzwerk/routing.md`

```markdown
---
title: "Routing-Tabelle Core-Switch"
tags: [netzwerk, switch, cisco]
synonyms: [routing, gateway, statische-route]
---
# Routing-Tabelle Core-Switch

Hier steht der eigentliche Dokumenteninhalt im Markdown-Format...
```

Der relative Dateipfad ohne Endung ist der `slug` (z.B. `it/netzwerk/routing`) und zugleich der `PRIMARY KEY` in der DB. Die Hierarchie baut sich über `parent_slug` auf (z.B. `it/netzwerk`, `NULL` für Root-Dokumente).

### Das Datenbankschema (MS SQL Server) — Kurzüberblick

Vollständige DDL-Skripte: [04-Datenmodell-Validierung-Edgecases.md](04-Datenmodell-Validierung-Edgecases.md).

```sql
CREATE TABLE dbo.documents (
    slug        NVARCHAR(450)   NOT NULL PRIMARY KEY,  -- z.B. 'it/netzwerk/routing'
    parent_slug NVARCHAR(450)   NULL,                   -- z.B. 'it/netzwerk' (NULL für Root)
    title       NVARCHAR(400)   NOT NULL,
    content     NVARCHAR(MAX)   NOT NULL,               -- reiner MD-Inhalt unter dem Front Matter
    tags        NVARCHAR(MAX)   NULL,                   -- JSON-Array als Text
    synonyms    NVARCHAR(MAX)   NULL,                   -- JSON-Array als Text
    CONSTRAINT FK_documents_parent
        FOREIGN KEY (parent_slug) REFERENCES dbo.documents(slug) ON DELETE NO ACTION
);
```

> `ON DELETE CASCADE` ist auf sich selbst referenzierenden Tabellen in SQL Server nicht erlaubt (Zyklus-Gefahr). Da `import` ohnehin per `DELETE FROM documents` (ohne WHERE) den kompletten Tabelleninhalt in einer Transaktion leert, ist Cascade nicht nötig — siehe [04](04-Datenmodell-Validierung-Edgecases.md).

---

## 4. Die Komponenten & Sub-Commands

Gesteuert über `System.CommandLine`-Subcommands.

### A. `KnowHowToAI.Cli validate --config <path>`

Prüft das lokale Docs-Root-Verzeichnis, bevor importiert wird.

* **YAML-Check:** Ist das Front Matter valides YAML? Ist `title` vorhanden?
* **Slug-Check:** Entspricht jeder Pfadsegment-Name der strikten Regel (`a-z`, `0-9`, `-`)?
* **Hierarchie-Check:** Gibt es verwaiste Pfade? (Existiert `it/netzwerk/routing.md`, muss auch `it/netzwerk.md` **und** `it.md` existieren.)
* Gibt bei Fehlern eine Liste `Datei → Grund` aus, Exit-Code ≠ 0.

### B. `KnowHowToAI.Cli import --config <path>`

* Führt zuerst DbUp aus (Schema auf aktuellem Stand bringen).
* Triggert intern `validate`. Nur bei Erfolg geht es weiter.
* In einer Transaktion: `DELETE FROM dbo.documents;` gefolgt von Bulk-Insert aller geparsten Dateien via Dapper.

### C. `KnowHowToAI.Cli export --config <path> --target <dir>`

* Prüft Marker-Datei im Zielverzeichnis (siehe [04](04-Datenmodell-Validierung-Edgecases.md#export-marker-datei)).
* Liest alle Zeilen aus `dbo.documents`.
* Erstellt die Ordnerstruktur im Zielverzeichnis basierend auf den Slugs und schreibt `.md`-Dateien inkl. generiertem YAML Front Matter neu.

### D. `KnowHowToAI.Cli server --config <path>`

Startet die App im stdio-Modus. Bietet exakt **drei MCP-Tools** (`KnowHowToAI.Cli.McpTools.DocsMcpTools`, dünne Delegation an `SqlDocumentsStore`):

1. **`list_children(parent_slug)`** → `IReadOnlyList<DocumentSummary>` (Slug + Title)
   *SQL:* `SELECT slug, title FROM dbo.documents WHERE parent_slug = @ParentSlug` (bzw. `IS NULL` für Root)
   *Zweck:* Ermöglicht dem LLM das gezielte "Durchblättern" der Bibliothek entlang der Fachbereiche.
2. **`search_docs(query)`** → `IReadOnlyList<DocumentSummary>`
   *SQL:* `LIKE '%query%'` gegen `title`, `content`, `tags`, `synonyms` (siehe [04, Abschnitt "search_docs-Query"](04-Datenmodell-Validierung-Edgecases.md#search_docs-query-umgesetzt-in-sqldocumentsstoresearchdocsasync)).
   *Zweck:* Einfache, robuste Stichwortsuche ohne SQL-Server-Feature-Voraussetzung.
3. **`get_doc(slug)`** → `DocumentDetail?` (Title + Content, `null` wenn Slug unbekannt)
   *SQL:* `SELECT title, content FROM dbo.documents WHERE slug = @Slug`
   *Zweck:* Lazy-Loading des eigentlichen Inhalts, sobald das LLM das Ziel-Dokument identifiziert hat.

Die Tools geben strukturierte Typen zurück statt roher JSON-Strings — das MCP-SDK serialisiert sie automatisch; manuelles `JsonSerializer.Serialize` in den Tool-Methoden entfällt.

**Zusätzlich eine MCP-Resource, kein viertes Tool:** `docs://authoring-guide` (`KnowHowToAI.Cli.McpTools.DocsMcpResources`) liefert das Datei-Format (Front-Matter-Template, Slug-Regeln, Hierarchie-/Orphan-Regel) als kompakten Markdown-Text — nötig, damit Claude auch in einem leeren docs-root eines fremden Projekts weiß, wie eine neue `.md`-Datei aussehen muss, ohne dieses Repo zu kennen (siehe [01, Phase 2](01-Konzept-und-Workflow.md#phase-2-doku-erweitern-oder-umstrukturieren-schreib-modus)). Zusätzlich setzt der Server `ServerInstructions` (kurzer Hinweis auf die drei Tools + die Resource), der bei jeder Verbindung automatisch beim Client ankommt. MCP-Resources sind ein eigener Protokoll-Typ, kein Tool — die Zählung "drei schlanke MCP-Tools" ([00-Overview.md](00-Overview.md)) bleibt unverändert.

Details zu Implementierungsreihenfolge: [05-Roadmap.md](05-Roadmap.md).
