# KnowHowTo AI

Hierarchische, versionierte und rollenabhängige Wissensbasis für Agenten und
Menschen. Der Zugriff erfolgt ausschließlich über MCP (Model Context Protocol);
V1 transportiert über STDIO.

## Was ist das?

KnowHowTo AI speichert fachliches und technisches Wissen nicht als Markdown-Dateien,
sondern strukturiert in einem MS SQL Server: eine globale Node-Hierarchie, pro Rolle
eigene oder per Fallback aufgelöste Inhalte, vollständige unveränderliche Snapshots
pro Transaktion und benannte Releases. Agenten arbeiten mit kleinen,
deterministischen Tools (`begin_transaction`, `create_node`, `replace_content`,
`search`, `export_tree`, …) und lesen metadata-first statt ganze Bäume zu laden.

Abgeleitete Inhalte (z. B. Endanwender-Dokumentation auf Basis von Consultant- und
Developer-Wissen) speichern ihre Quellrevisionen; das System erkennt veraltete
Ableitungen (Stale), ohne automatisch zu synchronisieren.

## Kernfunktionen

- Globale Wissenshierarchie mit stabilen Node-IDs, ein Root pro Snapshot
- Frei definierbare Rollen mit deterministischen, nicht-rekursiven Resolution
  Orders und transparentem Fallback
- Vollständige, unveränderliche Snapshots: jede Änderung läuft über eine
  Transaction mit eigenem Working Snapshot; konkurrierende Commits scheitern
  deterministisch statt zu mergen
- Content-Revisions und Content-Abhängigkeiten mit transitiver Stale-Erkennung
- Gespeicherter Markdown-Content ohne Überschriften; die Dokumentstruktur entsteht
  aus der Hierarchie (Heading-freier Export)
- Rollenbezogener Markdown-Export, exakte parametrisierte Textsuche mit
  deterministischem Ranking, seitenweises Paging über opake Cursors
- Strukturierte, maschinenlesbare Tool-Antworten mit stabilen Fehler- und
  Warncodes
- Benannte Releases als unveränderliche Verweise auf committed Snapshots;
  Netto-Diffs zwischen Snapshots und Transactions

## Technischer Stack

- C# / .NET, MS SQL Server 2019 oder neuer
- MCP-Server über STDIO (ModelContextProtocol-SDK)
- Dapper für den SQL-Zugriff, Markdig für Markdown-Validierung, Serilog für
  Protokollierung nach stderr

## Voraussetzungen

- MS SQL Server-Datenbank, manuell bereitgestellt (Schema-Migrationen werden beim
  Serverstart automatisch angewendet); Verbindung steht in der
  `DatabaseConnection`-Sektion von `src/KnowHowToAI.Server/appsettings.json`
- .NET SDK (Ziel-Framework `net10.0`)

## Bauen und Testen

```text
dotnet build KnowHowToAI.slnx -v q --nologo
pwsh -NoProfile -File scripts/test-fast.ps1
pwsh -NoProfile -File scripts/test-integration.ps1
```

Integrationstests mit echtem SQL Server laufen gegen die konfigurierte Datenbank
(Kategorie `ManualDatabaseIntegration`); der Test-Harness erzeugt oder entfernt
keine Datenbanken.

## Dokumentation

Die verbindliche technische Dokumentation (Ist-Stand) liegt unter
[docs/README.md](docs/README.md) mit einer Lese-Matrix je Aufgabe. Die
Entwicklungsgeschichte ist der Git-Historie zu entnehmen.
