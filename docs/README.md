# KnowHowToAI – Dokumentation

Diese Dokumentation beschreibt den Ist-Zustand von KnowHowToAI. Sie ist die
verbindliche fachliche und technische Quelle für implementiertes Verhalten.
Konzepte und Roadmaps unter `tasks/` beschreiben das geplante Soll-Delta und
ersetzen diese Ist-Dokumentation nicht. Das Planungs- und Ausführungspattern
steht in [tasks/README.md](../tasks/README.md); für den Web-Editor-Neustart sind
[Konzept](../tasks/web-editor-neustart/Konzept.md) und
[Roadmap](../tasks/web-editor-neustart/roadmap.md) dessen aktuelle Projektartefakte.

## Verbindlichkeit und Vorrang

- Die Dokumente unter `docs/` sind bindend. Bei einer Abweichung zwischen
  Dokumentation und Code wird nicht weiterimplementiert, bis die Abweichung geklärt
  und an allen betroffenen Stellen beseitigt ist.
- Jede normative Aussage steht an genau einer Stelle. Dokumente verlinken
  aufeinander, statt Inhalt zu kopieren.
- Verbindliche Regeln und harte Invarianten sind zentral gesammelt in
  [Invarianten](Invarianten.md). Fehler- und Warncodes sind ein öffentlicher
  Vertrag und liegen im [MCP-API-Katalog](McpApi.md).
- Änderungen an Verhalten, Verträgen, Datenmodell oder Konfiguration werden im
  selben Commit wie der Code in das zuständige Dokument eingearbeitet
  (Regel: [.agents/rules/DokuRichtlinien.mdc](../.agents/rules/DokuRichtlinien.mdc)).

## Dokumente

| Dokument | Inhalt |
|---|---|
| [Intention](Intention.md) | Zweck, Ziele, Arbeitsabläufe, Aufgabenverteilung Agent/Server, Drift-Prinzip |
| [Wissenshierarchie](Wissenshierarchie.md) | Nodes, globale Hierarchie, Titel-/Content-Trennung, Heading-Verbot, Markdown, Refactoring |
| [Zielgruppen und Content](Zielgruppen-und-Content.md) | Zielgruppenmodell, Resolution Orders, Content-Revisions, Dependencies, Freshness, Drift |
| [Transaktionen und Historie](Transaktionen-und-Historie.md) | Transactions, Working Snapshots, Commit/Discard, Konkurrenz, Releases, Diffs |
| [Retrieval](Retrieval.md) | Navigation, Export, Search, Content-Schreiboperationen, Validatoren, Warnungen |
| [MCP-API](McpApi.md) | MCP-Tools, Anfrage-/Antwortverträge, Envelope, Fehler-/Warncode-Katalog |
| [Datenmodell](Datenmodell.md) | SQL-Tabellen, Snapshot-Schlüssel, Transaction-Metadaten, Migrationen |
| [Architektur](Architektur.md) | Stack, Schichten, Transportgrenzen, Projektstruktur, Deployment |
| [Konfiguration und Betrieb](Konfiguration-und-Betrieb.md) | Konfigurationsschlüssel, Protokollierung, Build, Tests, Linter |
| [Manuelle UI-Abnahme](Manuelle-UI-Abnahme.md) | manuelle Checkliste für Browserzoom, Reflow und Tastaturnavigation der Anwendungsshell |
| [Invarianten](Invarianten.md) | verbindliche Regeln des Gesamtsystems |
| [Entscheidungen](Entscheidungen.md) | Architekturentscheidungen, bewusste V1-Grenzen, spätere Erweiterungen |

## Lese-Matrix nach Aufgabe

Vor jeder fachlichen oder technischen Änderung sind diese Datei (der Einstieg)
sowie [Invarianten](Invarianten.md) zu lesen, dazu das fachlich betroffene Dokument:

| Aufgabe | Zusätzlich lesen |
|---|---|
| Nodes, Hierarchie, Markdown, Wissens-Refactoring | [Wissenshierarchie](Wissenshierarchie.md), [Retrieval](Retrieval.md) |
| Zielgruppen, Fallback, Revisions, Dependencies, Freshness | [Zielgruppen und Content](Zielgruppen-und-Content.md) |
| Transactions, Snapshots, Concurrency, Historie, Releases | [Transaktionen und Historie](Transaktionen-und-Historie.md), [Datenmodell](Datenmodell.md) |
| Export, Navigation, Search, Schreiben, Validatoren | [Retrieval](Retrieval.md) |
| MCP-Tools, Schemas, Fehlercodes | [MCP-API](McpApi.md) sowie die vom Tool berührten Fachdokumente |
| SQL-Schema, Repositories, Migrationen | [Datenmodell](Datenmodell.md), [Architektur](Architektur.md) |
| Projektsetup, Konfiguration, Transport, Betrieb | [Architektur](Architektur.md), [Konfiguration und Betrieb](Konfiguration-und-Betrieb.md) |
| Begründungen, V1-Grenzen, Erweiterungsplanung | [Entscheidungen](Entscheidungen.md) |

Ein Agent darf sich nicht allein auf Suchtreffer oder einzeln extrahierte Absätze
stützen. Zusammengehörige Abschnitte des betroffenen Dokuments sind vollständig zu
lesen.

## Pflege

- Neue Aussagen werden dem fachlich zuständigen Dokument zugeordnet. Ein neues
  Dokument entsteht nur bei einer klar neuen Verantwortungsgrenze.
- Querverweise sind relative Markdown-Links, keine kopierten Regeltexte.
- Beispiele erläutern Regeln und schaffen keine widersprechenden Sonderfälle.
- Code enthält keine Referenzen auf Dokumentpfade; die Doku ist über `docs/`
  auffindbar.
