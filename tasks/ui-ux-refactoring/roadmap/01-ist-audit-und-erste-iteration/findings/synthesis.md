# Ist-Synthese – UI-Audit

## Evidenzbasis

Die 20 Einzelbefunde unter [findings/screenshots](screenshots) beziehen sich auf den reproduzierbaren Lauf `temp/ui-audit/2026-09-20_18-36-16`, Desktop 1280×800, und auf die im Manifest benannten Routen/Zustände. Die Bilder sind temporär; diese Synthese und die Einzelbefunde sind der versionierte Nachweis. Ein Befund beschreibt den sichtbaren Zustand, nicht eine vermutete Produktabsicht.

## Priorisierte Cluster

### P0/P1 – Handlungsfähigkeit im Wissensarbeitsplatz

In 15 sind Strukturaktionen im ersten Viewport nicht sichtbar. In 16 und 17 sind Dirty-State und Editorinhalt sichtbar, das Speichern liegt jedoch unterhalb des Viewports. Damit ist die zentrale Support-Aufgabe – Text einer Node bearbeiten und sicher abschließen oder verwerfen – nicht als zusammenhängender Ablauf lesbar. Die technische Metadaten-/Linkfläche nimmt gleichzeitig viel vertikalen Raum ein. Siehe 15–17.

### P0/P1 – Bestätigung und destruktive Sicherheit

20 beweist den ausgelösten Löschpfad, zeigt aber im 1280×800-Viewport keine sichtbare Bestätigung; die erwartete Bestätigung liegt unterhalb des sichtbaren Bereichs. Das ist eine Audit-Lücke mit hoher Relevanz für destruktives Verhalten, keine Freigabe für eine Produktannahme. 13/14 zeigen zusätzlich inkonsistente, harte Dialograhmen. Siehe 13, 14 und 20.

### P1 – Technischer Zustand verdrängt Inhalt

15–17 führen Revision, Node-ID, Verfügbarkeit, Inhaltsmodus und Links vor dem eigentlichen Inhalt. 18/19 machen RoleId und den globalen Kontext prominent, während die aufgabenorientierte Erklärung zurücktritt. Die technische Information muss erhalten bleiben, ist aber derzeit nicht progressiv offengelegt. Siehe 15–19.

### P1/P2 – Modus und Kontext sind implizit

18 kennzeichnet Read-only verständlich, 19 leitet Working nur aus der sichtbaren Formular-/Aktionsfläche ab. Der globale Hinweis „Keine Rolle ausgewählt“ bleibt daneben stehen. 16/17 zeigen den Dirty-Hinweis, koppeln ihn aber nicht sichtbar an Speichern/Verwerfen. Das erhöht die kognitive Last, obwohl die bestehenden Zustände vorhanden sind.

### P2 – Navigation, Suche und History als Folgearbeit

01 wirkt technisch-diagnostisch dominiert; 02/03 erklären Rollenauswahl und Bestätigung schwach. 06/07 lassen Filterfläche Ergebnisse verdrängen, 08 ist technisch formuliert, 09 ist pixelidentisch zu 08 und belegt keinen Diff-Zustand. Diese Bereiche bleiben bewusst Kandidaten und werden in M1 nicht als große Änderungsaufgaben freigegeben.

## Audit-Lücken und Konsequenz

03 ist nur der ausgewählte Zustand vor einer belastbaren Root-Bestätigung, 09 belegt keinen Snapshot-Diff, 20 zeigt keine sichtbare Löschbestätigung. Diese drei Lücken sind in den Einzeldateien markiert und bilden M1.2-T1. Bis dahin werden sie nicht als Produktbefund „behoben“ dargestellt.

## Nächster kleiner Slice

M1.2-T1 stabilisiert nur die capture-seitige Sichtbarkeit und Assertions. M1.2-T2 adressiert anschließend den bestehenden Wissensarbeitsplatz/Editor. Erst der manuelle Re-Audit entscheidet, ob daraus weitere kleine Tasks entstehen.
