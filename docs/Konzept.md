# KnowHowTo AI – Konzeptindex

Dieses Dokument ist der verbindliche Einstieg in das Fachkonzept von KnowHowTo AI.
Das vollständige Konzept besteht aus diesem Index und allen unten verlinkten Modulen
unter `docs/konzept/`. Die Aufteilung ändert weder Inhalt noch Verbindlichkeit der
bisherigen Architekturgrundlage.

## Verbindlichkeit und Vorrang

- Dieser Index und die verlinkten Konzeptmodule bilden gemeinsam die einzige
  fachliche und architektonische Quelle für den Greenfield-Stand.
- `docs/Roadmap.md` konkretisiert Reihenfolge, Status und Abnahme der Implementierung,
  ersetzt aber keine Konzeptentscheidung.
- Bei einem Widerspruch zwischen Konzeptmodulen wird nicht implementiert, bis der
  Widerspruch bewusst geklärt und an allen betroffenen Stellen korrigiert wurde.
- Informationen werden nicht zwischen Modulen kopiert. Die normative Aussage bleibt
  an genau einer Stelle; andere Dokumente verlinken darauf.
- Änderungen an einer Invariante, einem öffentlichen Vertrag oder dem Datenmodell
  müssen Konzept, Roadmap und Agentenregeln gemeinsam aktualisieren.

## Pflichtlektüre für jede Implementierungsaufgabe

Vor jeder fachlichen oder technischen Implementierung sind mindestens zu lesen:

1. dieser Index,
2. [V1-Scope, Invarianten und Zielarchitektur](konzept/06-Datenmodell-V1-Invarianten-Architektur.md),
3. die laut folgender Matrix betroffenen Fachmodule,
4. der aktuelle Meilenstein in `docs/Roadmap.md`.

Ein Agent darf sich nicht allein auf Suchtreffer oder einzelne extrahierte Absätze
stützen. Zusammengehörige Abschnitte des betroffenen Moduls sind vollständig zu lesen.

## Konzeptmodule

| Modul | Verbindlicher Inhalt | Bisherige Abschnitte |
|---|---|---:|
| [Grundlagen, Hierarchie und Markdown](konzept/01-Grundlagen-Hierarchie-Markdown.md) | Zielbild, Stack, Konfiguration, Systemgrenzen, Nodes, globale Hierarchie, Markdown und Refactoring | 1–16 |
| [Rollen, Provenienz und Drift](konzept/02-Rollen-Provenienz-Drift.md) | Rollenmodell, Fallback, Content-Revisions, Dependencies, Freshness und Synchronisationsflow | 17–30 |
| [Transaktionen, Snapshots und Releases](konzept/03-Transaktionen-Snapshots-Releases.md) | Working Snapshots, Commit/Discard, Konkurrenz, Löschung, Releases und historische Reproduzierbarkeit | 31–46 |
| [Export, Retrieval und Validierung](konzept/04-Export-Retrieval-Validierung.md) | Export, Navigation, Search, Content-Mutationen, Normalisierung, Validatoren und Warnungen | 47–60 |
| [MCP-API und Tool-Verträge](konzept/05-MCP-API.md) | V1-Tools, Transaktionspflicht, explizite Rollen und strukturierte Antworten | 61–64 |
| [Datenmodell, V1-Scope, Invarianten und Architektur](konzept/06-Datenmodell-V1-Invarianten-Architektur.md) | Relationales Modell, Historie, bewusste V1-Grenzen, Erweiterbarkeit, Invarianten und Zielarchitektur | 65–81 |
| [Referenzabläufe und Architekturentscheidungen](konzept/07-Referenzablaeufe-Entscheidungen.md) | vollständige Beispiele, Entscheidungsbegründungen und Implementierungsreihenfolge | 82–89 |

## Lese-Matrix nach Aufgabe

| Aufgabe | Zusätzlich zu diesem Index und Modul 06 lesen |
|---|---|
| Projektsetup, Konfiguration, Transportgrenzen | Modul 01 |
| Nodes, Hierarchie, Markdown oder Wissens-Refactoring | Module 01 und 04 |
| Rollen, Fallback, Content-Revisions, Dependencies oder Freshness | Modul 02 |
| Transactions, Snapshots, Concurrency, Löschung, Historie oder Releases | Modul 03 |
| Export, Navigation, Search, Patching oder Validatoren | Modul 04 |
| MCP-Tools, Schemas, Fehlerantworten oder STDIO-Adapter | Modul 05 sowie alle vom Tool berührten Fachmodule |
| SQL-Schema, Repositories oder Migrationen | Module 03 und 06 |
| End-to-End-Workflow oder fachliche Abnahme | Modul 07 sowie alle vom Workflow berührten Fachmodule |

## Pflege dieses Konzepts

- Neue Aussagen werden dem fachlich zuständigen Modul zugeordnet; neue Module werden
  nur bei einer klar neuen Verantwortungsgrenze angelegt.
- Abschnittsnummern bleiben stabil. Neue Abschnitte werden in das passende Modul
  eingeordnet und Index sowie Lese-Matrix werden angepasst.
- Querverweise verwenden relative Markdown-Links, keine kopierten Regeltexte.
- Beispiele erläutern Regeln, schaffen aber keine widersprechenden Sonderfälle.
- Die Roadmap verlinkt jeden Meilenstein auf die dafür erforderlichen Module.
