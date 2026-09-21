# M2 – Systemrahmen und Navigationsfluss

## Ziel und Status

**Status: abgeschlossen / bestanden.** M2 repariert zuerst die Nutzerführung vom
Read-only-Wissenseintrag in den vorhandenen Editor und harmonisiert danach
Layout, Mutation-Zustand und Transaction-Fortsetzung. Bestehende Routen,
Funktionen, ReadContexts, NodeIds und Fachverträge bleiben erhalten. Die
Reihenfolge ist verbindlich und vollständig autonom ausführbar; ein manuelles
Entscheidungsgate zwischen den Leaves entfällt.

## Voraussetzungen und Grenzen

- M1.5-T0–T7 ist abgeschlossen; M1.5-T7 wird nur als Regression verwendet.
- `AudiencesPage` und Zielgruppenverwaltung bleiben außerhalb.
- Fallback-/None-/Independent-/Derived-Contentfunktionen bleiben M5.4; M2
  erklärt diese Zustände und dupliziert keine Contentmutation.
- Es gibt keine neue Route, keinen zweiten fachlichen Einstieg, keine Action
  Registry, keinen Stepper und keine Änderung der Commit-/Discard-Reihenfolge.
- Die ausführbare Zustands-, URL- und Terminologiematrix steht in
  [M2-Planung](planning.md) und vollständig in [M2.0-T1](tasks/M2.0-T1.md).

## Arbeitspakete

- [x] **M2.0 – Node-lokalen Bearbeitungseinstieg**
  - [x] [M2.0-T1 – Node-lokalen Bearbeitungseinstieg führen](tasks/M2.0-T1.md)
  - Ergebnis: `Bearbeiten` ist bei Explicit+Independent sichtbar; ein kleiner
    Dialog führt über eine ausdrückliche Arbeitskopienwahl oder einen neuen
    Start zum Editor derselben Node und Zielgruppe. Fallback, None, Derived und
    historische Kontexte bleiben ehrlich geführt.

- [x] **M2.1 – Gemeinsame Layoutbasis**
  - [x] [M2.1-T1 – Vollbreiten-Seitenrahmen, Prosa-Measure und Action-Group](tasks/M2.1-T1.md)
  - Ergebnis: DRY-Basis und featureweise Adoption außerhalb `AudiencesPage`;
    der abgeschlossene M1.5-T7-Grid-Nachweis bleibt Regression.

- [x] **M2.2 – Mutation-URL und Auswahl**
  - [x] [M2.2-T1 – Node-Mutationen mit synchroner URL-/Selection-Führung](tasks/M2.2-T1.md)
  - Ergebnis: Create child, Create root, Update und Delete behandeln stabile
    `NodeId`, sichtbare Selection, Breadcrumb, Detail und URL als einen Zustand.

- [x] **M2.3 – Transaction-zu-Bearbeitung-Führung**
  - [x] [M2.3-T1 – Bestehenden Wissensbaum-Einstieg im Transaction-Detail führen](tasks/M2.3-T1.md)
  - Ergebnis: Der vorhandene Weg `Im Wissensbaum öffnen` ist im Transaction-
    Detail sichtbar und führt in die Working-Ansicht; er erzeugt keinen zweiten
    Read-only-Einstieg.

- [x] **M2-Audit – autonomer Abschlussaudit**
  - Nur lesen: Ziel, Invarianten, Links, Nachweise und Scopegrenzen prüfen.
    Bei Findings höchstens eine Korrekturrunde; keine eigenständige
    Produkt- oder Feature-Erweiterung.
  - Ergebnis: Korrekturrunde erforderlich; siehe [Audit](audit.md).

## Milestone-Abnahme

- [x] Der node-lokale Weg `Wissenseintrag → Bearbeiten → Arbeitskopie wählen/
  beginnen → gleicher Editor` ist für Explicit+Independent belegt.
- [x] Fallback, None, Derived sowie historische Snapshot-/Release-Kontexte
  zeigen keinen irreführenden Editiereinstieg und keine Sackgasse innerhalb der
  bestehenden Wege.
- [x] Seitenrahmen, Prosa-Measure und Action-Groups sind featureweise belegt;
  1280, 1920, 2560 und Responsive sind nachgewiesen.
- [x] Create root/child, Update und Delete synchronisieren URL und Auswahl;
  ReadContext, `audienceId` und `NodeId` bleiben erhalten.
- [x] Transaction-Detail führt sichtbar in den vorhandenen Working-
  Wissensbaum; kein zweiter fachlicher Einstieg wurde eingeführt.
- [x] Der Audit ist abgeschlossen; maximal eine Korrekturrunde wurde genutzt.
