# 17 – Editor Markdown-Quelle dirty

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/17_content-editor-markdown-source-dirty_desktop_1280x800.png` · Route `/knowledge/<NodeId>` · Markdown-Quelle aktiv, Inhalt dirty · Desktop 1280×800.

## Neutrale Beobachtung

- Der Header zeigt dasselbe deutliche „Ungespeicherte Änderungen“-Signal wie im WYSIWYG-Zustand.
- Die gleiche Node bleibt über Baum, Breadcrumb und Titel zugeordnet.
- Markdown-Quelle ist aktiv markiert; WYSIWYG bleibt als Wechselmöglichkeit sichtbar.
- Ein monospaces Textfeld zeigt „UI-Audit-Quelle“ und eine ungespeicherte Markdown-Zeile.
- Das Quelltextfeld ist fokussiert und nimmt den unteren zentralen Bereich ein.
- Metadaten, History und Download stehen weiterhin vor dem Editor.
- Speichern bzw. der Abschlussstatus ist im ersten 1280×800-Viewport nicht sichtbar.
- Die Aufnahme erklärt nicht, welche Konsequenz ein Moduswechsel für dirty Inhalt hat.

## Probleme und Schwere

- **P0/P1 – Abschluss:** Speichern ist auch im Source-Workflow nicht im ersten sichtbaren Arbeitsabschnitt erreichbar.
- **P1 – Moduswechsel:** Die Beziehung zwischen WYSIWYG, Quelle und bestehendem Dirty-State bleibt implizit.
- **P1 – Raum:** Technische Metadaten verdrängen Quelltext und Abschlussaktion.
- **P2 – Sprache:** „Markdown-Quelle“ ist korrekt, aber ohne kurze Aufgabenbeschreibung für Nicht-Entwickler schwer einzuordnen.
- **P2 – Vergleich:** WYSIWYG und Source unterscheiden sich technisch, aber ihre gemeinsame Aufgabe wird nicht gleich geführt.

## Gelungene Aspekte

- Aktiver Source-Modus, Fokus und Monospace-Darstellung sind eindeutig.
- Dirty-Zustand ist konsistent und sichtbar.
- Die Node-Zuordnung bleibt über Baum/Breadcrumb/Titel stabil.
- Die vorhandene Quelle ist lesbar und bearbeitbar; keine neue Editorfunktion ist erforderlich.

## Folgerungen ohne Featureausweitung

- Speichern im sichtbaren Source-Arbeitsabschnitt führen.
- Moduswechsel mit vorhandener Aufgabe und Dirty-Vertrag verständlich beschriften.
- Technische Metadaten progressiv offenlegen, ohne Quelltextzugang zu entfernen.
- Kein zusätzlicher Content-Verwerfen-Button; transaktionsweites Verwerfen bleibt in der bestehenden Transaction-Lifecycle-Navigation.

## Abhängigkeiten

M1.2-T2, Editor-/Dirty-/Keyboard-Verträge und separater Transaction-Lifecycle für Commit/Discard.

## Audit-Lücken

Keine.
