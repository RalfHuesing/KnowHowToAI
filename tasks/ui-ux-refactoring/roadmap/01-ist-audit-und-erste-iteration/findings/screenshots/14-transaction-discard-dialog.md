# 14 – Discard-Dialog

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/14_transaction_discard-dialog_desktop_1280x800.png` · Route `/transactions/<TransactionId>` · Discard-/Verwerfen-Bestätigungsdialog · Desktop 1280×800.

## Neutrale Beobachtung

- Der Dialog liegt über dem offenen Transaktionskontext und markiert einen kritischen Pfad.
- Rahmen, Abstände und Tonalität wirken hart und inkonsistent zur restlichen Oberfläche.
- Die Konsequenz des Verwerfens ist nicht als kurze, sofort verständliche Aufgabeninformation priorisiert.
- Bestätigen und Abbrechen sind gemeinsam sichtbar.
- Der Hintergrundzustand bleibt vorhanden, lenkt aber zusätzlich vom Dialogtext ab.
- Die Aufnahme zeigt keinen neuen Fachzustand, sondern die bestehende Bestätigungssituation.

## Probleme und Schwere

- **P1 – Sicherheit:** Die Folge des Verwerfens könnte vor der primären Bestätigung deutlicher formuliert sein.
- **P1 – Konsistenz:** Der Dialograhmen passt nicht harmonisch zu anderen Arbeitsabschnitten.
- **P2 – Hierarchie:** Destruktive und sichere Aktion sind nicht ausreichend differenziert.
- **P2 – Fokus:** Fokus- und Tastaturverhalten sind aus dem Screenshot allein nicht verifizierbar.

## Gelungene Aspekte

- Der destruktive Pfad verlangt grundsätzlich eine Bestätigung.
- Der offene Transaktionskontext bleibt nachvollziehbar.
- Abbrechen ist als Sicherheitsausgang vorhanden.

## Folgerungen ohne Featureausweitung

- Bestehenden Dialog visuell konsistent und verständlich rahmen.
- Verwerfen-Folge vor der vorhandenen primären Bestätigung erläutern.
- Abbrechen sekundär, sichtbar und keyboard-fähig halten.
- Fachliche Verwerfen-Semantik unverändert lassen.

## Abhängigkeiten

Dialog-/Dirty-/Keyboard-Vertrag und Transaction-Lifecycle; M1.3-Kandidat.

## Audit-Lücken

Keine.
