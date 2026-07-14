# KnowHowToAI — Projektübersicht

> **Elevator Pitch:** Ein generisches, wiederverwendbares .NET-Tool, das eine hierarchische Markdown-Wissensdatenbank (Front Matter + Ordnerstruktur) in einen MS-SQL-Server-Cache synchronisiert und diesen Cache per MCP (Model Context Protocol) für LLMs (Claude, Cursor, ...) durchsuchbar macht. Geschrieben wird in Markdown-Dateien im Filesystem, gelesen wird strukturiert per SQL über drei schlanke MCP-Tools.

Dieses Verzeichnis (`docs/`) beschreibt **das Tool selbst**, nicht eine konkrete Wissensdatenbank. Die eigentlichen `.md`-Inhalte, die später durchsucht werden, leben in einem beliebigen, frei konfigurierbaren Zielverzeichnis außerhalb dieses Repos (siehe [03-Projektstruktur-und-Konfiguration.md](03-Projektstruktur-und-Konfiguration.md)).

## Dokumenten-Karte

| Dokument | Inhalt |
| --- | --- |
| [01-Konzept-und-Workflow.md](01-Konzept-und-Workflow.md) | Intention, Akteure, der tägliche Doku-Loop (Explorieren → Exportieren → Validieren → Importieren) |
| [02-Architektur-und-Techstack.md](02-Architektur-und-Techstack.md) | Tech-Stack, Code-Guidelines, MCP-Tools, SQL-Schema-Überblick |
| [03-Projektstruktur-und-Konfiguration.md](03-Projektstruktur-und-Konfiguration.md) | Solution-/Projekt-Layout, Namespaces, `appsettings.json`, CLI-Kommandos |
| [04-Datenmodell-Validierung-Edgecases.md](04-Datenmodell-Validierung-Edgecases.md) | SQL-Skripte, Validierungsregeln, Slug-Regeln, Edge Cases & wie sie behandelt werden |
| [05-Roadmap.md](05-Roadmap.md) | MVP-Scope (v1), Implementierungs-Reihenfolge für einen frischen Chat, Backlog (v2+) |

## Grundsatzentscheidungen (Kurzfassung)

Diese Entscheidungen wurden bewusst getroffen und sollten in der Umsetzung **nicht** in Frage gestellt werden, ohne den Nutzer zu fragen:

1. **Generisches Tool, kein gebundener Inhalt.** Docs-Root-Pfad und SQL-Connection-String sind pro Einsatzort frei konfigurierbar (`appsettings.json`). Dieses Repo enthält nur die Software.
2. **MS SQL Server statt SQLite.** Läuft lokal oder im Netzwerk, keine "Offline-first"-Anforderung. Alles Nötige steht in `appsettings.json`.
3. **Schema-Verwaltung via nummerierte SQL-Skripte + DbUp.** Kein EF Core, kein ORM-Migrations-Ballast.
4. **Volltextsuche via SQL Server Full-Text Search** (`CONTAINS`/`FREETEXT`), kein LIKE, kein Vector-RAG.
5. **Strikte Slug-Regeln:** nur `a-z`, `0-9`, `-`, `/` in Dateipfaden — vermeidet Case-Collisions zwischen Windows/Linux/Git vollständig.
6. **3-Projekt-Solution:** `KnowHowToAI.Core` (Logik), `KnowHowToAI.Cli` (Entry Point, CLI + MCP-Hosting), `KnowHowToAI.Core.Tests` (xUnit v3).
7. **Sync nur manuell in v1** (kein Watch-Modus). `export` schreibt eine Marker-Datei und wiped nur, wenn diese vorhanden ist — sonst Abbruch mit Fehler (Schutz vor versehentlichem Datenverlust in Fremd-Verzeichnissen).
8. **Distribution in v1:** reines Build-Artefakt, self-contained Single-File-`.exe` via `scripts/publish.ps1` (siehe [03, Abschnitt 5](03-Projektstruktur-und-Konfiguration.md#5-deployment-single-file-publish)), MCP-Config verweist auf den Pfad zur `.exe`. Packaging als globales .NET-Tool ist Backlog.
9. **Offizielles `ModelContextProtocol`-NuGet-Paket** für den stdio-Server, **`System.CommandLine`** für die CLI.

Alle Details und Begründungen ("Warum?") stehen in den verlinkten Dokumenten.
