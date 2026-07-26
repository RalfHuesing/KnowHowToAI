# Architektur & Tech-Stack

## 1. Kernphilosophie & Code-Guidelines

* **Pragmatismus über Abstraktion:** Keine Interface-Wüsten (`IDocumentService`, `IDocumentRepository` etc. sind verboten, wenn es nur eine Implementierung gibt). Direkte, testbare Klassen.
* **Keep it Simple:** Early Returns nutzen, tiefe Verschachtelungen (`if-else`-Kaskaden) vermeiden, zyklomatische Komplexität minimal halten.
* **Source of Truth:** Die Wahrheit liegt im lokalen Dateisystem als Markdown-Dateien mit YAML Front Matter. MS SQL Server dient als performanter, relationaler Lese- und Suchcache für den MCP-Server.
* **Wipe and Dump:** Der Import-Prozess löscht die bestehenden Zeilen der `documents`-Tabelle komplett und baut sie aus den validierten Markdown-Dateien deterministisch neu auf — in einer Transaktion.
* **Kein ORM-Ballast:** Kein EF Core. Dapper für Queries, ein schlanker eigener `SchemaMigrator` (kein DbUp, keine Journal-Tabelle — siehe [04, Abschnitt 1](04-Datenmodell-Validierung-Edgecases.md#1-sql-skripte-sql-scripts)) für Schema-Verwaltung.

---

## 2. Tech-Stack & Dependencies

| Bereich | Wahl | Begründung |
| --- | --- | --- |
| OS & Shell | **Windows** + **PowerShell 7 (`pwsh`)** | Primäre Entwicklungs- und Ausführungsumgebung. Tool-Aufrufe nutzen pwsh, `rg`, `dotnet` CLI, `git`, `python`, `node` (keine Linux/Bash-Befehlsketten). Details siehe [.agents/rules/07-environment.mdc](../.agents/rules/07-environment.mdc) |
| Runtime | .NET 10 (Console Application) | Aktuelle LTS-Version |
| Protokoll | MCP via stdio | Standard für MCP-Clients und AI-Agenten |
| MCP-SDK | [`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) (offizielles C#-SDK) | Attribute-basierte Tool-Registrierung (`[McpServerTool]`), fertiges stdio-Hosting über `Microsoft.Extensions.Hosting` |
| CLI-Parsing | `System.CommandLine` | Subcommands, Optionen, Auto-Help, offizielle .NET-Library |
| Datenbank | **MS SQL Server** (lokal oder im Netzwerk) | Vorgabe: kein anderer SQL-Dialekt vorgesehen |
| DB-Zugriff | **Dapper** + `Microsoft.Data.SqlClient` | Schlanke, schnelle SQL-Queries ohne EF-Core-Ballast |
| Schema-Verwaltung | Eigener `SchemaMigrator` + nummerierte, selbst-idempotente Skripte in `sql-scripts/` | Kein ORM, keine Journal-/Versionstabelle — Skripte prüfen selbst per `IF NOT EXISTS`, ob es etwas zu tun gibt |
| Suche | **`LIKE '%...%'`** über `title`/`content`/`tags`/`synonyms` | Kein Full-Text-Search-Feature vorausgesetzt (nicht auf jeder Ziel-Instanz installiert), kein RAG-Overkill |
| Front-Matter-Parsing | `YamlDotNet` | Etablierter, schlanker YAML-Parser für .NET |
| Logging | **Serilog**, Sink ausschließlich auf eine rotierende Datei unter `Logs/` relativ zur `.exe` | `Console.Out` ist exklusiv für das MCP-JSON-RPC-Protokoll reserviert, `Console.Error` wäre bei einem von einem MCP-Host gestarteten Hintergrundprozess ohnehin nicht einsehbar und nicht persistent |
| Testing | **xUnit v3** | Fokus auf Unit-Tests für Parser, Validator, Import/Export-Logik |
| Konfiguration | `Microsoft.Extensions.Configuration` (`appsettings.json` + Umgebungsvariablen-Override) | Ein Konfigurationsort pro Einsatzort, siehe [03](03-Projektstruktur-und-Konfiguration.md) |
| Linting | **AiNetLinter** (externes CLI-Tool, als Test im Testprojekt eingebunden) | Roslyn-basierte Qualitätsprüfung (Komplexität, Sealed Classes, Phantom-Dependencies) zusätzlich zu Build+Tests; Details siehe [03, Abschnitt 4](03-Projektstruktur-und-Konfiguration.md#4-ainetlinter-code-qualitäts-gate) |

> **Kritischer Hinweis für die Implementierung:** Beim MCP-Server darf **absolut nichts** auf `Console.Out`/`Console.Write` loggen, da dies das JSON-RPC-Protokoll korrumpiert. Serilog schreibt deshalb ausschließlich in eine rotierende Datei (`Logs/knowhowtoai-<Datum>.log`, `AppContext.BaseDirectory`-relativ) — kein Konsolen-Sink für keines der vier Kommandos, siehe [Program.cs](../src/KnowHowToAI.Cli/Program.cs). Rotation (Intervall, Aufbewahrungsdauer) und Minimum-Level sind **nicht** hartcodiert, sondern kommen aus `KnowHowToAiOptions.Logging` (Defaults: täglich rollend, 14 Tage, `Information`), siehe [03, Abschnitt 2](03-Projektstruktur-und-Konfiguration.md#2-konfiguration-appsettingsjson) und [06-configuration.mdc](../.agents/rules/06-configuration.mdc).
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

Vollständige DDL-Skripte: [04-Datenmodell-Validierung-Edgecases.md](04-Datenmodell-Validierung-Edgecases.md). Der Tabellenname `documents` ist der Default aus `KnowHowToAiOptions.DocumentsTableName` — pro `appsettings.json` frei umbenennbar (siehe [03, Abschnitt 2](03-Projektstruktur-und-Konfiguration.md#2-konfiguration-appsettingsjson)), z.B. um mehrere thematisch getrennte Wissensbibliotheken in derselben Datenbank zu halten.

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

> `ON DELETE CASCADE` ist auf sich selbst referenzierenden Tabellen in SQL Server nicht erlaubt (Zyklus-Gefahr). Da `import` ohnehin per `DELETE FROM <DocumentsTableName>` (ohne WHERE) den kompletten Tabelleninhalt in einer Transaktion leert, ist Cascade nicht nötig — siehe [04](04-Datenmodell-Validierung-Edgecases.md).

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
2. **`search_docs(query)`** → `SearchResult { results: DocumentSummary[], truncated: bool }`
   *SQL:* `LIKE '%query%'` gegen `title`, `content`, `tags`, `synonyms` (siehe [04, Abschnitt "search_docs-Query"](04-Datenmodell-Validierung-Edgecases.md#search_docs-query-umgesetzt-in-sqldocumentsstoresearchdocsasync)).
   *Query-Semantik:* `LIKE '%query%'` mit Bracket-Escaping (`%`/`_`/`[` werden literal behandelt), kein Wildcard-Smuggling möglich. Längen-Cap via `KnowHowToAi.Search.MaxQueryLength` (Default 200), längere Queries lösen `ArgumentException` aus.
   *Response-Shape:* `truncated: true` bedeutet, dass die Suche mehr Treffer hat als `MaxResults` (Default 50) — der `truncated`-Marker ist die einzige Möglichkeit für das LLM zu erkennen, dass die Trefferliste gekappt wurde, und sollte zur Verfeinerung der Suche führen statt alle Treffer zu erwarten.
   *Deterministische Sortierung:* Title-Treffer zuerst, dann alphabetisch nach `title` (kein Full-Text-Ranking, konsistent mit `LIKE`-Architektur).
   *Zweck:* Einfache, robuste Stichwortsuche ohne SQL-Server-Feature-Voraussetzung. Volle Tool-Description siehe [04, Abschnitt 1](04-Datenmodell-Validierung-Edgecases.md#1-sql-skripte-sql-scripts).
3. **`get_doc(slug)`** → `DocumentDetail?` (Title + Content, `null` wenn Slug unbekannt)
   *SQL:* `SELECT title, content FROM dbo.documents WHERE slug = @Slug`
   *Zweck:* Lazy-Loading des eigentlichen Inhalts, sobald das LLM das Ziel-Dokument identifiziert hat.

Die Tools geben strukturierte Typen zurück statt roher JSON-Strings — das MCP-SDK serialisiert sie automatisch; manuelles `JsonSerializer.Serialize` in den Tool-Methoden entfällt.

#### Quell-Doku für die Tool-Descriptions

Die `[Description(...)]`-Strings in `DocsMcpTools.cs` sind aus diesem Abschnitt gespeist — bei Änderungen an einer Tool-Beschreibung hier und im Code synchron halten (sonst driften LLM-Sicht und Architektur-Doku auseinander). Die Description ist die knappe LLM-Sicht; dieser Unterabschnitt ist die ausführliche Architektur-Begründung für jede Aussage in der Description.

**`list_children` — Detail-Begründungen:**

* **Sortierung alphabetisch nach Slug:** stabil, deterministisch, gut cache-bar; eine Slug-Reihenfolge ist die einzige, die für LLM-Navigation entlang der Hierarchie Sinn ergibt. Sortierung nach `title` wäre mehrdeutig (`"IT"` vs. `"it"`).
* **Keine Cap:** das Tool liefert nur Slug + Title, beides sehr kompakt; ein breites Verzeichnis mit 500 Root-Dokumenten wäre im Token-Budget unkritisch. Eine Cap wäre ein falscher Anreiz — das LLM müsste dann raten, ob es etwas übersieht, statt das Verzeichnis entlangzublättern. Wer eine harte Obergrenze braucht, kann sie im Client filtern.
* **Ungültiger Slug wird akzeptiert, liefert leere Liste:** das ist Konsequenz aus der reinen SQL-`WHERE parent_slug = @ParentSlug`-Semantik — wenn nichts matched, kommt nichts zurück, kein Fehler. Validierung *vor* dem SQL-Round-Trip wäre zusätzlicher Ballast (Slug-Regeln stehen in [04, Abschnitt 2](04-Datenmodell-Validierung-Edgecases.md#2-slug-regeln) und werden vom `validate`-Subcommand geprüft, nicht zur MCP-Laufzeit).
* **Leerer String `""` ≠ `null`:** semantischer Unterschied — `null` heißt "Wurzel", `""` ist ein konkreter (ungültiger) Slug und führt zur `ArgumentException`, die das SQL-Parameter-Binding wirft. Beide Fälle explizit dokumentiert, damit das LLM nicht überrascht ist.

**`search_docs` — Detail-Begründungen:**

* **Response-Shape `{ results, truncated }`:** der `truncated`-Marker ist die *einzige* Möglichkeit für das LLM, eine gekappte Trefferliste zu erkennen — `results.Count < MaxResults` ist aus dem LLM-Sichtfeld nicht ableitbar, wenn die Gesamtzahl unbekannt ist. Token-Budget-Schutz (F-PE-001) und LLM-UX (F-MC-001) greifen ineinander: ohne Marker würde das LLM entweder massiv Treffer erwarten und die Liste „durchscrollen", oder blind weiter suchen, weil es nicht weiß, ob es schon alles hat.
* **Wildcard-Literal-Verhalten (`%`, `_`, `[` escapet):** SQL-Injection ist über die Dapper-Parameter-Bindung ausgeschlossen, aber `LIKE`-Wildcards sind eine *funktionale* Falle — eine LLM-Eingabe `"%"` würde sonst alle Zeilen matchen. Bracket-Escape (`%`→`[%]` etc., siehe [04, Abschnitt 1](04-Datenmodell-Validierung-Edgecases.md#search_docs-query-umgesetzt-in-sqldocumentsstoresearchdocsasync)) zwingt die Eingabe in eine rein literale Suche. Begründung: F-SE-001 (DoS-Schutz gegen Wildcard-Smuggling) plus Vorhersehbarkeit für das LLM.
* **Sortierung — Title-Treffer zuerst, dann alphabetisch:** bewusst keine komplexere Ranking-Heuristik (kein Full-Text-Score, keine Token-Gewichtung), konsistent mit der `LIKE`-Architektur (kein FTS-Feature vorausgesetzt). Title-Treffer sind die höchste Signalstärke ("der Benutzer sucht genau das"), alles andere ist alphabetisch stabil. Begründung: F-PE-002.
* **Leere/Whitespace-Query → leere `results`, kein Fehler:** konsistent mit Edge Case 4.2 „leere DB" (kein Server-Absturz wegen fehlender Daten). Ein Fehler wäre pädagogisch fragwürdig — das LLM würde dann über die Fehlermeldung stolpern, statt die harmlose leere Antwort zu akzeptieren.
* **Längen-Cap (`MaxQueryLength` Default 200):** trivialer DoS-Vektor gegen den SQL-Server (riesige Pattern-Strings → lang laufende `LIKE`-Scans). Cap vor dem SQL-Round-Trip, sauberer `ArgumentException` als Tool-Error. Default 200 ist deutlich größer als jede realistische Suchanfrage, klein genug, um keine Performance-Sorgen zu erlauben.
* **Treffer-Cap (`MaxResults` Default 50):** Token-Budget-Schutz. 50 `DocumentSummary`-Datensätze (Slug + Title) sind im LLM-Budget gut tragbar; bei mehr Treffern lieber verfeinern als 500 Einträge zurückgeben.

**`get_doc` — Detail-Begründungen:**

* **`null` bei unbekanntem Slug statt Tool-Error:** das LLM kann den Slug selbst finden (über `list_children` + `search_docs`); ein Error wäre eine Sackgasse. `null` als normales "nicht gefunden"-Signal ist die etablierte Konvention in [04, Edge Case 4.2](04-Datenmodell-Validierung-Edgecases.md#42-leeres-docs-root-verzeichnis-leere-db).
* **Kein Truncation-Mechanismus in v1:** der Content ist `NVARCHAR(MAX)`, kommt 1:1 zurück. Bei sehr großen Dokumenten (>50 KB) kann das das Token-Budget sprengen. Bewusste Entscheidung gegen v1-Truncation: das LLM muss die Wahl der Aufteilung selbst treffen (mehrere kleine Slugs statt ein riesiger). Ein clientseitiger Truncator wäre ein falscher Anreiz (Content-Ende wäre unbemerkt verloren).
* **YAML-Front-Matter nicht im Content:** wird beim `import` aus den `.md`-Dateien geparst und in eigene DB-Spalten (`title`, `tags`, `synonyms`) geschrieben; im `content`-Feld steht nur der Body. LLM bekommt also *nicht* das Original-Front-Matter zu sehen — falls nötig, müssen `tags`/`synonyms` über eine zukünftige Tool-Erweiterung exponiert werden.

**Sichtbarkeit ohne SQL Profiler:** Jeder Tool-Aufruf loggt vor der SQL-Abfrage seine Parameter (z.B. `search_docs(query=...)`) und nach der Abfrage die Größe der Antwort als Item-Count für Listen bzw. Content-Länge für `get_doc` (ohne erneute JSON-Serialisierung) — **nicht** deren Inhalt, da der Log sonst selbst zum riesigen, unübersichtlichen Datenberg würde. Damit ist im Log erkennbar, was das verbundene LLM anfragt und wie viel Datenvolumen zurückgeht, ohne einen SQL Profiler mitlaufen lassen zu müssen.

**Zusätzlich eine MCP-Resource, kein viertes Tool:** `docs://authoring-guide` (`KnowHowToAI.Cli.McpTools.DocsMcpResources`) liefert das Datei-Format (Front-Matter-Template, Slug-Regeln, Hierarchie-/Orphan-Regel) als kompakten Markdown-Text — nötig, damit ein Agent auch in einem leeren docs-root eines fremden Projekts weiß, wie eine neue `.md`-Datei aussehen muss, ohne dieses Repo zu kennen (siehe [01, Phase 2](01-Konzept-und-Workflow.md#phase-2-doku-erweitern-oder-umstrukturieren-schreib-modus)). Zusätzlich setzt der Server `ServerInstructions` (kurzer Hinweis auf die drei Tools + die Resource), der bei jeder Verbindung automatisch beim Client ankommt. MCP-Resources sind ein eigener Protokoll-Typ, kein Tool — die Zählung "drei schlanke MCP-Tools" ([00-Overview.md](00-Overview.md)) bleibt unverändert.

Details zu Implementierungsreihenfolge: [05-Roadmap.md](05-Roadmap.md).
