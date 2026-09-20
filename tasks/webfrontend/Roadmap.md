# Roadmap: Webfrontend und Wissensplattform

Stand: 2026-09-20

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

## Rollierender Planungshorizont

- Der aktuelle Detailplanungshorizont endet mit M5. M0–M4 sind abgeschlossen; M5.0 ist am 2026-09-20 geschlossen und M5.1–M5.5 sind so entschieden und präzisiert, dass ein Agent jeden Leaf-Task deterministisch bearbeiten kann. M6–M8 bleiben bis zu ihren eigenen manuellen Gates gesperrt.
- Die beschriebenen Inhalte von M6 bis M8 halten derzeit Zielrichtung, Abhängigkeiten und bekannte Risiken fest. Sie sind bewusst noch keine Ausführungsfreigabe.
- M3 und jedes folgende Milestone beginnen mit einem Arbeitspaket `Mx.0 – Manuelle Planung und Konzeptschärfung`. Dieses Arbeitspaket wird gemeinsam mit dem Benutzer bearbeitet und nie als Implementierungs-Leaf-Task an einen Agenten delegiert.
- Im `Mx.0`-Gate werden der umgesetzte Ist-Stand des Vorgängermilestones ausgewertet, offene Produkt- und Technikentscheidungen getroffen, Konzeptdokumente fortgeschrieben und die nachfolgenden Leaf-Tasks des Milestones konkretisiert, geteilt, ersetzt oder entfernt.
- Erst wenn das jeweilige `Mx.0`-Gate abgeschlossen und committed ist, sind die nachfolgenden Leaf-Tasks dieses Milestones zur Agentenausführung freigegeben.
- Entscheidungen werden nicht vorsorglich für spätere Milestones erzwungen. Sie werden im zuständigen `Mx.0`-Gate mit dem dann bekannten Ist-Stand getroffen.

### Freigabestand für M0 bis M5

Alle Benutzer- und Spikeentscheidungen für M0 bis M5 sind geschlossen. Weitere Benutzerentscheidungen ab M6 werden ausdrücklich nicht vorgezogen.

M0 ist abgeschlossen. Die evidenzbasierten Ergebnisse O-001, O-003, O-002 und O-015 sind als K-021 bis K-025 im [Konzeptindex](README.md#gesetzte-leitentscheidungen) festgehalten und für alle Folge-Milestones verbindlich. Allgemeine UI-Basis, Tree, Editor und Testwerkzeuge werden nicht erneut gesucht oder zwischen Varianten entschieden.

Freigabestand 2026-09-20: M0–M4 sind abgeschlossen. M5.0 ist mit dem [Planungsartefakt](roadmap/05-rollen-content-und-rich-text/planning.md) geschlossen; M5.1–M5.5 sind zur sequenziellen Agentenausführung freigegeben. M6–M8 bleiben bis zum jeweiligen manuellen Gate gesperrt.

## Gesamtausführung durch einen Agenten

Ein Agent mit dem Auftrag „alles umsetzen“ arbeitet deterministisch:

1. Milestone-Dateien in numerischer Reihenfolge öffnen.
2. Darin den ersten nicht erledigten Leaf-Task in Dokumentreihenfolge wählen.
3. Abhängigkeiten, referenzierte Konzepte, [Projektstruktur](konzept/08-projektstruktur-und-codekonventionen.md) und erforderliche `docs/` vollständig lesen.
4. Prüfen, ob das Milestone innerhalb des aktuellen Planungshorizonts liegt oder sein `Mx.0`-Gate abgeschlossen ist. Andernfalls nicht implementieren und zur manuellen Planung zurückkehren.
5. Prüfen, ob eine [offene Frage](konzept/07-entscheidungen-und-offene-fragen.md) den Task blockiert. Bei Blockade nicht raten, sondern den Benutzer fragen und den Punkt zuerst dokumentarisch schließen.
6. Genau den Leaf-Task implementieren, prüfen, dokumentieren, abhaken und atomar committen.
7. Erfüllte Arbeitspaket- und Milestone-Checkboxen im selben Commit schließen.
8. Mit dem nächsten Leaf-Task fortfahren, bis die Roadmap abgeschlossen oder eine explizite Blockade erreicht ist.

Bereits vorhandenes Verhalten wird nicht blind neu implementiert. Der Agent verifiziert es gegen die Task-Abnahme, ergänzt fehlende Nachweise und markiert den Task erst danach als erledigt.

## Verbindliche Arbeitsregeln

- Milestones und Tasks werden in dokumentierter Reihenfolge umgesetzt; Abweichungen benötigen eine festgehaltene Begründung.
- Ein `Mx.0`-Arbeitspaket ist ein manuelles Konzept-Gate, kein Agent-Task. Dessen Abschluss gibt nur das unmittelbar zugehörige Milestone frei.
- Vor jedem Task gelten `AGENTS.md`, die Projektregeln und die Lese-Matrix in [`docs/`](../../docs/README.md).
- [Projektstruktur und Codekonventionen](konzept/08-projektstruktur-und-codekonventionen.md) ist für jeden Task mit Produktions-, Test- oder Projektstrukturänderung Pflichtlektüre.
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
| [M0 – Komponenten- und Architekturentscheidungen](roadmap/00-komponenten-und-architektur/roadmap.md) | Risiken vor Produktivcode entscheiden | – |
| [M1 – Gemeinsamer Webhost und MCP HTTP](roadmap/01-webhost-und-mcp-http/roadmap.md) | Eine EXE, ein Port, Blazor und HTTP-MCP | M0 |
| [M2 – Designsystem und Anwendungsshell](roadmap/02-designsystem-und-shell/roadmap.md) | belastbare moderne UI-Grundlage | M1 |
| [M3 – Read-only Wissenscockpit](roadmap/03-read-only-wissenscockpit/roadmap.md) | vollständige menschliche Lesesicht; beginnt mit manuellem M3.0-Gate | M2 |
| [M4 – Transactions und Strukturpflege](roadmap/04-transactions-und-strukturpflege/roadmap.md) | sichere visuelle Strukturänderungen; beginnt mit manuellem M4.0-Gate | M3 |
| [M5 – Rollen-Content und Rich Text](roadmap/05-rollen-content-und-rich-text/roadmap.md) | vollständige Contentpflege ohne Agent; M5.0 abgeschlossen, M5.1–M5.5 freigegeben | M4 |
| [M6 – Betriebs- und Qualitätshärtung](roadmap/06-betrieb-und-qualitaet/roadmap.md) | belastbarer Kernbetrieb; beginnt mit manuellem M6.0-Gate | M1–M5 |
| [M7 – Einfacher PDF-Teilbaumexport](roadmap/07-pdf-export/roadmap.md) | niedrig priorisierter PDF-Download; beginnt mit manuellem M7.0-Gate | M6 |
| [M8 – Bilder und Assetverwaltung](roadmap/08-bilder-und-assets/roadmap.md) | niedrig priorisierte Bilder; beginnt mit manuellem M8.0-Gate | M5, M7 |

## Separate spätere Vorhaben

Nicht als Tasks dieser Roadmap ausführen:

- Authentifizierung, Autorisierung, ACL, Audit und Mandantenmodell.
- Direkter Endkundenzugang.
- Integrierte Agentenorchestrierung und Semantic Kernel.
- Semantic Search und Embeddings.
- Kollaboratives Live-Editing oder automatisches Merge/Rebase.
- Presentation Views mit alternativen Navigationsstrukturen.
- Allgemeine REST-/OpenAPI-Integrations-API für n8n oder andere Systeme; erst bei einem konkreten Automationsfall.
