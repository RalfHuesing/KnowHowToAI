# M1 – Ist-Audit und erste visuelle Iteration

## Ziel

Die bestehende Oberfläche wird anhand reproduzierbarer Desktop-Zustände auf Verständlichkeit, Layoutführung und sichtbare Handlungsfähigkeit geprüft. M1 liefert zunächst eine belastbare Ist-Basis und danach kleine, verifizierbare Korrekturen. Es werden keine neuen Fachfunktionen und keine geänderten Fachverträge eingeführt.

## Designlinie

- Support/Consultants erkennen Aufgabe, Inhalt und nächste Aktion ohne technische Vorbildung.
- Deutsche, aufgabenorientierte Begriffe sind sichtbar; technische Details erscheinen progressiv im nativen Expertenbereich.
- Je gleichzeitig sichtbarem Arbeitsabschnitt gibt es eine primäre Aktion; Inhalt und Aufgabe stehen vor Systemzustand.
- Accessibility, Keyboard, Responsive, Dirty-State und Transaktionsverhalten bleiben Verträge.

## Meilensteine

- [x] **M1.0 Planung** – Zielgruppe, Leitplanken, Evidenz- und Entscheidungsregeln in `planning.md`.
- [x] **M1.1 Ist-Audit**
  - [x] [M1.1-T1 – Ist-Audit anhand der 20 Screenshot-Zustände](tasks/M1.1-T1.md) – Befunde und Synthese abgeschlossen.
- [ ] **M1.2 Erste Korrektur**
  - [x] [M1.2-T1 – Audit-Capture-Zustände 03, 09 und 20 stabilisieren](tasks/M1.2-T1.md) – Runner/Test-Zustände sichtbar und semantisch korrekt erfassen; keine Produktlösung vortäuschen.
  - [ ] [M1.2-T2 – Wissensarbeitsplatz und Editor im ersten Viewport](tasks/M1.2-T2.md) – bestehende Workflows handlungsfähig machen: Speichern sichtbar, Read-only/Working eindeutig, Metadaten progressiv, Struktur und Content unterscheidbar.
- [ ] **M1.3 manueller Re-Audit** – ohne eigenes Leaf; nach dem Re-Audit werden neue kleine Tasks aus den Belegen geschnitten.

## Nicht freigegebene Kandidaten

Shell/Navigation, Suche/Filter und History/Diff bleiben nachgelagerte Kandidaten. Für sie wird in M1 kein großer Änderungs- oder Refactoring-Task freigegeben; die vorhandenen Hinweise bleiben als Folgebeobachtung in der Synthese.

## Abschlusskriterien

M1.1 ist mit den 20 Befunddateien und der Synthese abgeschlossen. M1.2 gilt erst nach gezieltem Testlauf, visueller Prüfung und dokumentiertem Vergleich als abgeschlossen. M1.3 bestätigt oder verwirft die Befunde anhand eines neuen manuellen Audits.
