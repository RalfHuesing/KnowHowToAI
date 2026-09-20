# 16 – Editor WYSIWYG dirty

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/16_content-editor-wysiwyg-dirty_desktop_1280x800.png` · Route `/knowledge/<NodeId>` · WYSIWYG aktiv, Inhalt dirty · Desktop 1280×800.

## Neutrale Beobachtung

- Der Header zeigt „Ungespeicherte Änderungen“ als deutliches Dirty-Signal.
- Im Baum ist eine Node ausgewählt; Breadcrumb und Titel orientieren den Inhalt.
- WYSIWYG ist als aktiver Modus markiert.
- Der Editor enthält sichtbaren Text „UI-Audit-WYSIWYG“ und „ungespeichert“.
- Crepe-/Editor-Steuerelemente und ein Link-/Eingabebereich erscheinen unterhalb des Textes.
- Metadaten, History und Download stehen oberhalb des Editorbereichs.
- Speichern bzw. der Abschlussstatus ist im ersten 1280×800-Viewport nicht sichtbar.
- Der ausgewählte Node-Titel ist im Baum verkürzt.

## Probleme und Schwere

- **P0/P1 – Abschluss:** Die zentrale bestehende Speichern-Aktion ist während des dirty Workflows nicht sichtbar.
- **P1 – Zusammenhang:** Dirty-Signal oben und Speichern außerhalb des Viewports bilden keinen geschlossenen Handlungsabschnitt.
- **P1 – Raum:** Metadaten/Links verbrauchen vertikalen Raum vor dem Inhalt.
- **P2 – Dichte:** Editor-Steuerelemente erscheinen, bevor der Abschluss der Aufgabe klar geführt wird.
- **P2 – Orientierung:** Abgeschnittener Node-Titel erschwert Zuordnung bei längeren Namen.

## Gelungene Aspekte

- Dirty-Status ist textlich und farblich deutlich sichtbar.
- WYSIWYG-Modus und fokussierter bearbeiteter Inhalt sind eindeutig.
- Baum, Breadcrumb und Titel halten die Node-Zuordnung nachvollziehbar.
- Der bestehende Editor zeigt tatsächliche Bearbeitung statt einer leeren Demo.

## Folgerungen ohne Featureausweitung

- Speichern im selben sichtbaren Arbeitsabschnitt wie Dirty-Status und Editor führen.
- Metadaten/Links sekundär oder progressiv anzeigen, ohne sie zu löschen.
- Modus und Inhalt vor Systemdiagnose priorisieren.
- Bestehende Speichern-Semantik erhalten; keinen zusätzlichen Editor-Verwerfen-Button aus diesem Befund ableiten.

## Abhängigkeiten

M1.2-T2, Editor-/Dirty-/Working-Vertrag, Keyboard-Fokus, Transaction-Lifecycle für separates Verwerfen.

## Audit-Lücken

Keine.

## Re-Audit nach M1.2-T2

`temp/ui-audit/2026-09-20_20-05-26/16_content-editor-wysiwyg-dirty_desktop_1280x800.png`

„Ungespeicherte Änderungen“ und die bestehende Speichern-Aktion stehen nun im
selben sichtbaren Inhaltsabschnitt oberhalb der WYSIWYG-Fläche. Der Working-
Kontext ist am Node verständlich markiert; technische Details verdrängen den
Editor nicht mehr.
