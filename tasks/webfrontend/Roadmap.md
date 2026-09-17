# Roadmap: Webfrontend und Wissensplattform

Stand: 2026-09-17

Status: Planungsartefakt; `docs/` beschreibt ausschließlich den implementierten Ist-Zustand.

## Ausführungsmodell

Die Roadmap ist in Milestones, Arbeitspakete und Agent-Tasks gegliedert:

```text
Milestone Mx
  → Arbeitspaket Mx.y
      → ausführbarer Agent-Task Mx.y-Tz
```

- Nur eine Checkbox mit `-T` in der ID ist ein eigenständig ausführbarer Task.
- Milestone- und Arbeitspaket-Checkboxen sind Fortschrittsaggregate; sie werden nicht als Ganzes an einen Agenten gegeben.
- Ein Agent bearbeitet grundsätzlich genau einen Leaf-Task und dessen explizite Abnahmepunkte.
- Jeder Leaf-Task ist so geschnitten, dass Analyse, Implementierung, Tests, Ist-Dokumentation und Commit in ein Kontextfenster von 256–512k Tokens passen.
- Wird beim Start erkennbar, dass ein Leaf-Task diese Grenze überschreitet, wird er vor der Implementierung in weitere sequenzielle Leaf-Tasks geteilt.
- Ein Task übernimmt keine unerwähnten Nachbarfeatures. Notwendige Folgethemen werden als neue Tasks dokumentiert.

## Verbindliche Arbeitsregeln

- Milestones und Tasks werden in dokumentierter Reihenfolge umgesetzt; Abweichungen benötigen eine festgehaltene Begründung.
- Vor jedem Task gelten `AGENTS.md`, die Projektregeln und die Lese-Matrix in [`docs/`](../../docs/README.md).
- Der Agent liest die im Milestone und Task verlinkten Konzept- und Ist-Dokumente vollständig im relevanten Umfang.
- Ein Leaf-Task endet mit funktionsfähigem Code, risikogerechten Tests, aktualisierter `docs/`-Ist-Dokumentation, aktualisierten Roadmap-Checkboxen und einem atomaren Commit.
- Parent-Checkboxen werden erst gesetzt, wenn alle direkten Kinder und das jeweilige Abnahmekriterium erfüllt sind.
- Ein Spike endet mit einer dokumentierten Entscheidung; wegwerfbarer Spike-Code gelangt nicht ungeprüft in Produktion.
- Authentifizierung und Autorisierung gehören ausdrücklich nicht zu dieser Roadmap.
- Allgemeine REST-/OpenAPI-Endpunkte werden nicht vorsorglich implementiert.

## Milestones

Der Status wird ausschließlich in der jeweiligen Milestone-Datei gepflegt.

| Milestone | Ziel | Abhängigkeit |
|---|---|---|
| [M0 – Komponenten- und Architekturentscheidungen](roadmap/00-komponenten-und-architektur.md) | Risiken vor Produktivcode entscheiden | – |
| [M1 – Gemeinsamer Webhost und MCP HTTP](roadmap/01-webhost-und-mcp-http.md) | Eine EXE, ein Port, Blazor und HTTP-MCP | M0 |
| [M2 – Designsystem und Anwendungsshell](roadmap/02-designsystem-und-shell.md) | belastbare moderne UI-Grundlage | M1 |
| [M3 – Read-only Wissenscockpit](roadmap/03-read-only-wissenscockpit.md) | vollständige menschliche Lesesicht | M2 |
| [M4 – Transactions und Strukturpflege](roadmap/04-transactions-und-strukturpflege.md) | sichere visuelle Strukturänderungen | M3 |
| [M5 – Rollen-Content und Rich Text](roadmap/05-rollen-content-und-rich-text.md) | vollständige Contentpflege ohne Agent | M4 |
| [M6 – Bilder und Assetverwaltung](roadmap/06-bilder-und-assets.md) | stabile Bilder im gesamten Lebenszyklus | M5 |
| [M7 – Betriebs- und Qualitätshärtung](roadmap/07-betrieb-und-qualitaet.md) | belastbarer Intranetbetrieb | M1–M6 |
| [M8 – Einfacher PDF-Teilbaumexport](roadmap/08-pdf-export.md) | niedrig priorisierter PDF-Download | M6, M7 |

## Separate spätere Vorhaben

Nicht als Tasks dieser Roadmap ausführen:

- Authentifizierung, Autorisierung, ACL, Audit und Mandantenmodell.
- Direkter Endkundenzugang.
- Integrierte Agentenorchestrierung und Semantic Kernel.
- Semantic Search und Embeddings.
- Kollaboratives Live-Editing oder automatisches Merge/Rebase.
- Presentation Views mit alternativen Navigationsstrukturen.
- Allgemeine REST-/OpenAPI-Integrations-API für n8n oder andere Systeme; erst bei einem konkreten Automationsfall.
