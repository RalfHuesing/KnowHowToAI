# 09 – History-Diff

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/09_history_snapshot-diff_desktop_1280x800.png` · Route `/history` · beabsichtigter Snapshot-Diff · Desktop 1280×800.

## Neutrale Beobachtung

- Der Dateiname bezeichnet einen Diff-Zustand.
- Das Bild ist pixelidentisch zur Aufnahme 08.
- Es gibt keinen sichtbaren zusätzlichen Vergleichsbereich, keine markierte Änderung und keine erkennbare zweite Snapshot-Auswahl.
- Navigation und technische Historydarstellung wirken wie im Grundzustand.
- Aus dem Bild lässt sich nicht ableiten, ob ein Diff leer, nicht geladen oder gar nicht ausgewählt ist.
- Der Capture belegt daher eher die Wiederholung der Grundansicht als den behaupteten Zustand.

## Probleme und Schwere

- **P1 – Nachweis:** Ein Diff ist visuell nicht belegt.
- **P1 – Erwartung:** Nutzer könnten eine Auswahl vorgenommen haben, ohne eine sichtbare Veränderung zu erhalten.
- **P2 – Benennung:** Dateiname und Bildinhalt erzeugen einen falschen dauerhaften Befund.
- **P2 – Daten:** Seed-/Snapshot-Vorbedingung ist für spätere Reproduktion unklar.

## Gelungene Aspekte

- Die History-Route ist stabil erreichbar.
- Der pixelidentische Vergleich macht die Capture-Lücke eindeutig und prüfbar.
- Der Grundzustand bleibt als Fallback nachvollziehbar.

## Folgerungen ohne Featureausweitung

- Zwei vorhandene Snapshots deterministisch auswählen und vor Capture auf sichtbare Unterscheidung prüfen.
- Bei leerem oder nicht erreichbarem Diff den Zustand entsprechend benennen und nicht als Diff ausgeben.
- Keine Diff-Funktion oder Fachsemantik aus diesem Bild ableiten.
- Manifest und Einzelbefund an den tatsächlich sichtbaren Zustand binden.

## Abhängigkeiten

M1.2-T1, Visual-Shell-Seed, Snapshotdaten und bestehender History-/Diff-Vertrag.

## Audit-Lücken

**Ja.** Kein sichtbarer Diff-Nachweis; dieser Zustand ist bis zur Korrektur nicht als funktionierender Diff belegbar.

## M1.2-Nachweis

Der stabilisierte UiAudit-Lauf `temp/ui-audit/2026-09-20_19-48-27/09_history_snapshot-diff_desktop_1280x800.png` wählt den ältesten Snapshot als Ausgang und den neuesten als Ziel. Nach dem asynchronen Auswahl- und Ladezustand wird auf mindestens einen Diff-Eintrag gewartet und dieser in den 1280×800-Viewport gescrollt. Das Bild zeigt sichtbare Änderungen (unter anderem Rollenauflösung, Knoten und Inhalt); die ursprüngliche Audit-Lücke ist für den Runner geschlossen.

## M1.3-Bestätigung

Der grüne manuelle Re-Audit-Lauf `temp/ui-audit/2026-09-20_20-05-26/09_history_snapshot-diff_desktop_1280x800.png` bestätigt erneut sichtbare Snapshot-Änderungen. Die Capture-Lücke ist abgeschlossen; History/Diff bleibt fachlich unverändert und außerhalb der ersten M1.4-Reihe.

## M1.5-Re-Audit und Folgeentscheidung

Der Lauf `temp/ui-audit/2026-09-20_21-35-15/09_history_snapshot-diff_desktop_1280x800.png`
bestätigt erneut sichtbare Vergleichseinträge. Die Auswahl der vorhandenen
Ausgangs-/Ziel-Snapshots und der Vergleich sind im Ablauf vorhanden, werden im
Capture aber nicht als zusammenhängender Kontext geführt; technische IDs
dominieren den sichtbaren Diff. M1.5-T5 darf deshalb nur Darstellung,
Gruppierung und progressive Sichtbarkeit ändern. Auswahl-, Lade- und
Diff-Logik sowie Routen bleiben unverändert.
