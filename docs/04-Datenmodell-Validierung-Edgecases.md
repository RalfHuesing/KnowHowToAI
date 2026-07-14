# Datenmodell, Validierung & Edge Cases

Dieses Dokument ist die verbindliche Referenz für alle Grenzfälle. Ziel: Ein frischer Implementierungs-Chat muss **nichts raten**.

---

## 1. SQL-Skripte (`sql-scripts/`, DbUp-verwaltet)

DbUp führt alle Skripte im Ordner in aufsteigender Dateinamen-Reihenfolge genau einmal aus und protokolliert das in einer eigenen Journal-Tabelle (Standard: `dbo.SchemaVersions`, wird von DbUp automatisch angelegt).

**Laufzeit-Quelle: Embedded Resources, nicht Festplatte.** `sql-scripts/*.sql` bleibt der eine bearbeitbare Quellordner im Repo, wird aber beim Build in `KnowHowToAI.Core` als Embedded Resource eingebettet (`WithScriptsEmbeddedInAssembly`) statt zur Laufzeit von der Festplatte gelesen (`WithScriptsFromFileSystem`). Begründung: Das verteilte Artefakt ist eine einzelne `.exe` (siehe [00-Overview.md](00-Overview.md), Grundsatzentscheidung 8) — sie muss unabhängig vom Arbeitsverzeichnis oder Kopierziel funktionieren. Eine Skriptänderung erfordert dadurch einen Rebuild von `KnowHowToAI.Core`; das ist für dieses Projekt akzeptabel, da Schema-Änderungen ohnehin über den normalen Implementierungs-/Commit-Workflow laufen (siehe `.agents/rules/03-git-workflow.mdc`), nicht als Hotfix am laufenden System.

**Logging-Abstraktion:** `SchemaMigrator` (in `KnowHowToAI.Core/Migrations/`) nimmt DbUps `IUpgradeLog` als Parameter entgegen, statt selbst eine Logging-Bibliothek zu referenzieren. `KnowHowToAI.Cli` reicht beim Aufruf eine Serilog-basierte Implementierung durch, die zwingend auf `Console.Error` schreibt (siehe [02-Architektur-und-Techstack.md](02-Architektur-und-Techstack.md)) — DbUps eingebautes `LogToConsole()` würde auf `Console.Out` schreiben und das MCP-Protokoll korrumpieren und darf daher nicht verwendet werden.

### `0001_create_documents_table.sql`

```sql
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'documents' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.documents (
        slug        NVARCHAR(450)   NOT NULL PRIMARY KEY,
        parent_slug NVARCHAR(450)   NULL,
        title       NVARCHAR(400)   NOT NULL,
        content     NVARCHAR(MAX)   NOT NULL,
        tags        NVARCHAR(MAX)   NULL,
        synonyms    NVARCHAR(MAX)   NULL,
        CONSTRAINT FK_documents_parent
            FOREIGN KEY (parent_slug) REFERENCES dbo.documents(slug)
    );

    CREATE INDEX IX_documents_parent_slug ON dbo.documents(parent_slug);
END
```

### `search_docs`-Query (umgesetzt in `SqlDocumentsStore.SearchDocsAsync`)

```sql
SELECT slug AS Slug, title AS Title FROM dbo.documents
WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
ORDER BY title;
```

**Entscheidung: `LIKE '%...%'`, kein SQL Server Full-Text Search.** Ursprünglich war Full-Text Search (`CONTAINSTABLE`/`FREETEXTTABLE`) vorgesehen (natives Ranking). Verworfen, weil Full-Text Search eine separate, nicht auf jeder SQL-Server-Instanz installierte Feature-Komponente voraussetzt — auf der Ziel-Instanz dieses Projekts ist sie nicht vorhanden, und das Tool soll gegen eine Standard-Installation ohne Zusatzvoraussetzungen laufen. `LIKE` braucht kein Setup-Prerequisite, keinen Catalog, keinen Index. `@Pattern` wird in `SqlDocumentsStore.BuildLikePattern` aus der rohen LLM-Eingabe als `%query%` gebaut — anders als bei Full-Text-Such-Syntax kann dabei nichts an Sonderzeichen/Anführungszeichen in der Eingabe scheitern. Kein Ranking: Ergebnisse werden alphabetisch nach `title` sortiert, nicht nach Relevanz.

---

## 2. Slug-Regeln

* **Geltungsbereich:** jedes Pfadsegment eines Dateipfads relativ zum Docs-Root, ohne `.md`-Endung.
* **Regex pro Segment:** `^[a-z0-9]+(-[a-z0-9]+)*$`
* **Erlaubt:** `it`, `netzwerk-routing`, `core-switch-01`
* **Verboten:** Großbuchstaben, Umlaute (`ä ö ü ß`), Leerzeichen, Unterstriche, führende/folgende Bindestriche, doppelte Bindestriche.
* **Title/Tags/Synonyms im Front Matter sind davon nicht betroffen** — dort ist normales Deutsch (inkl. Umlaute, Leerzeichen, Groß-/Kleinschreibung) ausdrücklich erwünscht.
* **Begründung:** Windows ist case-insensitive, Linux/Git case-sensitive. Strikte lowercase-only-Slugs schließen Case-Collisions (`Routing.md` vs. `routing.md`) und Encoding-Probleme über Plattformen hinweg vollständig aus, unabhängig davon, auf welchem OS validate/import/export laufen.
* **Validator-Verhalten:** Jede Regelverletzung ist ein Fehler (kein Warning), Datei wird namentlich mit Grund gelistet.

---

## 3. Validierungsregeln (`validate`)

| Regel | Prüfung | Fehlerverhalten |
| --- | --- | --- |
| YAML-Syntax | Front Matter zwischen den `---`-Markern muss parsebar sein | Datei + YAML-Parser-Fehlermeldung |
| `title` Pflichtfeld | Front Matter muss `title` enthalten, nicht leer | Datei + "title fehlt" |
| Slug-Zeichen | Jedes Pfadsegment erfüllt die Regex aus Abschnitt 2 | Datei + ungültiges Segment |
| Orphan-Check | Für Slug `a/b/c` müssen `a.md` **und** `a/b.md` existieren | Datei + fehlender Parent-Pfad |
| Eindeutigkeit | Ergibt sich automatisch aus dem Dateisystem (ein Slug = ein Pfad) — keine zusätzliche Prüfung nötig, siehe Edge Case 4.1 |
| `tags`/`synonyms` optional | Dürfen fehlen oder leere Liste sein, dann `NULL` in der DB | kein Fehler |

Der Validator sammelt **alle** Fehler in einem Durchlauf (nicht beim ersten Fehler abbrechen) und gibt sie gesammelt aus — wichtig, damit Claude in einem Rutsch alle Probleme fixen kann, statt iterativ einen nach dem anderen zu entdecken.

---

## 4. Edge Cases & wie sie behandelt werden

### 4.1 Root-Dokument + gleichnamiger Ordner
`it.md` (Dokument) und `it/` (Ordner mit Kindern wie `it/netzwerk.md`) dürfen **gleichzeitig** existieren und sind der Normalfall, kein Konflikt: `it.md` liefert den Inhalt für Slug `it`, der Ordner `it/` ist reine Namensraum-Struktur für Kinder. Existiert `it/` als Ordner, aber **kein** `it.md`, ist das ein Orphan-Fehler (Regel aus Abschnitt 3).

### 4.2 Leeres Docs-Root-Verzeichnis / leere DB
`list_children(null)` auf einer leeren Tabelle liefert eine leere Liste, kein Fehler. `search_docs`/`get_doc` liefern ebenfalls leere/„nicht gefunden"-Antworten statt Exceptions — der MCP-Server darf nie wegen fehlender Daten abstürzen.

### 4.3 Import-Transaktion & Nebenläufigkeit
`DELETE FROM dbo.documents` + Neubefüllung laufen in **einer** Transaktion (`BEGIN TRAN` / `COMMIT`). Ein parallel laufender MCP-Server sieht dadurch nie einen halb-geleerten Zustand — SQL Server Standard-Isolationslevel (`READ COMMITTED`) reicht, da die Transaktion die Tabelle für die Dauer des Wipe-and-Dump exklusiv genug behandelt (Lesevorgänge während der Transaktion blockieren kurz oder sehen den alten Stand, abhängig vom Isolationslevel — für v1 ausreichend, kein `SNAPSHOT`-Isolation-Requirement).

### 4.4 Export-Marker-Datei
* **Zweck:** Verhindert, dass `export` versehentlich ein Fremdverzeichnis (z.B. `C:\Users\...\Documents`) leert.
* **Ablauf:**
  1. Zielverzeichnis existiert nicht oder ist leer → Tool erstellt es (falls nötig), schreibt sofort die Marker-Datei (`.knowhowtoai-export-marker.json`, Inhalt z.B. `{"tool":"KnowHowToAI.Cli","createdAt":"<ISO-Timestamp>"}`) und exportiert.
  2. Zielverzeichnis existiert, enthält Dateien, **Marker-Datei ist vorhanden** → alle `.md`-Dateien (nicht die Marker-Datei selbst, nicht andere Fremd-Dateien) werden gelöscht, danach vollständiger Re-Export.
  3. Zielverzeichnis existiert, enthält Dateien, **Marker-Datei fehlt** → Abbruch mit Fehlermeldung ("Zielverzeichnis enthält Dateien ohne Marker — bitte leeres Verzeichnis angeben oder Marker-Datei manuell anlegen, wenn der Inhalt sicher überschrieben werden darf."). Kein Datenverlust ohne explizite Bestätigung.
* Committed man das Docs-Root-Verzeichnis in Git, sollte die Marker-Datei ebenfalls eingecheckt werden (sie ist harmlos und signalisiert Folge-Läufen "hier darf gewiped werden").

### 4.5 Sonderzeichen in Front-Matter-Werten
`title`, `tags`, `synonyms` werden als vom Menschen lesbarer Text behandelt (UTF-8, Umlaute erlaubt). Bei der DB-Persistierung werden `tags`/`synonyms` als JSON-Array-Text (`["netzwerk","switch"]`) gespeichert, nicht als kommagetrennter String — eindeutiger beim Parsen in beide Richtungen (Import/Export). Für die `LIKE`-Suche (Abschnitt 1) bedeutet das: Ein Treffer in `tags`/`synonyms` matcht gegen die rohe JSON-Text-Repräsentation, nicht gegen einzelne Array-Elemente — für die Zwecke der Stichwortsuche ausreichend.

### 4.6 Sehr große Dokumente
`content NVARCHAR(MAX)` hat keine praktische Größenbegrenzung für Markdown-Dokumentation. Für `get_doc` gibt es in v1 kein Trunkieren — das LLM bekommt den vollen Inhalt. Falls das später zum Problem wird (sehr große Einzeldokumente), ist Chunking/Trunkieren ein Backlog-Thema (siehe [05-Roadmap.md](05-Roadmap.md)).

### 4.7 `--config` zeigt auf nicht existierende/fehlerhafte Datei
CLI bricht mit klarer Fehlermeldung ab (Pfad + "Datei nicht gefunden" bzw. Configuration-Binding-Fehler), kein stiller Fallback auf Defaults — falsche Konfiguration soll nie unbemerkt gegen die falsche Datenbank/den falschen Ordner laufen.

### 4.8 Verbindung zum SQL Server nicht erreichbar
`validate` benötigt **keine** DB-Verbindung (rein dateibasiert). `import`, `export` und `server` benötigen eine Verbindung — bei Fehlschlag: klare Fehlermeldung mit Connection-String-Ziel (Server/Datenbank-Name, ohne Zugangsdaten zu loggen), Exit-Code ≠ 0 bzw. beim MCP-Server ein Tool-Error statt Crash des ganzen Prozesses.
