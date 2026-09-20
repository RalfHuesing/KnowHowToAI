# 20 – Rollen-Löschdialog

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/20_roles_delete-dialog_desktop_1280x800.png` · Route `/roles` · Löschaktion ausgelöst, erwarteter Bestätigungsdialog · Desktop 1280×800.

## Neutrale Beobachtung

- Der Capture zeigt im sichtbaren Bereich praktisch dieselbe Rollenliste wie 19.
- Formular und Rollen-Karten bleiben an ihrer bisherigen Position.
- Die Löschaktion ist aus dem Zustand/der Interaktion bekannt, aber kein sichtbarer Bestätigungsdialog liegt über der Liste.
- Der untere Bereich des erwarteten Pfads befindet sich außerhalb des 1280×800-Viewports.
- Ein Bestätigungstext, betroffener Rollenname und primäre destruktive Aktion sind im Bild nicht prüfbar.
- Der Screenshot kann daher den Click bzw. Szenariofortschritt, nicht aber die Nutzerentscheidung vor Löschung belegen.

## Probleme und Schwere

- **P0/P1 – Sicherheit:** Die Bestätigung einer destruktiven Aktion ist im Capture nicht sichtbar und damit nicht auditierbar.
- **P1 – Vertrauen:** Nutzer sehen weder, wofür sie bestätigen sollen, noch welchen sicheren Abbruchweg es gibt.
- **P1 – Capturesemantik:** Dateiname behauptet einen Dialogzustand, während das Bild den Working-Listen-Zustand zeigt.
- **P2 – Layout:** Der untere Dialog-/Bestätigungsbereich wird nicht in den sichtbaren Arbeitsabschnitt geführt.

## Gelungene Aspekte

- Die Rollenliste und die ausgelöste Aktion sind reproduzierbar vorbereitet.
- Der Befund macht die Grenze zwischen Interaktion und sichtbarem Ergebnis klar.
- Die bestehende Rollenkarte bleibt als Kontext erhalten.

## Folgerungen ohne Featureausweitung

- Den vorhandenen Bestätigungsdialog im Runner fokussieren und erst nach sichtbarer Assertion capturen.
- Manifest und Dateiname an den tatsächlich sichtbaren Zustand binden.
- Keine neue Produktbestätigung im Audit-Task vortäuschen.
- Nach der Capture-Korrektur den bestehenden Dialog auf Text, Fokus und Abbruchweg prüfen.

## Abhängigkeiten

M1.2-T1, bestehender Rollen-/Dialog-/Keyboard-Vertrag, Working-Transaction-Lifecycle.

## Audit-Lücken

**Ja.** Der Click ist belegt; ein sichtbarer Bestätigungsdialog ist im 1280×800-Capture nicht belegt.

## M1.2-Nachweis

Der stabilisierte UiAudit-Lauf `temp/ui-audit/2026-09-20_19-48-27/20_roles_delete-dialog_desktop_1280x800.png` prüft den vorhandenen Bestätigungstext und die primäre Löschaktion, scrollt den Bestätigungsbereich in den Viewport und setzt den Fokus auf die vorhandene Bestätigungsaktion. Das Bild zeigt den bestehenden Dialog vollständig; die ursprüngliche Audit-Lücke ist für den Runner geschlossen.
