# M2 – Systemrahmen und Navigationsfluss

## Ziel

M2 etabliert eine gemeinsame Layoutbasis und schließt die zwei belegten Handlungsbrüche zwischen fachlicher Auswahl, sichtbarer Arbeitsfläche und URL. Bestehende Routen, Funktionen, ReadContexts, NodeIds und Fachverträge bleiben unverändert. Die Etappe endet mit einem manuellen Entscheidungsgate für IA, Terminologie und den node-lokalen Bearbeitungseinstieg.

## Status und Abhängigkeiten

- **Status:** geplant, noch nicht zur Ausführung freigegeben; `planning.md` ist das M2-Planungsgate.
- **Voraussetzungen:** M1.5-T0–T7 abgeschlossen; der T7-Nachweis wird nicht dupliziert.
- **Nicht im Scope:** `RolesPage`, Rollenverwaltung, neue Fachfunktionen, Fallback-/erster Content (M5.4-T1), Commit-Reihenfolge und neue Routen.
- **Leitplanken:** [M2-Konzept](../../konzept/README.md), [Layout-/Aktionssystem](../../konzept/04-layout-und-aktionssystem.md), [Nutzerreisen](../../konzept/02-nutzerreisen-und-zustandsfluesse.md), [Findings](../../konzept/05-findings-und-evidenz.md).

## Arbeitspakete und Leaves

### M2.1 – Gemeinsame Layoutbasis

- [ ] [M2.1-T1 – Vollbreiten-Seitenrahmen, Prosa-Measure und Action-Group](tasks/M2.1-T1.md)

Ergebnis: DRY `page-frame`/`readable`/`action-group`-Basis und featureweise Adoption außerhalb `RolesPage`; Nachweise bei 1280, 1920, 2560 und Responsive. M1.5-T7 wird nur als abgeschlossener Regressionsnachweis berücksichtigt.

### M2.2 – Mutation-URL und Auswahl

- [ ] [M2.2-T1 – Node-Mutationen mit synchroner URL-/Selection-Führung](tasks/M2.2-T1.md)

Ergebnis: Create child, Create root, Update und Delete behandeln stabile `NodeId`, sichtbare Selection und URL konsistent; Red-Test-first.

### M2.3 – Transaction-zu-Bearbeitung-Führung

- [ ] [M2.3-T1 – Bestehenden Wissensbaum-Einstieg im Transaction-Detail führen](tasks/M2.3-T1.md)

Ergebnis: Der vorhandene Weg `Im Wissensbaum öffnen` ist im Bearbeitungsfluss des Transaction-Details sichtbar und priorisiert, ohne neue Aktion oder Route.

## Entscheidungsgate nach den ersten Leaves

Nach M2.1–M2.3 wird ein manueller Gate durchgeführt. Erst danach werden IA-Variante, Terminologie, node-lokaler Bearbeitungseinstieg, Icon/Text-Auflösung, PageActions-Kandidaten und gegebenenfalls Pflichtrollenführung als weitere Arbeit freigegeben. Die Entscheidung steht in [M2-Planung](planning.md) und verlinkt das [Entscheidungsregister](../../konzept/06-entscheidungsregister.md); sie wird nicht in einem Leaf vorweggenommen.

## Milestone-Abnahme

- [ ] Seitenrahmen und Basisaktionen sind ohne Funktionsverlust featureweise adoptiert; `RolesPage` bleibt unangetastet.
- [ ] Prosa hat eine begrenzte Lesemessung, Seitenrahmen und fachliche Arbeitsflächen nutzen die verfügbare Breite.
- [ ] Node-Mutationen aktualisieren sichtbare Auswahl und URL als einen nachvollziehbaren Zustand.
- [ ] Transaction-Detail führt über den vorhandenen Link sichtbar zurück in den Working-Wissensbaum.
- [ ] Nachweise belegen 1280/1920/2560 und Responsive; keine neue Route, kein M5.4-Verhalten, kein T7-Duplikat.

## Audit

- [ ] Begrenzter Audit gegen Ziel, Invarianten, Links und Nachweise
- Ergebnis: offen; Entscheidungsgate nach den drei Leaves erforderlich
