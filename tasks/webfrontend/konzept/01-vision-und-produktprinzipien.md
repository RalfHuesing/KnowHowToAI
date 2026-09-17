# Vision und Produktprinzipien

## Vision

KnowHowTo AI wird von einem reinen MCP-Server zu einer Wissensplattform für Menschen und Agenten.

- Die Datenbank ist der zentrale, versionierte **Wissensbunker**.
- Menschen pflegen, strukturieren, prüfen und publizieren Wissen über ein Webfrontend.
- Programmieragenten in Cursor, Claude, Hermes oder anderen MCP-fähigen Clients verwenden denselben Wissensstand.
- Agenten lesen bei ausreichendem Wissensstand primär gezielt; unkontrollierte Neuerzeugung und Duplikate werden vermieden.
- Wissen bleibt ohne Agent vollständig editierbar.
- Integrierte KI-Orchestrierung, z. B. Semantic Kernel, ist eine spätere Option und keine Voraussetzung des Frontends.

## Problem und Nutzen

Der MCP-Server bietet sichere fachliche Primitive, aber keine menschlich erfassbare Gesamtsicht:

- Struktur, Umfang und Zustand der Wissensbasis sind schwer überschaubar.
- Manuelle Nachbearbeitung ist unnötig indirekt.
- Transactions, Releases, Snapshots, Rollenauflösung, Stale-Zustände und Findings benötigen visuelle Arbeitsoberflächen.
- Verschieben und Sortieren von Nodes ist visuell einfacher und sicherer.
- Zielgruppengerechte Dokumente benötigen einen reproduzierbaren Publikationsprozess.

Das Frontend ist die vollumfängliche menschliche Arbeits- und Publikationsoberfläche der KnowHowTo-AI-Datenbank, keine reine Administration.

## Produktprinzipien

1. **Eine fachliche Wahrheit:** Keine Geschäftslogik in Browser, MCP-Handler oder HTTP-Endpunkten duplizieren.
2. **Menschen und Agenten als gleichwertige Clients:** Beide verwenden dieselben Regeln, Transactions, Validatoren und Identitäten.
3. **Expliziter Arbeitsstand:** Current Snapshot, historischer Snapshot oder Working Transaction sind jederzeit sichtbar.
4. **Sicheres Editieren:** Kein stilles Schreiben in den Current Snapshot; jeder Edit gehört zu einer sichtbaren Transaction.
5. **Reproduzierbare Publikation:** Offizielle Exporte referenzieren committed Snapshot oder Release sowie eine versionierte Publikationsdefinition.
6. **Progressive Disclosure:** Erst Struktur und Metadaten, Content nur bei Bedarf.
7. **Intranet-first:** Regulärer Betrieb zentral im Firmennetz; lokale Entwicklung und Tests bleiben möglich.
8. **KI optional:** Alle Kernworkflows funktionieren deterministisch ohne LLM.
9. **Standardkomponenten vor Eigenbau:** Auswahl nach Eignung, Wartung, Lizenz, Barrierefreiheit und Integrationskosten.
10. **Explizite Integrationsgrenzen:** Browser, n8n und MCP-Clients verwenden dokumentierte Adapter desselben Hosts.

## Bestehende Kernleitplanken

Das Frontend erhält die implementierten Invarianten:

- Globale Node-Hierarchie mit stabilen `NodeId`s.
- Rollenabhängiger Content bei gemeinsamer Hierarchie.
- Node-Titel bilden die Dokumentstruktur; `ContentMd` enthält keine Überschriften.
- Writes erfolgen ausschließlich in KnowHowTo-AI-Transactions.
- Working Snapshots werden validiert, committed oder verworfen.
- Committed Snapshots sind unveränderlich; konkurrierende Commits erzeugen `SnapshotConflict`.
- Releases sind unveränderliche Verweise auf committed Snapshots.
- Rollen-Fallback, Provenienz und transitive Stale-Erkennung bleiben transparent.
- Reads bleiben metadata-first und paginiert.
- Fachlogik verbleibt in Domain und Application; UI, REST und MCP sind Adapter.
- Content-Rollen sind Zielgruppenrollen, keine Benutzerrechte.

Verbindlicher Ist-Stand: [`docs/`](../../../docs/README.md).

## Zielgruppen

| Akteur | Hauptaufgaben |
|---|---|
| Wissensautor | Navigieren, suchen, Nodes strukturieren, Content bearbeiten, Hinweise erfassen |
| Consultant | Fachwissen und Kundenanpassungen pflegen, Rollen-Content ableiten, Publikationen vorbereiten |
| Entwickler | Technisches Wissen lesen und ergänzen, historische Stände und Diffs prüfen |
| Redakteur | Stale Content, TODOs und Qualitätswarnungen bearbeiten |
| Endkunde | Erhält freigegebene, rollenbezogene Publikationen; kein direkter Erstzugriff |
| Externer Agent | Liest und schreibt über MCP innerhalb expliziter Transactions |
| n8n/Integration | Verwendet stabile REST-Endpunkte für automatisierte Workflows |
| Späterer integrierter Agent | Bearbeitet ausgewählte redaktionelle Aufträge aus der UI |

## Erfolgskriterien des ersten nutzbaren Frontends

- Struktur, Rollen, offene Transactions und Releases sind ohne MCP-Client verständlich.
- Nodes und Rollen-Content lassen sich transaktional anlegen, bearbeiten, verschieben, validieren und committen.
- Fallback, Freshness, Findings und Arbeitsstand sind jederzeit sichtbar.
- UI-, REST- und MCP-Reads liefern denselben fachlichen Zustand.
- Durch UI oder Agent committed Änderungen erscheinen ohne Synchronisationsschritt beim jeweils anderen Client.
- Kein Webworkflow umgeht Domainregeln oder schreibt direkt in committed Snapshots.
- Das Frontend bleibt ohne integriertes LLM vollständig verwendbar.
- n8n kann dokumentierte REST-Endpunkte verwenden.
- MCP-Clients greifen zentral über `/mcp` auf dieselben Tools zu.
