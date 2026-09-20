# 13 – Commit-Dialog

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/13_transaction_commit-dialog_desktop_1280x800.png` · Route `/transactions/<TransactionId>` · Commit-Bestätigungsdialog · Desktop 1280×800.

## Neutrale Beobachtung

- Ein Bestätigungsdialog liegt über dem Transaktionsinhalt.
- Der Dialograhmen wirkt hart und stärker technisch als die umgebende Oberfläche.
- Commit und Abbrechen sind gemeinsam sichtbar, aber ihre visuelle Gewichtung ist nicht vollständig eindeutig.
- Die Dialogsprache erklärt die Folge des Commit nicht in einer kurzen aufgabenorientierten Zusammenfassung.
- Der Hintergrundzustand bleibt erkennbar, ist aber für die Bestätigung nicht der primäre Arbeitsabschnitt.
- Der Dialog ist als kritischer Lebenszyklusschritt grundsätzlich sichtbar.

## Probleme und Schwere

- **P1 – Konsistenz:** Rahmen, Abstände und visuelle Behandlung weichen von der übrigen Dialogsprache ab.
- **P1 – Sicherheit:** Die dauerhafte Wirkung des Commit könnte vor der primären Aktion deutlicher sein.
- **P2 – Aktion:** Commit und Abbrechen konkurrieren, statt genau eine primäre Dialogaktion zu führen.
- **P2 – Fokus:** Der sichtbare Fokus-/Abbruchweg ist nicht aus dem Bild sicher rekonstruierbar.

## Gelungene Aspekte

- Die Bestätigung trennt Commit vom übrigen Arbeitsinhalt.
- Beide bestehenden Ausgänge bleiben sichtbar.
- Der kritische Schritt ist nicht als sofortige Hintergrundaktion verborgen.

## Folgerungen ohne Featureausweitung

- Vorhandenen Dialograhmen an die bestehende UI-Sprache angleichen.
- Commit-Folge und bestehende primäre Bestätigung knapp voranstellen.
- Abbrechen sekundär, aber keyboard-erreichbar halten.
- Keine Änderung der Commit-Semantik oder Transaktionslogik.

## Abhängigkeiten

Native Dialog-/Keyboard-/Focus-Vertrag, Commit-Lifecycle, M1.3-Re-Audit.

## Audit-Lücken

Keine.

## M1.3-Re-Audit und Folgeentscheidung

Aktuelle Quelle: `temp/ui-audit/2026-09-20_20-05-26/13_transaction_commit-dialog_desktop_1280x800.png`. Der Dialog bleibt fachlich korrekt ausgelöst; der P1-Befund betrifft die gemeinsame visuelle Darstellung mit Zustand 14. [M1.4-T3](../../tasks/M1.4-T3.md) darf Rahmen, Abstände und Microcopy angleichen, aber Commit-Semantik, Primäraktion, Abbruchweg und Keyboard-Fokus nicht verändern.
