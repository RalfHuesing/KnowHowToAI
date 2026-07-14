# Konzept & Workflow: Das Zusammenspiel von Mensch, KI und MCP

Dieses Dokument beschreibt die **Intention**, den **Mehrwert** und den **täglichen Workflow** des Tools. Es ist die konzeptionelle Brücke zum technischen Architektur-Dokument ([02-Architektur-und-Techstack.md](02-Architektur-und-Techstack.md)).

---

## 1. Die Intention: Warum dieses Setup?

Klassische Dokumentations-Systeme scheitern im KI-Zeitalter an zwei Extremen:

1. **Das Datei-Chaos:** Ein riesiger Ordner voller unstrukturierter Markdown-Dateien überfordert den Kontext und die Suchfähigkeiten des LLMs.
2. **Die RAG-Infrastruktur-Hölle:** Vektor-Datenbanken sind für lokale, strukturierte Fachdokumentationen oft Overkill, neigen zu unvollständigen Textfragmenten und bieten dem LLM keine Orientierung über die Gesamtstruktur (die "Bibliothek").

### Unser Ansatz: Das Beste aus beiden Welten

Wir trennen die **Autorenumgebung** (wo geschrieben wird) strikt von der **Leseumgebung** (wo die KI sucht).

* **Das Dateisystem (Markdown + YAML Front Matter) ist für den Menschen & die KI zum Schreiben.** Markdown ist der globale Standard für Dokumentation, lässt sich versionieren (Git), leicht editieren und von jeder KI fehlerfrei generieren.
* **MS SQL Server ist für die KI zum Lesen (via MCP).** SQL ermöglicht schnelle, deterministische Abfragen über Hierarchien, Tags, Synonyme und Volltext. Das LLM erhält eine strukturierte Landkarte der Dokumente, statt blind in Textwüsten zu fischen.

> **Wichtig — kein Offline-Anspruch:** Anders als ein rein dateibasierter Ansatz benötigt dieses Tool einen erreichbaren SQL-Server (lokal oder im Netzwerk). Das ist eine bewusste Entscheidung: SQL Server bringt robustes Full-Text-Ranking, Transaktionssicherheit und Mehrbenutzerfähigkeit, die eine lokale SQLite-Datei nicht in gleicher Qualität bietet. Der Server-Zugriff ist reine Konfigurationssache (`appsettings.json`), keine Architekturschwäche.

---

## 2. Die Akteure und ihre Rollen

| Akteur | Rolle | Hauptwerkzeug |
| --- | --- | --- |
| **Der Entwickler** | Steuert das System, gibt fachliche Impulse, stößt den Sync-Prozess an. | CLI, Cursor/Claude Code, Git |
| **Der Co-Writer (Claude / Cursor)** | Schreibt neue Dokumente, strukturiert bestehende Doku um, pflegt Metadaten. | Editor, Markdown-Dateien |
| **Der Validator** (`KnowHowToAI.Cli validate`) | Agiert als digitaler Türsteher. Garantiert, dass die KI beim Schreiben keine strukturellen Fehler macht. | Console App |
| **Der MCP-Server** (`KnowHowToAI.Cli server`) | Übersetzt die strukturierten SQL-Daten in mundgerechte Häppchen für das LLM während der Arbeit. | Cursor / Claude Desktop / Claude Code |

Jede dieser Rollen kann in einem anderen Projekt/Repo als diesem hier eingesetzt werden — das Tool selbst ist projektunabhängig (siehe Grundsatzentscheidung 1 in [00-Overview.md](00-Overview.md)).

---

## 3. Der tägliche Workflow (Der "Doku-Loop")

```
┌────────────────────────────────────────────────────────┐
│               1. EXPLORIEREN & NUTZEN                   │
│  LLM liest strukturiert aus SQL Server via MCP          │
└───────────────────────────┬────────────────────────────┘
                            ▼
┌────────────────────────────────────────────────────────┐
│               2. EXPORTIEREN & BEARBEITEN               │
│  DB wird in lokale MD-Dateien exportiert (Marker-Check).│
│  Entwickler & Claude schreiben/refactoren Doku.         │
└───────────────────────────┬────────────────────────────┘
                            ▼
┌────────────────────────────────────────────────────────┐
│                  3. VALIDIEREN (Gate)                   │
│  CLI-Validator prüft YAML-Front-Matter, Slugs & Pfade.  │
│  Bei Fehlern: Claude repariert die Dateien.             │
└───────────────────────────┬────────────────────────────┘
                            ▼
┌────────────────────────────────────────────────────────┐
│                   4. IMPORTIEREN (Sync)                 │
│  DbUp bringt Schema auf den neuesten Stand, danach wird │
│  die Tabelle gewiped und aus validen MDs neu befüllt.   │
└────────────────────────────────────────────────────────┘
```

### Phase 1: Die Doku im Alltag nutzen (Lese-Modus)

Du arbeitest in Cursor/Claude Code. Du fragst das LLM: *"Wie war nochmal das Routing für den Core-Switch im IT-Bereich eingerichtet?"*

1. Das LLM nutzt `search_docs(query="routing core-switch")`.
2. SQL Server Full-Text Search liefert den Treffer `it/netzwerk/routing` (gerankt nach Relevanz).
3. Das LLM nutzt `get_doc(slug="it/netzwerk/routing")`, um gezielt *nur* diesen Inhalt zu laden.
4. **Ergebnis:** Minimale Token-Verschwendung, maximale Präzision.

### Phase 2: Doku erweitern oder umstrukturieren (Schreib-Modus)

Du beauftragst Claude: *"Erstelle mir eine neue Doku für unsere DNS-Konfiguration im IT-Bereich."*

1. Claude exportiert bei Bedarf den aktuellen Stand ins lokale Zielverzeichnis (`KnowHowToAI.Cli export`).
2. Claude analysiert die Ordnerstruktur und sieht `it/netzwerk/routing.md`.
3. Claude entscheidet autonom, eine neue Datei unter `it/netzwerk/dns.md` anzulegen — mit einem **strikt regelkonformen Slug** (nur `a-z`, `0-9`, `-`).
4. Claude schreibt den Text und befüllt das YAML Front Matter (Title, Tags, Synonyme) sauber im Header der Datei. Title/Tags/Synonyme dürfen normales Deutsch inkl. Umlauten enthalten — nur der Dateipfad (Slug) unterliegt der strikten Regel.

### Phase 3: Die Qualitätskontrolle (Validierung)

```bash
KnowHowToAI.Cli validate --config ./knowhowtoai.appsettings.json
```

* **Szenario A (Fehler):** Claude hat im YAML-Header ein Komma vergessen, einen ungültigen Slug (`Änderung.md`) verwendet oder den Parent-Ordner falsch benannt (Orphan). Der Validator bricht ab und listet alle Fehler mit Datei + Grund. Du sagst Claude: *"Behebe die Validierungsfehler in `it/netzwerk/dns.md`."*
* **Szenario B (Erfolg):** Der Validator meldet `Validation successful. 0 errors found.`

### Phase 4: Synchronisation (Wipe and Dump)

```bash
KnowHowToAI.Cli import --config ./knowhowtoai.appsettings.json
```

1. DbUp führt ausstehende SQL-Skripte aus `sql-scripts/` aus (Schema ist danach garantiert aktuell).
2. Import triggert intern `validate`. Bei Fehlern: Abbruch, keine DB-Änderung.
3. In **einer Transaktion**: `DELETE FROM documents` + Neubefüllung aus allen validierten `.md`-Dateien.
4. Der Full-Text-Index aktualisiert sich automatisch (SQL Server Change Tracking, `AUTO`-Population).

Ab sofort steht das neue Wissen dem MCP-Server (und damit allen LLM-Sitzungen) zur Verfügung.

---

## 4. Warum dieses Konzept trägt

> **Fehlertolerant beim Schreiben.** Selbst wenn eine KI beim Generieren von Markdown-Dateien "halluziniert" und Pfade falsch anlegt, wird dies *niemals* die produktive Lese-Datenbank korrumpieren. Der Validator blockiert fehlerhafte Strukturen sofort im lokalen Dateisystem, bevor sie die DB erreichen.

> **Git-freundlich beim Schreiben.** Alle Inhalte liegen als Plain-Text-Markdown vor. Du kannst sie in Git committen, Diffs vergleichen und bei Fehlern auf einen älteren Stand zurückgehen.

> **Robust beim Lesen.** SQL Server liefert transaktionssichere, gerankte Volltextsuche und skaliert deutlich über das hinaus, was eine LIKE-Abfrage auf einer Datei-DB leisten könnte.

> **Sicher beim Export.** Der Export in ein Zielverzeichnis erfordert eine vom Tool selbst erzeugte Marker-Datei, bevor gewiped wird (siehe [04-Datenmodell-Validierung-Edgecases.md](04-Datenmodell-Validierung-Edgecases.md#export-marker-datei)). Ein versehentliches Leeren eines falschen/fremden Ordners ist damit ausgeschlossen.
