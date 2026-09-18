# Vision und Produktprinzipien

## Vision

KnowHowToAI wird von einem reinen MCP-Server zu einer Wissensplattform für Menschen und Agenten.

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
- Zielgruppengerechte Dokumente benötigen einen einfachen PDF-Export ab einem gewählten Node.

Das Frontend ist die vollumfängliche menschliche Arbeits- und Publikationsoberfläche der KnowHowTo-AI-Datenbank, keine reine Administration.

## Produktprinzipien

1. **Eine fachliche Wahrheit:** Keine Geschäftslogik in Browser, MCP-Handler oder HTTP-Endpunkten duplizieren.
2. **Menschen und Agenten als gleichwertige Clients:** Beide verwenden dieselben Regeln, Transactions, Validatoren und Identitäten.
3. **Expliziter Arbeitsstand:** Current Snapshot, historischer Snapshot oder Working Transaction sind jederzeit sichtbar.
4. **Sicheres Editieren:** Kein stilles Schreiben in den Current Snapshot; jeder Edit gehört zu einer sichtbaren Transaction.
5. **Einfacher Teilbaumexport:** PDF exportiert den gewählten Node und alle Nachfahren mit genau einem serverseitigen Template.
6. **Progressive Disclosure:** Erst Struktur und Metadaten, Content nur bei Bedarf.
7. **Intranet-first:** Regulärer Betrieb zentral im Firmennetz; lokale Entwicklung und Tests bleiben möglich.
8. **KI optional:** Alle Kernworkflows funktionieren deterministisch ohne LLM.
9. **M0-Auswahl verbindlich nutzen:** Allgemeine UI nativ mit Blazor/HTML/CSS, Knowledge Tree nativ und paginiert, Rich Text mit Milkdown `@milkdown/crepe` `7.22.1`; keine erneute Variantensuche in Folge-Milestones.
10. **Bedarf vor Vorratsbau:** Der erste Schritt implementiert Weboberfläche und HTTP-MCP; weitere Integrationsadapter entstehen erst bei einem konkreten Anwendungsfall.

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
- Fachlogik verbleibt in Domain und Application; UI und MCP sind Adapter. Spätere REST- oder Automationsadapter verwenden dieselben Application Services.
- Content-Rollen sind Zielgruppenrollen, keine Benutzerrechte.

Verbindlicher Ist-Stand: [`docs/`](../../../docs/README.md).

## Zielgruppen

| Akteur | Hauptaufgaben |
|---|---|
| Wissensautor | Navigieren, suchen, Nodes strukturieren und freien Content bearbeiten |
| Consultant | Fachwissen und Kundenanpassungen pflegen, Rollen-Content ableiten, Publikationen vorbereiten |
| Entwickler | Technisches Wissen lesen und ergänzen, historische Stände und Diffs prüfen |
| Redakteur | Per normaler Suche gefundene TODO-Texte, stale Content und Qualitätswarnungen bearbeiten |
| Endkunde | Erhält rollenbezogene PDF-Ausgaben; kein direkter Erstzugriff |
| Externer Agent | Liest und schreibt über MCP innerhalb expliziter Transactions |
| Spätere n8n-/Systemintegration | Kann bei bestätigtem Bedarf einen eigenen REST-/OpenAPI-Adapter erhalten |
| Späterer integrierter Agent | Bearbeitet explizite Such- und Überarbeitungsaufträge aus der UI |

## Erfolgskriterien des ersten nutzbaren Frontends

- Struktur, Rollen, offene Transactions und Releases sind ohne MCP-Client verständlich.
- Nodes und Rollen-Content lassen sich transaktional anlegen, bearbeiten, verschieben, validieren und committen.
- Fallback, Freshness, Findings und Arbeitsstand sind jederzeit sichtbar.
- UI- und MCP-Reads liefern denselben fachlichen Zustand.
- Durch UI oder Agent committed Änderungen erscheinen ohne Synchronisationsschritt beim jeweils anderen Client.
- Kein Webworkflow umgeht Domainregeln oder schreibt direkt in committed Snapshots.
- Das Frontend bleibt ohne integriertes LLM vollständig verwendbar.
- MCP-Clients greifen zentral über `/mcp` auf dieselben Tools zu.
- Die Application-Grenzen erlauben später REST-/OpenAPI-Endpunkte, ohne Fachlogik aus UI oder MCP zu kopieren.
